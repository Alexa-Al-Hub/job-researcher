using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Skills.Interfaces;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync(CancellationToken ct = default);
    Task<Skill?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Skill> skills, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<Skill> skills, CancellationToken ct = default);
    Task<List<Skill>> GetBySourceAsync(SkillSource source, CancellationToken ct = default);
}
