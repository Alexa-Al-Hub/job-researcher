using JobAgent.Domain.Entities;

namespace JobAgent.Application.Correspondence.Interfaces;

public interface ICorrespondenceRepository
{
    Task<bool> ExistsAsync(string mailbox, string externalMessageId, CancellationToken ct = default);
    Task AddAsync(CorrespondenceMessage message, CancellationToken ct = default);

    /// <summary>The most recent message timestamp for a mailbox, for incremental sync.</summary>
    Task<DateTime?> GetLastReceivedAtAsync(string mailbox, CancellationToken ct = default);
}
