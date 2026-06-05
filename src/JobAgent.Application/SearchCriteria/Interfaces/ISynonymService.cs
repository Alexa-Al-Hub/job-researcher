using JobAgent.Domain.Entities;

namespace JobAgent.Application.SearchCriteria.Interfaces;

public interface ISynonymService
{
    Task GenerateAndSaveAsync(User user, IReadOnlyList<string> targetPositions, int? yearsOfExperience = null, CancellationToken ct = default);
}
