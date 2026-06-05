using JobAgent.Application.Applications.DTOs;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.Interfaces;

public interface IApplicationRepository
{
    Task<Domain.Entities.Application> CreateForJobAsync(int jobId, int userId, CancellationToken ct = default);
    Task UpdateStatusAsync(Domain.Entities.Application application, ApplicationStatus newStatus, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDto>> GetByStatusAsDtoAsync(ApplicationStatus status, CancellationToken ct = default);
    Task<int> GetTodayApplicationCountAsync(int userId, CancellationToken ct = default);
    Task<bool> ExistsForJobAndUserAsync(int jobId, int userId, CancellationToken ct = default);
}
