using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Interfaces;

public interface IScraper
{
    Platform Platform { get; }
    Task<IReadOnlyList<Job>> ScrapeAsync(string keywords, string location, CancellationToken ct = default);
}
