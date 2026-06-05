using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using JobAgent.Persistence.Context;

namespace JobAgent.Persistence.Repositories;

public class SearchCriteriaRepository : ISearchCriteriaRepository
{
    private readonly AppDbContext _db;

    public SearchCriteriaRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SearchCriteria>> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.SearchCriteria
            .Where(sc => sc.IsActive)
            .ToListAsync(ct);
    }

    public async Task<SearchCriteria?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SearchCriteria.FirstOrDefaultAsync(sc => sc.Id == id, ct);
    }

    public async Task AddAsync(SearchCriteria criteria, CancellationToken ct = default)
    {
        _db.SearchCriteria.Add(criteria);
        await _db.SaveChangesAsync(ct);
    }
}
