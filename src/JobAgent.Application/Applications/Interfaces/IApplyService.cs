using JobAgent.Domain.Entities;

namespace JobAgent.Application.Applications.Interfaces;

public interface IApplyService
{
    Task ApplyAsync(User user, CancellationToken ct = default);
}
