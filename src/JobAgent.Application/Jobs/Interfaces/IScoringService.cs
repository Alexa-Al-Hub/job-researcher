using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IScoringService
{
    Task ScoreAsync(User user, CancellationToken ct = default);
}
