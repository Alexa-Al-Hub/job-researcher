using JobAgent.Application.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Infrastructure.Appliers;

public class GlassdoorApplier : IApplier
{
    public Platform Platform => Platform.Glassdoor;
    private readonly ILogger<GlassdoorApplier> _logger;

    public GlassdoorApplier(ILogger<GlassdoorApplier> logger)
    {
        _logger = logger;
    }

    public Task<bool> ApplyAsync(Job job, CancellationToken ct = default)
    {
        _logger.LogWarning("Glassdoor applier is not yet implemented (needs credentials)");
        return Task.FromResult(false);
    }
}
