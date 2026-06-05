using JobAgent.Application.Jobs.DTOs;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IScraper
{
    Platform Platform { get; }
    Task<IReadOnlyList<CreateJobRequest>> ScrapeAsync(string keywords, string location, CancellationToken ct = default);
}
