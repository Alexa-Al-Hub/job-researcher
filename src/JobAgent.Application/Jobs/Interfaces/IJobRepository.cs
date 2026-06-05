using JobAgent.Application.Jobs.DTOs;
using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IJobRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task UpdateAsync(Job job, CancellationToken ct = default);
    Task<Job?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<JobDto?> GetByIdAsDtoAsync(int id, CancellationToken ct = default);
}
