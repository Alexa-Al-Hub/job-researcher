using JobAgent.Application.Applications.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Infrastructure.Appliers;

public class LinkedInApplier : IApplier
{
    public Platform Platform => Platform.LinkedIn;
    private readonly ILogger<LinkedInApplier> _logger;

    public LinkedInApplier(ILogger<LinkedInApplier> logger)
    {
        _logger = logger;
    }

    public Task<bool> ApplyAsync(Job job, CancellationToken ct = default)
    {
        _logger.LogWarning("LinkedIn applier is not yet implemented (needs credentials)");
        return Task.FromResult(false);
    }
}
