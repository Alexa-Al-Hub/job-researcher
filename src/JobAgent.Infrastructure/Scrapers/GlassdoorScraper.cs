using JobAgent.Application.Common;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Abstractions;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using static JobAgent.Infrastructure.Constants.ScraperConstants;

namespace JobAgent.Infrastructure.Scrapers;

public class GlassdoorScraper : BaseScraper
{
    public override Platform Platform => Platform.Glassdoor;
    protected override string DescriptionSelector => GlassdoorDescriptionSelector;

    private readonly IOptions<CredentialOptions> _credentials;

    public GlassdoorScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<GlassdoorScraper> logger)
        : base(browserFactory, rateLimitOptions, logger)
    {
        _credentials = credentials;
    }

    protected override bool CanExecute()
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.GlassdoorEmail) || string.IsNullOrEmpty(creds.GlassdoorPassword))
        {
            Logger.LogWarning("Glassdoor scraper skipped — credentials not configured");
            return false;
        }
        return true;
    }

    protected override async Task BeforeScrapingAsync(IPage page, CancellationToken ct)
    {
        var creds = _credentials.Value;
        Logger.LogInformation("Glassdoor: logging in as {Email}", creds.GlassdoorEmail);

        await page.GotoAsync(GlassdoorLoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var emailInput = await page.QuerySelectorAsync(GlassdoorEmailSelector);
        if (emailInput != null)
        {
            await emailInput.FillAsync(creds.GlassdoorEmail!);
            var continueBtn = await page.QuerySelectorAsync(GlassdoorEmailSubmitSelector);
            if (continueBtn != null)
                await continueBtn.ClickAsync();
            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        }

        var passwordInput = await page.QuerySelectorAsync(GlassdoorPasswordSelector);
        if (passwordInput != null)
        {
            await passwordInput.FillAsync(creds.GlassdoorPassword!);
            var signInBtn = await page.QuerySelectorAsync(GlassdoorPasswordSubmitSelector);
            if (signInBtn != null)
                await signInBtn.ClickAsync();
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        Logger.LogInformation("Glassdoor: login complete");
    }

    protected override async Task<IReadOnlyList<CreateJobRequest>> ParseJobsAsync(
        IPage page, string keywords, string location, CancellationToken ct)
    {
        var searchUrl = $"{GlassdoorBaseUrl}?sc.keyword={Uri.EscapeDataString(keywords)}&locT=C&locKeyword={Uri.EscapeDataString(location)}";
        Logger.LogInformation("Glassdoor scraping URL: {Url}", searchUrl);

        await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        var jobs = new List<CreateJobRequest>();

        for (var pageNum = 1; pageNum <= GlassdoorMaxPages; pageNum++)
        {
            var jobCards = await page.QuerySelectorAllAsync(GlassdoorCardSelector);
            Logger.LogInformation("Found {Count} job cards on Glassdoor (page {Page})", jobCards.Count, pageNum);

            foreach (var card in jobCards)
            {
                ct.ThrowIfCancellationRequested();
                var (title, href, company, salary) = await ExtractCardAsync(card, GlassdoorTitleSelector, GlassdoorCompanySelector, GlassdoorSalarySelector);

                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                    continue;

                if (!href.StartsWith("http"))
                    href = GlassdoorOrigin + href;

                jobs.Add(new CreateJobRequest(Platform.Glassdoor, title, company, href, Salary: salary));
            }

            if (pageNum >= GlassdoorMaxPages) break;

            var nextBtn = await page.QuerySelectorAsync(GlassdoorNextPageSelector);
            if (nextBtn == null || !await nextBtn.IsEnabledAsync())
                break;

            await nextBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        }

        return jobs;
    }
}
