using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.Interfaces;

public interface IApplier
{
    Platform Platform { get; }

    /// <summary>
    /// Attempts to submit an application for <paramref name="job"/>, uploading the
    /// tailored CV at <paramref name="cvPath"/>. Returns true only if the application
    /// was actually submitted; false routes the application to manual follow-up.
    /// </summary>
    Task<bool> ApplyAsync(Job job, string? cvPath, CancellationToken ct = default);
}
