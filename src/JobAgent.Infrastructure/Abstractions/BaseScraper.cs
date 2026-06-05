using JobAgent.Application.Common;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Abstractions;

public abstract class BaseScraper : IScraper
{
    protected readonly PlaywrightBrowserFactory BrowserFactory;
    protected readonly RateLimitOptions RateLimit;
    protected readonly ILogger Logger;

    public abstract Platform Platform { get; }

    protected BaseScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger logger)
    {
        BrowserFactory = browserFactory;
        RateLimit = rateLimitOptions.Value;
        Logger = logger;
    }

    public async Task<IReadOnlyList<CreateJobRequest>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        if (!CanExecute())
            return Array.Empty<CreateJobRequest>();

        var page = await BrowserFactory.NewPageAsync();
        try
        {
            await BeforeScrapingAsync(page, ct);
            return await ParseJobsAsync(page, keywords, location, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Error during {Platform} scraping", Platform);
            return Array.Empty<CreateJobRequest>();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    protected abstract Task<IReadOnlyList<CreateJobRequest>> ParseJobsAsync(
        IPage page, string keywords, string location, CancellationToken ct);

    protected virtual bool CanExecute() => true;

    protected virtual Task BeforeScrapingAsync(IPage page, CancellationToken ct) => Task.CompletedTask;

    protected static async Task<(string Title, string? Href, string Company, string? Salary)> ExtractCardAsync(
        IElementHandle card, string titleSelector, string companySelector, string? salarySelector = null, string? linkSelector = null)
    {
        var titleTask = card.QuerySelectorAsync(titleSelector);
        var companyTask = card.QuerySelectorAsync(companySelector);
        var salaryTask = salarySelector != null ? card.QuerySelectorAsync(salarySelector) : Task.FromResult<IElementHandle?>(null);
        var linkTask = linkSelector != null ? card.QuerySelectorAsync(linkSelector) : Task.FromResult<IElementHandle?>(null);
        await Task.WhenAll(titleTask, companyTask, salaryTask, linkTask);

        var titleEl = titleTask.Result;
        var companyEl = companyTask.Result;
        var salaryEl = salaryTask.Result;
        var linkEl = linkTask.Result;

        var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
        var href = linkEl != null
            ? await linkEl.GetAttributeAsync("href")
            : titleEl != null ? await titleEl.GetAttributeAsync("href") : null;
        var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
        var salary = salaryEl != null ? (await salaryEl.InnerTextAsync()).Trim() : null;

        return (title, href, company, salary);
    }
}
