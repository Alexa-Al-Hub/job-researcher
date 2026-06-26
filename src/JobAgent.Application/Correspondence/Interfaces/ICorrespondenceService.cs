using JobAgent.Domain.Entities;

namespace JobAgent.Application.Correspondence.Interfaces;

/// <summary>
/// Phase 4 of the pipeline: pull new mail from the configured mailboxes, classify it,
/// match it to the user's open applications, and update their status.
/// </summary>
public interface ICorrespondenceService
{
    Task SyncAsync(User user, CancellationToken ct = default);
}
