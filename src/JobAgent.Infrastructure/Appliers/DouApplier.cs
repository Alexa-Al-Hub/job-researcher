using JobAgent.Application.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Infrastructure.Appliers;

public class DouApplier : IApplier
{
    public Platform Platform => Platform.Dou;
    private readonly ILogger<DouApplier> _logger;

    public DouApplier(ILogger<DouApplier> logger)
    {
        _logger = logger;
    }

    public Task<bool> ApplyAsync(Job job, CancellationToken ct = default)
    {
        // DOU redirects to external company sites — mark for manual follow-up
        _logger.LogInformation("DOU job requires manual application: {Title} at {Company} — {Url}",
            job.Title, job.Company, job.Url);
        return Task.FromResult(false);
    }
}
