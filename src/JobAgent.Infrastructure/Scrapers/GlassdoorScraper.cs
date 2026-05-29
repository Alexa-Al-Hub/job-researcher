using JobAgent.Application.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Infrastructure.Scrapers;

public class GlassdoorScraper : IScraper
{
    public Platform Platform => Platform.Glassdoor;
    private readonly ILogger<GlassdoorScraper> _logger;

    public GlassdoorScraper(ILogger<GlassdoorScraper> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<Job>> ScrapeAsync(string keywords, string location, CancellationToken ct = default)
    {
        _logger.LogWarning("Glassdoor scraper is not yet implemented (needs credentials)");
        return Task.FromResult<IReadOnlyList<Job>>(Array.Empty<Job>());
    }
}
