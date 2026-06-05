using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.Skills.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Jobs.Services;

public class ScoringService : IScoringService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IJobScoringService _jobScoring;
    private readonly ISkillRepository _skillRepository;
    private readonly IOptions<ScoringOptions> _scoringOptions;
    private readonly ILogger<ScoringService> _logger;

    public ScoringService(
        IApplicationRepository applicationRepository,
        IJobScoringService jobScoring,
        ISkillRepository skillRepository,
        IOptions<ScoringOptions> scoringOptions,
        ILogger<ScoringService> logger)
    {
        _applicationRepository = applicationRepository;
        _jobScoring = jobScoring;
        _skillRepository = skillRepository;
        _scoringOptions = scoringOptions;
        _logger = logger;
    }

    public async Task ScoreAsync(User user, CancellationToken ct = default)
    {
        var options = _scoringOptions.Value;
        var foundApps = (await _applicationRepository.GetByStatusAsync(ApplicationStatus.Found, ct))
            .Where(a => a.UserId == user.Id).ToList();

        if (foundApps.Count == 0)
        {
            _logger.LogInformation("No applications to score");
            return;
        }

        var userSkills = await _skillRepository.GetByUserIdAsync(user.Id, ct);
        _logger.LogInformation("Scoring {Count} applications against {SkillCount} user skills (threshold: {Threshold})",
            foundApps.Count, userSkills.Count, options.MinScoreThreshold);

        foreach (var app in foundApps)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var (score, reason) = await _jobScoring.ScoreAsync(app.Job, userSkills, user.YearsOfExperience, ct);
                app.Score = score;
                app.ScoreReason = reason;

                if (score >= options.MinScoreThreshold)
                {
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.Scored, ct);
                    _logger.LogInformation("Scored {Score}/100 — {Title} at {Company}: {Reason}",
                        score, app.Job.Title, app.Job.Company, reason);
                }
                else
                {
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.Skipped, ct);
                    _logger.LogInformation("Skipped ({Score}/100) — {Title} at {Company}: {Reason}",
                        score, app.Job.Title, app.Job.Company, reason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scoring application {AppId}", app.Id);
            }
        }
    }
}
