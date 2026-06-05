using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Applications.Services;

public class ApplyService : IApplyService
{
    private readonly IEnumerable<IApplier> _appliers;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<ApplyService> _logger;

    public ApplyService(
        IEnumerable<IApplier> appliers,
        IApplicationRepository applicationRepository,
        IOptions<AgentOptions> agentOptions,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<ApplyService> logger)
    {
        _appliers = appliers;
        _applicationRepository = applicationRepository;
        _agentOptions = agentOptions;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    public async Task ApplyAsync(User user, CancellationToken ct = default)
    {
        var options = _agentOptions.Value;
        var enabledPlatforms = options.EnabledPlatforms;

        var todayCount = await _applicationRepository.GetTodayApplicationCountAsync(user.Id, ct);
        var remaining = options.MaxApplicationsPerDay - todayCount;

        if (remaining <= 0)
        {
            _logger.LogWarning("Daily application limit reached ({Max})", options.MaxApplicationsPerDay);
            return;
        }

        var appsToApply = await _applicationRepository.GetByStatusAsync(ApplicationStatus.CvTailored, ct);
        var activeAppliers = _appliers.Where(a => enabledPlatforms.Contains(a.Platform))
            .ToDictionary(a => a.Platform);

        var applied = 0;
        foreach (var app in appsToApply)
        {
            if (applied >= remaining)
            {
                _logger.LogWarning("Reached daily limit during apply phase");
                break;
            }

            ct.ThrowIfCancellationRequested();

            if (!activeAppliers.TryGetValue(app.Job.Platform, out var applier))
            {
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
                continue;
            }

            if (options.DryRun)
            {
                _logger.LogInformation("[DRY RUN] Would apply to: {Title} at {Company} ({Platform})",
                    app.Job.Title, app.Job.Company, app.Job.Platform);
                continue;
            }

            try
            {
                var success = await applier.ApplyAsync(app.Job, ct);
                if (success)
                {
                    app.AppliedAt = DateTime.UtcNow;
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.Applied, ct);
                    applied++;
                }
                else
                {
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
                }

                await Task.Delay(_rateLimitOptions.Value.DelayBetweenApplicationsMs, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying to job {JobId}", app.JobId);
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
            }
        }

        _logger.LogInformation("Apply phase complete. Applied: {Count}, Total today: {Total}",
            applied, todayCount + applied);
    }
}
