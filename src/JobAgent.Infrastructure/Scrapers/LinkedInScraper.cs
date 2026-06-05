using JobAgent.Application.Common;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Scrapers;

public class LinkedInScraper : IScraper
{
    public Platform Platform => Platform.LinkedIn;

    private readonly PlaywrightBrowserFactory _browserFactory;
    private readonly IOptions<CredentialOptions> _credentials;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<LinkedInScraper> _logger;

    public LinkedInScraper(
        PlaywrightBrowserFactory browserFactory,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<LinkedInScraper> logger)
    {
        _browserFactory = browserFactory;
        _credentials = credentials;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CreateJobRequest>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.LinkedInEmail) || string.IsNullOrEmpty(creds.LinkedInPassword))
        {
            _logger.LogWarning("LinkedIn scraper skipped — credentials not configured");
            return Array.Empty<CreateJobRequest>();
        }

        var jobs = new List<CreateJobRequest>();
        var page = await _browserFactory.NewPageAsync();

        try
        {
            // Login
            await LoginAsync(page, creds.LinkedInEmail, creds.LinkedInPassword, ct);

            // Search jobs
            var keyword = Uri.EscapeDataString(keywords);
            var loc = Uri.EscapeDataString(location);
            var searchUrl = $"https://www.linkedin.com/jobs/search/?keywords={keyword}&location={loc}&f_TPR=r86400";

            _logger.LogInformation("LinkedIn scraping URL: {Url}", searchUrl);
            await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);

            // Scroll to load more results
            for (var i = 0; i < 3; i++)
            {
                ct.ThrowIfCancellationRequested();
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);
            }

            // Extract job cards
            var jobCards = await page.QuerySelectorAllAsync(".jobs-search-results__list-item, .job-card-container");
            _logger.LogInformation("Found {Count} job cards on LinkedIn", jobCards.Count);

            foreach (var card in jobCards)
            {
                ct.ThrowIfCancellationRequested();

                var titleEl = await card.QuerySelectorAsync(".job-card-list__title, .job-card-container__link");
                var companyEl = await card.QuerySelectorAsync(".job-card-container__primary-description, .artdeco-entity-lockup__subtitle");
                var linkEl = await card.QuerySelectorAsync("a.job-card-list__title, a.job-card-container__link");

                var title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "";
                var company = companyEl != null ? (await companyEl.InnerTextAsync()).Trim() : "";
                var href = linkEl != null ? await linkEl.GetAttributeAsync("href") : null;

                if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(title))
                    continue;

                // Normalize URL — strip tracking params
                if (href.Contains('?'))
                    href = href[..href.IndexOf('?')];
                if (!href.StartsWith("http"))
                    href = "https://www.linkedin.com" + href;

                jobs.Add(new CreateJobRequest
                {
                    Platform = Platform.LinkedIn,
                    Title = title,
                    Company = company,
                    Url = href
                });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during LinkedIn scraping");
        }
        finally
        {
            await page.CloseAsync();
        }

        return jobs;
    }

    private async Task LoginAsync(IPage page, string email, string password, CancellationToken ct)
    {
        _logger.LogInformation("LinkedIn: logging in as {Email}", email);

        await page.GotoAsync("https://www.linkedin.com/login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await page.FillAsync("#username", email);
        await page.FillAsync("#password", password);
        await page.ClickAsync("[data-litms-control-urn='login-submit']");

        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = 30000 });
        await Task.Delay(_rateLimitOptions.Value.MinDelayBetweenRequestsMs, ct);

        _logger.LogInformation("LinkedIn: login successful");
    }
}
