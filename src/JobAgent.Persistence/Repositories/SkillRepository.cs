using JobAgent.Application.Skills.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.EntityFrameworkCore;

using JobAgent.Persistence.Context;

namespace JobAgent.Persistence.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly AppDbContext _db;

    public SkillRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Skill>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Skills.ToListAsync(ct);
    }

    public async Task<Skill?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _db.Skills.FirstOrDefaultAsync(s => s.Name == name, ct);
    }

    public async Task AddRangeAsync(IEnumerable<Skill> skills, CancellationToken ct = default)
    {
        _db.Skills.AddRange(skills);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveRangeAsync(IEnumerable<Skill> skills, CancellationToken ct = default)
    {
        _db.Skills.RemoveRange(skills);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Skill>> GetBySourceAsync(SkillSource source, CancellationToken ct = default)
    {
        return await _db.Skills.Where(s => s.Source == source).ToListAsync(ct);
    }
}
