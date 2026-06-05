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

public class IndeedScraper : BaseScraper
{
    public override Platform Platform => Platform.Indeed;

    public IndeedScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<IndeedScraper> logger)
        : base(browserFactory, rateLimitOptions, logger) { }

    protected override async Task<IReadOnlyList<CreateJobRequest>> ParseJobsAsync(
        IPage page, string keywords, string location, CancellationToken ct)
    {
        var url = $"{IndeedBaseUrl}?q={Uri.EscapeDataString(keywords)}&l={Uri.EscapeDataString(location)}";
        Logger.LogInformation("Indeed scraping URL: {Url}", url);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);

        var jobCards = await page.QuerySelectorAllAsync(IndeedCardSelector);
        Logger.LogInformation("Found {Count} job cards on Indeed", jobCards.Count);

        var jobs = new List<CreateJobRequest>();
        foreach (var card in jobCards)
        {
            ct.ThrowIfCancellationRequested();
            var (title, href, company, salary) = await ExtractCardAsync(card, IndeedTitleSelector, IndeedCompanySelector, IndeedSalarySelector);

            if (string.IsNullOrEmpty(href))
                continue;

            if (!href.StartsWith("http"))
                href = IndeedOrigin + href;

            jobs.Add(new CreateJobRequest(Platform.Indeed, title, company, href, Salary: salary));
        }

        return jobs;
    }
}
