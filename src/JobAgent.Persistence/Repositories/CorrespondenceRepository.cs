using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace JobAgent.Persistence.Repositories;

public class CorrespondenceRepository : ICorrespondenceRepository
{
    private readonly AppDbContext _db;

    public CorrespondenceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(string mailbox, string externalMessageId, CancellationToken ct = default)
    {
        return _db.Correspondence.AnyAsync(
            c => c.Mailbox == mailbox && c.ExternalMessageId == externalMessageId, ct);
    }

    public async Task AddAsync(CorrespondenceMessage message, CancellationToken ct = default)
    {
        _db.Correspondence.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> GetLastReceivedAtAsync(string mailbox, CancellationToken ct = default)
    {
        var query = _db.Correspondence.Where(c => c.Mailbox == mailbox);
        if (!await query.AnyAsync(ct))
            return null;

        return await query.MaxAsync(c => (DateTime?)c.ReceivedAt, ct);
    }
}
