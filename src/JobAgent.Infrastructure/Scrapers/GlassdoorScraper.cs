using JobAgent.Application.Common;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Scrapers;

public class GlassdoorScraper : IScraper
{
    public Platform Platform => Platform.Glassdoor;

    private readonly PlaywrightBrowserFactory _browserFactory;
    private readonly IOptions<CredentialOptions> _credentials;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<GlassdoorScraper> _logger;

    public GlassdoorScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<GlassdoorScraper> logger)
    {
        _browserFactory = browserFactory;
        _credentials = credentials;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CreateJobRequest>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.GlassdoorEmail) || string.IsNullOrEmpty(creds.GlassdoorPassword))
        {
            _logger.LogWarning("Glassdoor scraper skipped — credentials not configured");
            return Array.Empty<CreateJobRequest>();
        }

        var jobs = new List<CreateJobRequest>();
        var page = await _browserFactory.NewPageAsync();

        try
        {
            // Login
            await LoginAsync(page, creds.GlassdoorEmail, creds.GlassdoorPassword, ct);

            // Search jobs
            var keyword = Uri.EscapeDataString(keywords);
            var loc = Uri.EscapeDataString(location);
            var searchUrl = $"https://www.glassdoor.com/Job/jobs.htm?sc.keyword={keyword}&locT=C&locKeyword={loc}";

            _logger.LogInformation("Glassdoor scraping URL: {Url}", searchUrl);
            await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);

            // Extract job cards
            var jobCards = await page.QuerySelectorAllAsync("[data-test='jobListing'], .JobsList_jobListItem__wjTHv, li.react-job-listing");
            _logger.LogInformation("Found {Count} job cards on Glassdoor", jobCards.Count);

            foreach (var card in jobCards)
            {
                ct.ThrowIfCancellationRequested();

                var titleEl = await card.QuerySelectorAsync("[data-test='job-title'], a.jobTitle");
                var companyEl = await card.QuerySelectorAsync("[data-test='emp-name'], .EmployerProfile_compactEmployerName__LE242");
                var salaryEl = await card.QuerySelectorAsync("[data-test='detailSalary'], .salary-estimate");

                var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
                var salary = salaryEl != null ? (await salaryEl.InnerTextAsync()).Trim() : null;
                var href = titleEl != null ? await titleEl.GetAttributeAsync("href") : null;

                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                    continue;

                if (!href.StartsWith("http"))
                    href = "https://www.glassdoor.com" + href;

                jobs.Add(new CreateJobRequest(Platform.Glassdoor, title, company, href, Salary: salary));
            }

            // Try next pages (up to 3)
            for (var pageNum = 2; pageNum <= 3; pageNum++)
            {
                ct.ThrowIfCancellationRequested();

                var nextBtn = await page.QuerySelectorAsync("[data-test='pagination-next'], button[aria-label='Next']");
                if (nextBtn == null || !await nextBtn.IsEnabledAsync())
                    break;

                await nextBtn.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(_rateLimitOptions.Value.MaxDelayBetweenRequestsMs, ct);

                var nextCards = await page.QuerySelectorAllAsync("[data-test='jobListing'], .JobsList_jobListItem__wjTHv, li.react-job-listing");

                foreach (var card in nextCards)
                {
                    ct.ThrowIfCancellationRequested();

                    var titleEl = await card.QuerySelectorAsync("[data-test='job-title'], a.jobTitle");
                    var companyEl = await card.QuerySelectorAsync("[data-test='emp-name'], .EmployerProfile_compactEmployerName__LE242");
                    var salaryEl = await card.QuerySelectorAsync("[data-test='detailSalary'], .salary-estimate");

                    var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                    var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
                    var salary = salaryEl != null ? (await salaryEl.InnerTextAsync()).Trim() : null;
                    var href = titleEl != null ? await titleEl.GetAttributeAsync("href") : null;

                    if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                        continue;

                    if (!href.StartsWith("http"))
                        href = "https://www.glassdoor.com" + href;

                    jobs.Add(new CreateJobRequest(Platform.Glassdoor, title, company, href, Salary: salary));
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during Glassdoor scraping");
        }
        finally
        {
            await page.CloseAsync();
        }

        return jobs;
    }

    private async Task LoginAsync(IPage page, string email, string password, CancellationToken ct)
    {
        _logger.LogInformation("Glassdoor: logging in as {Email}", email);

        await page.GotoAsync("https://www.glassdoor.com/profile/login_input.htm", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Email step
        var emailInput = await page.QuerySelectorAsync("#inlineUserEmail, [name='username'], input[type='email']");
        if (emailInput != null)
        {
            await emailInput.FillAsync(email);
            var continueBtn = await page.QuerySelectorAsync("[data-test='email-form-button'], button[type='submit']");
            if (continueBtn != null)
                await continueBtn.ClickAsync();
            await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);
        }

        // Password step
        var passwordInput = await page.QuerySelectorAsync("#inlineUserPassword, [name='password'], input[type='password']");
        if (passwordInput != null)
        {
            await passwordInput.FillAsync(password);
            var signInBtn = await page.QuerySelectorAsync("[data-test='password-form-button'], button[type='submit']");
            if (signInBtn != null)
                await signInBtn.ClickAsync();
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);

        _logger.LogInformation("Glassdoor: login complete");
    }
}
