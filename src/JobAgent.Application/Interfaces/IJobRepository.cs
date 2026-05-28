using JobAgent.Domain.Entities;

namespace JobAgent.Application.Interfaces;

public interface IJobRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task AddAsync(Job job, CancellationToken ct = default);
    Task UpdateAsync(Job job, CancellationToken ct = default);
    Task<IReadOnlyList<Job>> GetByStatusAsync(Domain.Enums.JobStatus status, CancellationToken ct = default);
    Task<int> GetTodayApplicationCountAsync(CancellationToken ct = default);
}
