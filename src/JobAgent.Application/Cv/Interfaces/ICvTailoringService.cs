using JobAgent.Domain.Entities;

namespace JobAgent.Application.Cv.Interfaces;

public interface ICvTailoringService
{
    Task<string?> TailorAsync(Job job, string baseCvPath, CancellationToken ct = default);
}
