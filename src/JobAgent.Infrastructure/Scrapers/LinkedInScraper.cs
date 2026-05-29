using JobAgent.Application.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Infrastructure.Scrapers;

public class LinkedInScraper : IScraper
{
    public Platform Platform => Platform.LinkedIn;
    private readonly ILogger<LinkedInScraper> _logger;

    public LinkedInScraper(ILogger<LinkedInScraper> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Job>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        _logger.LogWarning("LinkedIn scraper is not yet implemented (needs credentials)");
        return Task.FromResult<IReadOnlyList<Job>>(Array.Empty<Job>());
    }
}
