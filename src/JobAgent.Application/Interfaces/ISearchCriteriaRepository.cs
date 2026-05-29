using JobAgent.Domain.Entities;

namespace JobAgent.Application.Interfaces;

public interface ISearchCriteriaRepository
{
    Task<List<SearchCriteria>> GetActiveAsync(CancellationToken ct = default);
    Task<SearchCriteria?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(SearchCriteria criteria, CancellationToken ct = default);
}
