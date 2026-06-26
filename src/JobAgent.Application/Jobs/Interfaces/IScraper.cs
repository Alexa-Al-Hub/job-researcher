using JobAgent.Application.Jobs.DTOs;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.Interfaces;

public interface IScraper
{
    Platform Platform { get; }
    Task<IReadOnlyList<CreateJobRequest>> ScrapeAsync(string keywords, string location, CancellationToken ct = default);

    /// <summary>
    /// Visits each job detail page and extracts its full description text.
    /// Returns a map of job URL → description (entries are omitted when extraction fails).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> FetchDescriptionsAsync(
        IReadOnlyList<string> urls, CancellationToken ct = default);
}
