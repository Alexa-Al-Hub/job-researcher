using JobAgent.Domain.Entities;

namespace JobAgent.Application.Applications.Interfaces;

/// <summary>Answers a single application screening question from the applicant profile.</summary>
public interface IApplicationFormService
{
    /// <summary>
    /// Returns a truthful answer to <paramref name="question"/>, choosing from
    /// <paramref name="options"/> when provided. Returns null when it cannot answer
    /// confidently — the caller should then stop rather than submit a guess.
    /// </summary>
    Task<string?> AnswerAsync(
        string question, IReadOnlyList<string>? options, Job job, CancellationToken ct = default);
}
