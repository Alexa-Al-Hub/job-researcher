using JobAgent.Domain.Entities;

namespace JobAgent.Application.SearchCriteria.Interfaces;

public interface ISearchCriteriaRepository
{
    Task<List<Domain.Entities.SearchCriteria>> GetActiveAsync(CancellationToken ct = default);
    Task<Domain.Entities.SearchCriteria?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(Domain.Entities.SearchCriteria criteria, CancellationToken ct = default);
}
