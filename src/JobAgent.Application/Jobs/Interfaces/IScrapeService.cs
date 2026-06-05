using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IScrapeService
{
    Task ScrapeAsync(User user, CancellationToken ct = default);
}
