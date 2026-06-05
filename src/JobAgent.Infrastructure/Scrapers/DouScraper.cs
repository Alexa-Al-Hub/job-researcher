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

public class DouScraper : BaseScraper
{
    public override Platform Platform => Platform.Dou;

    public DouScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<DouScraper> logger)
        : base(browserFactory, rateLimitOptions, logger) { }

    protected override async Task<IReadOnlyList<CreateJobRequest>> ParseJobsAsync(
        IPage page, string keywords, string location, CancellationToken ct)
    {
        var url = DouBaseUrl + Uri.EscapeDataString(keywords);
        Logger.LogInformation("DOU scraping URL: {Url}", url);

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(DouVacancySelector, new PageWaitForSelectorOptions { Timeout = DouWaitTimeout });

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var moreButton = await page.QuerySelectorAsync(DouMoreButtonSelector);
            if (moreButton == null || !await moreButton.IsVisibleAsync())
                break;

            await moreButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        }

        var vacancyElements = await page.QuerySelectorAllAsync(DouVacancySelector);
        Logger.LogInformation("Found {Count} vacancy elements on DOU", vacancyElements.Count);

        var jobs = new List<CreateJobRequest>();
        foreach (var el in vacancyElements)
        {
            ct.ThrowIfCancellationRequested();
            var (title, href, company, salary) = await ExtractCardAsync(el, DouTitleSelector, DouCompanySelector, DouSalarySelector);

            if (string.IsNullOrEmpty(href))
                continue;

            jobs.Add(new CreateJobRequest(Platform.Dou, title, company, href, Salary: salary));
        }

        return jobs;
    }
}
