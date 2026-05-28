using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Services;

public class OrchestratorService : IOrchestrator
{
    private readonly IEnumerable<IScraper> _scrapers;
    private readonly IEnumerable<IApplier> _appliers;
    private readonly IJobRepository _repository;
    private readonly ICvTailoringService _cvTailoring;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        IEnumerable<IScraper> scrapers,
        IEnumerable<IApplier> appliers,
        IJobRepository repository,
        ICvTailoringService cvTailoring,
        IOptions<AgentOptions> agentOptions,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<OrchestratorService> logger)
    {
        _scrapers = scrapers;
        _appliers = appliers;
        _repository = repository;
        _cvTailoring = cvTailoring;
        _agentOptions = agentOptions;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var options = _agentOptions.Value;
        var enabledPlatforms = options.EnabledPlatforms;

        _logger.LogInformation("Starting orchestrator. DryRun={DryRun}, Platforms={Platforms}",
            options.DryRun, string.Join(", ", enabledPlatforms));

        // Phase 1: Scrape
        var activeScrapers = _scrapers.Where(s => enabledPlatforms.Contains(s.Platform));
        foreach (var scraper in activeScrapers)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("Scraping {Platform}...", scraper.Platform);

            try
            {
                var jobs = await scraper.ScrapeAsync(ct);
                var newCount = 0;

                foreach (var job in jobs)
                {
                    if (await _repository.ExistsByUrlAsync(job.Url, ct))
                        continue;

                    await _repository.AddAsync(job, ct);
                    newCount++;
                }

                _logger.LogInformation("Scraped {Total} jobs from {Platform}, {New} new",
                    jobs.Count, scraper.Platform, newCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping {Platform}", scraper.Platform);
            }
        }

        // Phase 2: Tailor CVs
        var foundJobs = await _repository.GetByStatusAsync(JobStatus.Found, ct);
        _logger.LogInformation("Found {Count} jobs needing CV tailoring", foundJobs.Count);

        foreach (var job in foundJobs)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var tailoredPath = await _cvTailoring.TailorAsync(job, options.BaseCvPath, ct);
                if (tailoredPath != null)
                {
                    job.CvPath = tailoredPath;
                    job.Status = JobStatus.CvTailored;
                }
                else
                {
                    job.CvPath = options.BaseCvPath;
                    job.Status = JobStatus.CvTailored;
                }
                await _repository.UpdateAsync(job, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tailoring CV for job {JobId}", job.Id);
            }
        }

        // Phase 3: Apply
        var todayCount = await _repository.GetTodayApplicationCountAsync(ct);
        var remaining = options.MaxApplicationsPerDay - todayCount;

        if (remaining <= 0)
        {
            _logger.LogWarning("Daily application limit reached ({Max})", options.MaxApplicationsPerDay);
            return;
        }

        var jobsToApply = await _repository.GetByStatusAsync(JobStatus.CvTailored, ct);
        var activeAppliers = _appliers.Where(a => enabledPlatforms.Contains(a.Platform))
            .ToDictionary(a => a.Platform);

        var applied = 0;
        foreach (var job in jobsToApply)
        {
            if (applied >= remaining)
            {
                _logger.LogWarning("Reached daily limit during apply phase");
                break;
            }

            ct.ThrowIfCancellationRequested();

            if (!activeAppliers.TryGetValue(job.Platform, out var applier))
            {
                job.Status = JobStatus.ManualFollowUp;
                await _repository.UpdateAsync(job, ct);
                continue;
            }

            if (options.DryRun)
            {
                _logger.LogInformation("[DRY RUN] Would apply to: {Title} at {Company} ({Platform})",
                    job.Title, job.Company, job.Platform);
                continue;
            }

            try
            {
                var success = await applier.ApplyAsync(job, ct);
                job.Status = success ? JobStatus.Applied : JobStatus.ApplyFailed;
                job.AppliedAt = success ? DateTime.UtcNow : null;
                await _repository.UpdateAsync(job, ct);

                if (success) applied++;

                await Task.Delay(_rateLimitOptions.Value.DelayBetweenApplicationsMs, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying to job {JobId}", job.Id);
                job.Status = JobStatus.ApplyFailed;
                await _repository.UpdateAsync(job, ct);
            }
        }

        _logger.LogInformation("Orchestrator complete. Applied to {Count} jobs today (total: {Total})",
            applied, todayCount + applied);
    }
}
