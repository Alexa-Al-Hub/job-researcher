using JobAgent.Domain.Entities;

namespace JobAgent.Application.Users.Interfaces;

public interface ICvSyncService
{
    Task<User?> ParseAndSyncAsync(string baseCvPath, CancellationToken ct = default);
}
