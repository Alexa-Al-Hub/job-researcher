using JobAgent.Domain.Entities;

namespace JobAgent.Application.Applications.Interfaces;

public interface ITailorService
{
    Task TailorAsync(User user, CancellationToken ct = default);
}
