using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Scrapers;

public class IndeedScraper : IScraper
{
    public Platform Platform => Platform.Indeed;

    private readonly PlaywrightBrowserFactory _browserFactory;
    private readonly IOptions<SearchOptions> _searchOptions;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<IndeedScraper> _logger;

    public IndeedScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<SearchOptions> searchOptions,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<IndeedScraper> logger)
    {
        _browserFactory = browserFactory;
        _searchOptions = searchOptions;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Job>> ScrapeAsync(CancellationToken ct = default)
    {
        var jobs = new List<Job>();
        var search = _searchOptions.Value;
        var keyword = Uri.EscapeDataString(search.Keywords);
        var location = Uri.EscapeDataString(search.Location);
        var url = $"https://www.indeed.com/jobs?q={keyword}&l={location}";

        _logger.LogInformation("Indeed scraping URL: {Url}", url);

        var page = await _browserFactory.NewPageAsync();
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);

            var jobCards = await page.QuerySelectorAllAsync(".job_seen_beacon, .jobsearch-ResultsList > li");
            _logger.LogInformation("Found {Count} job cards on Indeed", jobCards.Count);

            foreach (var card in jobCards)
            {
                ct.ThrowIfCancellationRequested();

                var titleEl = await card.QuerySelectorAsync("h2.jobTitle a, .jobTitle > a");
                var companyEl = await card.QuerySelectorAsync("[data-testid='company-name'], .companyName");
                var salaryEl = await card.QuerySelectorAsync("[data-testid='attribute_snippet_testid'], .salary-snippet-container");

                var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                var href = titleEl != null ? await titleEl.GetAttributeAsync("href") : null;
                var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
                var salary = salaryEl != null ? (await salaryEl.InnerTextAsync()).Trim() : null;

                if (string.IsNullOrEmpty(href))
                    continue;

                if (!href.StartsWith("http"))
                    href = "https://www.indeed.com" + href;

                jobs.Add(new Job
                {
                    Platform = Platform.Indeed,
                    Title = title,
                    Company = company,
                    Url = href,
                    Salary = salary,
                    Status = JobStatus.Found
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during Indeed scraping (likely anti-bot block)");
        }
        finally
        {
            await page.CloseAsync();
        }

        return jobs;
    }
}
