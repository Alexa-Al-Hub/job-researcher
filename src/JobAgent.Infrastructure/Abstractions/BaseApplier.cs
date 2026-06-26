using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Abstractions;

/// <summary>
/// Shared apply flow: log in (if needed) → open the application form → fill each
/// field (known fields directly, unknown ones via the LLM form service) → submit.
/// Platform subclasses supply login and the apply-button selector.
/// </summary>
public abstract class BaseApplier : IApplier
{
    protected readonly PlaywrightBrowserFactory BrowserFactory;
    protected readonly IApplicationFormService FormService;
    protected readonly RateLimitOptions RateLimit;
    protected readonly ILogger Logger;

    public abstract Platform Platform { get; }

    /// <summary>Selector for the button that opens the application form on the job page.</summary>
    protected abstract string ApplyButtonSelector { get; }

    protected BaseApplier(
        PlaywrightBrowserFactory browserFactory,
        IApplicationFormService formService,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger logger)
    {
        BrowserFactory = browserFactory;
        FormService = formService;
        RateLimit = rateLimitOptions.Value;
        Logger = logger;
    }

    public async Task<bool> ApplyAsync(Job job, string? cvPath, CancellationToken ct = default)
    {
        if (!CanExecute())
            return false;

        var page = await BrowserFactory.NewPageAsync();
        try
        {
            await BeforeApplyAsync(page, ct);

            if (!await StartApplicationAsync(page, job, ct))
            {
                Logger.LogInformation("{Platform}: no in-app apply form for \"{Title}\" — manual follow-up",
                    Platform, job.Title);
                return false;
            }

            return await CompleteApplicationAsync(page, job, cvPath, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Error applying to {Platform} job {Url}", Platform, job.Url);
            return false;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    protected virtual bool CanExecute() => true;

    protected virtual Task BeforeApplyAsync(IPage page, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Navigates to the job and opens the application form. Returns false if there is none.</summary>
    protected virtual async Task<bool> StartApplicationAsync(IPage page, Job job, CancellationToken ct)
    {
        await page.GotoAsync(job.Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        var applyButton = await page.QuerySelectorAsync(ApplyButtonSelector);
        if (applyButton == null || !await applyButton.IsVisibleAsync())
            return false;

        await applyButton.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        return await page.QuerySelectorAsync(ApplierConstants.FormFieldProbe) != null;
    }

    /// <summary>Walks the (possibly multi-step) form, filling fields and clicking Next/Submit.</summary>
    protected virtual async Task<bool> CompleteApplicationAsync(
        IPage page, Job job, string? cvPath, CancellationToken ct)
    {
        var cvUploaded = false;

        for (var step = 0; step < ApplierConstants.MaxFormSteps; step++)
        {
            ct.ThrowIfCancellationRequested();

            if (!cvUploaded && !string.IsNullOrEmpty(cvPath))
                cvUploaded = await TryUploadCvAsync(page, cvPath);

            if (!await FillVisibleFieldsAsync(page, job, ct))
            {
                Logger.LogWarning("{Platform}: a required question could not be answered — aborting \"{Title}\"",
                    Platform, job.Title);
                return false;
            }

            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

            var submit = await page.QuerySelectorAsync(ApplierConstants.SubmitSelector);
            if (submit != null && await submit.IsVisibleAsync() && await submit.IsEnabledAsync())
            {
                await submit.ClickAsync();
                Logger.LogInformation("{Platform}: submitted application for \"{Title}\" at {Company}",
                    Platform, job.Title, job.Company);
                return true;
            }

            var next = await page.QuerySelectorAsync(ApplierConstants.NextSelector);
            if (next == null || !await next.IsVisibleAsync())
            {
                Logger.LogWarning("{Platform}: no Submit/Next control on step {Step} — aborting \"{Title}\"",
                    Platform, step, job.Title);
                return false;
            }

            await next.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        Logger.LogWarning("{Platform}: exceeded {Max} form steps for \"{Title}\"",
            Platform, ApplierConstants.MaxFormSteps, job.Title);
        return false;
    }

    protected async Task<bool> TryUploadCvAsync(IPage page, string cvPath)
    {
        if (!File.Exists(cvPath))
            return false;

        var fileInput = await page.QuerySelectorAsync(ApplierConstants.FileInputSelector);
        if (fileInput == null)
            return false;

        await fileInput.SetInputFilesAsync(cvPath);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Logger.LogInformation("{Platform}: uploaded CV {Path}", Platform, Path.GetFileName(cvPath));
        return true;
    }

    /// <summary>Fills every visible empty field. Returns false if a *required* field can't be answered.</summary>
    private async Task<bool> FillVisibleFieldsAsync(IPage page, Job job, CancellationToken ct)
    {
        var fields = await page.QuerySelectorAllAsync(ApplierConstants.FillableFieldSelector);

        foreach (var field in fields)
        {
            ct.ThrowIfCancellationRequested();

            if (!await field.IsVisibleAsync())
                continue;

            var currentValue = await field.InputValueAsync();
            if (!string.IsNullOrWhiteSpace(currentValue))
                continue;

            var label = NormalizeLabel(await DeriveLabelAsync(field));
            if (string.IsNullOrWhiteSpace(label))
                continue;

            var required = await field.EvaluateAsync<bool>(
                "el => el.required || el.getAttribute('aria-required') === 'true'");
            var tag = await field.EvaluateAsync<string>("el => el.tagName.toLowerCase()");

            if (tag == "select")
            {
                var options = await field.EvaluateAsync<string[]>(
                    "el => Array.from(el.options).map(o => o.text).filter(t => t && t.trim())");
                var answer = await FormService.AnswerAsync(label, options, job, ct);
                if (answer == null)
                {
                    if (required) return false;
                    continue;
                }

                try
                {
                    await field.SelectOptionAsync(new[] { new SelectOptionValue { Label = answer } });
                }
                catch
                {
                    if (required) return false;
                }
            }
            else
            {
                var answer = await FormService.AnswerAsync(label, null, job, ct);
                if (answer == null)
                {
                    if (required) return false;
                    continue;
                }

                await field.FillAsync(answer);
            }
        }

        return true;
    }

    private static async Task<string?> DeriveLabelAsync(IElementHandle field) =>
        await field.EvaluateAsync<string?>(@"el => {
            if (el.getAttribute('aria-label')) return el.getAttribute('aria-label');
            if (el.id) { const l = document.querySelector(`label[for='${el.id}']`); if (l) return l.innerText; }
            const wrap = el.closest('label'); if (wrap) return wrap.innerText;
            if (el.placeholder) return el.placeholder;
            return el.name || null;
        }");

    private static string NormalizeLabel(string? label) =>
        string.IsNullOrWhiteSpace(label)
            ? ""
            : string.Join(' ', label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
}
