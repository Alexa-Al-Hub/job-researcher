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

public class LinkedInScraper : BaseScraper
{
    public override Platform Platform => Platform.LinkedIn;

    private readonly IOptions<CredentialOptions> _credentials;

    public LinkedInScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<LinkedInScraper> logger)
        : base(browserFactory, rateLimitOptions, logger)
    {
        _credentials = credentials;
    }

    protected override bool CanExecute()
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.LinkedInEmail) || string.IsNullOrEmpty(creds.LinkedInPassword))
        {
            Logger.LogWarning("LinkedIn scraper skipped — credentials not configured");
            return false;
        }
        return true;
    }

    protected override async Task BeforeScrapingAsync(IPage page, CancellationToken ct)
    {
        var creds = _credentials.Value;
        Logger.LogInformation("LinkedIn: logging in as {Email}", creds.LinkedInEmail);

        await page.GotoAsync(LinkedInLoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.FillAsync(LinkedInUsernameSelector, creds.LinkedInEmail!);
        await page.FillAsync(LinkedInPasswordSelector, creds.LinkedInPassword!);
        await page.ClickAsync(LinkedInSubmitSelector);
        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = LinkedInLoginTimeout });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        Logger.LogInformation("LinkedIn: login successful");
    }

    protected override async Task<IReadOnlyList<CreateJobRequest>> ParseJobsAsync(
        IPage page, string keywords, string location, CancellationToken ct)
    {
        var searchUrl = $"{LinkedInBaseUrl}?keywords={Uri.EscapeDataString(keywords)}&location={Uri.EscapeDataString(location)}&f_TPR=r86400";
        Logger.LogInformation("LinkedIn scraping URL: {Url}", searchUrl);

        await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        for (var i = 0; i < LinkedInScrollCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        }

        var jobCards = await page.QuerySelectorAllAsync(LinkedInCardSelector);
        Logger.LogInformation("Found {Count} job cards on LinkedIn", jobCards.Count);

        var jobs = new List<CreateJobRequest>();
        foreach (var card in jobCards)
        {
            ct.ThrowIfCancellationRequested();
            var (title, href, company, _) = await ExtractCardAsync(card, LinkedInTitleSelector, LinkedInCompanySelector, linkSelector: LinkedInLinkSelector);

            if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                continue;

            if (href.Contains('?'))
                href = href[..href.IndexOf('?')];
            if (!href.StartsWith("http"))
                href = LinkedInOrigin + href;

            jobs.Add(new CreateJobRequest(Platform.LinkedIn, title, company, href));
        }

        return jobs;
    }
}
