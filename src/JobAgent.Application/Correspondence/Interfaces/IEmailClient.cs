using JobAgent.Application.Correspondence.DTOs;

namespace JobAgent.Application.Correspondence.Interfaces;

/// <summary>Read-only access to a mailbox. There is intentionally no send capability.</summary>
public interface IEmailClient
{
    Task<IReadOnlyList<EmailMessage>> FetchSinceAsync(string mailbox, DateTime since, CancellationToken ct = default);
}
