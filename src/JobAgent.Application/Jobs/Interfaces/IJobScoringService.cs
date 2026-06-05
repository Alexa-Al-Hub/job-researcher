using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IJobScoringService
{
    Task<(int Score, string Reason)> ScoreAsync(Job job, IReadOnlyList<Skill> userSkills, int? yearsOfExperience = null, CancellationToken ct = default);
}
