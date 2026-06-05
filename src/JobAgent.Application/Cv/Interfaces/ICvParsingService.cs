using JobAgent.Application.Cv.Models;

namespace JobAgent.Application.Cv.Interfaces;

public interface ICvParsingService
{
    Task<CvProfile?> ParseAsync(string cvPath, CancellationToken ct = default);
}
