using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Correspondence.DTOs;
using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Correspondence.Services;

public class CorrespondenceService : ICorrespondenceService
{
    /// <summary>Minimum LLM confidence before an email is allowed to change an application's status.</summary>
    private const int MatchConfidenceThreshold = 70;

    private readonly IEmailClient _emailClient;
    private readonly IEmailTriageService _triageService;
    private readonly ICorrespondenceRepository _correspondenceRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IOptions<GmailOptions> _gmailOptions;
    private readonly ILogger<CorrespondenceService> _logger;

    public CorrespondenceService(
        IEmailClient emailClient,
        IEmailTriageService triageService,
        ICorrespondenceRepository correspondenceRepository,
        IApplicationRepository applicationRepository,
        IOptions<GmailOptions> gmailOptions,
        ILogger<CorrespondenceService> logger)
    {
        _emailClient = emailClient;
        _triageService = triageService;
        _correspondenceRepository = correspondenceRepository;
        _applicationRepository = applicationRepository;
        _gmailOptions = gmailOptions;
        _logger = logger;
    }

    public async Task SyncAsync(User user, CancellationToken ct = default)
    {
        var options = _gmailOptions.Value;
        if (options.Accounts.Count == 0)
        {
            _logger.LogInformation("No Gmail accounts configured. Skipping correspondence sync.");
            return;
        }

        // An inbound email is most likely a reply to an application that's already out.
        var openApps = (await _applicationRepository.GetByStatusAsync(ApplicationStatus.Applied, ct))
            .Concat(await _applicationRepository.GetByStatusAsync(ApplicationStatus.InterviewInvite, ct))
            .Where(a => a.UserId == user.Id)
            .ToList();

        var appsById = openApps.ToDictionary(a => a.Id);
        var candidates = openApps
            .Select(a => new CandidateApplication(a.Id, a.Job.Company, a.Job.Title, a.Job.Platform.ToString()))
            .ToList();

        _logger.LogInformation("Syncing correspondence for {Count} mailbox(es) against {Open} open application(s)",
            options.Accounts.Count, candidates.Count);

        foreach (var mailbox in options.Accounts)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await SyncMailboxAsync(mailbox, candidates, appsById, options.LookbackDays, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing correspondence for {Mailbox}", mailbox);
            }
        }
    }

    private async Task SyncMailboxAsync(
        string mailbox,
        IReadOnlyList<CandidateApplication> candidates,
        IReadOnlyDictionary<int, Domain.Entities.Application> appsById,
        int lookbackDays,
        CancellationToken ct)
    {
        var since = await _correspondenceRepository.GetLastReceivedAtAsync(mailbox, ct)
                    ?? DateTime.UtcNow.AddDays(-lookbackDays);

        var emails = await _emailClient.FetchSinceAsync(mailbox, since, ct);
        _logger.LogInformation("Fetched {Count} email(s) from {Mailbox} since {Since:u}", emails.Count, mailbox, since);

        var processed = 0;
        foreach (var email in emails)
        {
            ct.ThrowIfCancellationRequested();

            if (await _correspondenceRepository.ExistsAsync(email.Mailbox, email.ExternalMessageId, ct))
                continue;

            var triage = await _triageService.ClassifyAsync(email, candidates, ct);

            var matchedId = triage.MatchedApplicationId is { } id
                            && triage.Confidence >= MatchConfidenceThreshold
                            && appsById.ContainsKey(id)
                ? id
                : (int?)null;

            await _correspondenceRepository.AddAsync(new CorrespondenceMessage
            {
                Mailbox = email.Mailbox,
                ExternalMessageId = email.ExternalMessageId,
                ThreadId = email.ThreadId,
                ApplicationId = matchedId,
                FromAddress = email.From,
                Subject = email.Subject,
                Body = email.Body,
                ReceivedAt = email.ReceivedAt,
                Direction = CorrespondenceDirection.Inbound,
                Category = triage.Category,
                MatchConfidence = triage.Confidence,
                SuggestedReply = triage.SuggestedReply
            }, ct);
            processed++;

            if (matchedId is { } matched)
                await ApplyStatusTransitionAsync(appsById[matched], triage.Category, ct);
        }

        _logger.LogInformation("Processed {Count} new message(s) from {Mailbox}", processed, mailbox);
    }

    private async Task ApplyStatusTransitionAsync(
        Domain.Entities.Application app, CorrespondenceCategory category, CancellationToken ct)
    {
        var newStatus = category switch
        {
            CorrespondenceCategory.InterviewInvite => ApplicationStatus.InterviewInvite,
            CorrespondenceCategory.Rejection => ApplicationStatus.Rejected,
            CorrespondenceCategory.Question => ApplicationStatus.ManualFollowUp,
            _ => (ApplicationStatus?)null
        };

        if (newStatus is null || app.Status == newStatus.Value)
            return;

        await _applicationRepository.UpdateStatusAsync(app, newStatus.Value, ct);
        _logger.LogInformation("Application {AppId} ({Title} at {Company}) -> {Status} from email",
            app.Id, app.Job.Title, app.Job.Company, newStatus.Value);
    }
}
