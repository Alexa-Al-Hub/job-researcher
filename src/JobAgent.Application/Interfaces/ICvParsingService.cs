using JobAgent.Application.Models;

namespace JobAgent.Application.Interfaces;

public interface ICvParsingService
{
    Task<CvProfile?> ParseAsync(string cvPath, CancellationToken ct = default);
}
