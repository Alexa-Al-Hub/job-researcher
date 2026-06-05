using AutoMapper;
using AutoMapper.QueryableExtensions;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace JobAgent.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public JobRepository(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
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

    public async Task<JobDto?> GetByIdAsDtoAsync(int id, CancellationToken ct = default)
    {
        return await _db.Jobs
            .Include(j => j.JobSkills)
                .ThenInclude(js => js.Skill)
            .Where(j => j.Id == id)
            .ProjectTo<JobDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }
}
