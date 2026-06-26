using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Application.Jobs.Services;

public class DescriptionService : IDescriptionService
{
    private readonly IEnumerable<IScraper> _scrapers;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<DescriptionService> _logger;

    public DescriptionService(
        IEnumerable<IScraper> scrapers,
        IJobRepository jobRepository,
        ILogger<DescriptionService> logger)
    {
        _scrapers = scrapers;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task FetchAsync(CancellationToken ct = default)
    {
        var jobs = await _jobRepository.GetMissingDescriptionAsync(ct);
        if (jobs.Count == 0)
        {
            _logger.LogInformation("No jobs missing descriptions");
            return;
        }

        _logger.LogInformation("Fetching descriptions for {Count} jobs across {Platforms} platform(s)",
            jobs.Count, jobs.Select(j => j.Platform).Distinct().Count());

        // Fetch each platform's descriptions in parallel — these are independent browser
        // sessions and touch no shared state. Within a platform, requests stay sequential
        // (the scraper rate-limits between them). DB writes are deferred until all fetching
        // completes, since the DbContext is not thread-safe.
        var fetchTasks = jobs
            .GroupBy(j => j.Platform)
            .Select(group => FetchForPlatformAsync(group.Key, group.ToList(), ct))
            .ToList();

        var results = await Task.WhenAll(fetchTasks);

        var updates = results
            .SelectMany(r => r)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        await _jobRepository.UpdateDescriptionsAsync(updates, ct);

        _logger.LogInformation("Persisted {Fetched}/{Total} job descriptions", updates.Count, jobs.Count);
    }

    /// <summary>
    /// Returns a map of jobId → description for the jobs whose detail pages yielded text.
    /// Performs no database access so it is safe to run concurrently with other platforms.
    /// </summary>
    private async Task<IReadOnlyDictionary<int, string>> FetchForPlatformAsync(
        Platform platform, IReadOnlyList<Job> jobs, CancellationToken ct)
    {
        var empty = (IReadOnlyDictionary<int, string>)new Dictionary<int, string>();

        var scraper = _scrapers.FirstOrDefault(s => s.Platform == platform);
        if (scraper is null)
        {
            _logger.LogWarning("No scraper registered for {Platform}; skipping {Count} jobs", platform, jobs.Count);
            return empty;
        }

        // Job.Url is unique, so URL maps back to exactly one job.
        var jobsByUrl = jobs.ToDictionary(j => j.Url, j => j);

        try
        {
            var descriptions = await scraper.FetchDescriptionsAsync(jobsByUrl.Keys.ToList(), ct);

            var updates = new Dictionary<int, string>();
            foreach (var (url, description) in descriptions)
            {
                if (!string.IsNullOrWhiteSpace(description) && jobsByUrl.TryGetValue(url, out var job))
                    updates[job.Id] = description;
            }

            _logger.LogInformation("Fetched {Fetched}/{Total} descriptions from {Platform}",
                updates.Count, jobs.Count, platform);
            return updates;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error fetching descriptions from {Platform}", platform);
            return empty;
        }
    }
}
