using JobAgent.Application.Correspondence.DTOs;

namespace JobAgent.Application.Correspondence.Interfaces;

/// <summary>Classifies an email and matches it to a candidate application via the LLM.</summary>
public interface IEmailTriageService
{
    Task<TriageResult> ClassifyAsync(
        EmailMessage email, IReadOnlyList<CandidateApplication> candidates, CancellationToken ct = default);
}
