using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IJobScoringService
{
    Task<(int Score, string Reason)> ScoreAsync(Job job, IReadOnlyList<Skill> userSkills, CancellationToken ct = default);
}
