using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;

namespace JobAgent.Application.Interfaces;

public interface IApplier
{
    Platform Platform { get; }
    Task<bool> ApplyAsync(Job job, CancellationToken ct = default);
}
