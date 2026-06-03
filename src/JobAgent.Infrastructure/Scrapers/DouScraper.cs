using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Scrapers;

public class DouScraper : IScraper
{
    public Platform Platform => Platform.Dou;

    private readonly PlaywrightBrowserFactory _browserFactory;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<DouScraper> _logger;

    public DouScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<DouScraper> logger)
    {
        _browserFactory = browserFactory;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    // TODO: JRC-007 — extract abstract base class with shared browser/pagination logic for all scrapers
    public async Task<IReadOnlyList<Job>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        var jobs = new List<Job>();
        var keyword = Uri.EscapeDataString(keywords);
        var url = $"https://jobs.dou.ua/vacancies/?search={keyword}";

        _logger.LogInformation("DOU scraping URL: {Url}", url);

        var page = await _browserFactory.NewPageAsync();
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".l-vacancy", new PageWaitForSelectorOptions { Timeout = 10000 });

            // Click "More vacancies" button until no more
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var moreButton = await page.QuerySelectorAsync(".more-btn a");
                if (moreButton == null)
                    break;

                var isVisible = await moreButton.IsVisibleAsync();
                if (!isVisible)
                    break;

                await moreButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);
            }

            var vacancyElements = await page.QuerySelectorAllAsync(".l-vacancy");
            _logger.LogInformation("Found {Count} vacancy elements on DOU", vacancyElements.Count);

            foreach (var el in vacancyElements)
            {
                ct.ThrowIfCancellationRequested();

                var titleEl = await el.QuerySelectorAsync(".vt");
                var companyEl = await el.QuerySelectorAsync(".company");
                var salaryEl = await el.QuerySelectorAsync(".salary");

                var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                var href = titleEl != null ? await titleEl.GetAttributeAsync("href") : null;
                var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
                var salary = salaryEl != null ? (await salaryEl.InnerTextAsync()).Trim() : null;

                if (string.IsNullOrEmpty(href))
                    continue;

                jobs.Add(new Job
                {
                    Platform = Platform.Dou,
                    Title = title,
                    Company = company,
                    Url = href,
                    Salary = salary
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during DOU scraping");
        }
        finally
        {
            await page.CloseAsync();
        }

        return jobs;
    }
}
