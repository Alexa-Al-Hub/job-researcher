namespace JobAgent.Application.Jobs.Interfaces;

/// <summary>
/// Phase 1.5 of the discovery pipeline: enriches freshly scraped jobs with the full
/// description text from their detail pages, before scoring and tailoring run.
/// </summary>
public interface IDescriptionService
{
    Task FetchAsync(CancellationToken ct = default);
}
