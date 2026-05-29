using JobAgent.Application.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobAgent.Infrastructure.Persistence;

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default)
    {
        return await _db.Jobs.AnyAsync(j => j.Url == url, ct);
    }

    public async Task AddAsync(Job job, CancellationToken ct = default)
    {
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Job job, CancellationToken ct = default)
    {
        _db.Jobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Job?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);
    }
}
