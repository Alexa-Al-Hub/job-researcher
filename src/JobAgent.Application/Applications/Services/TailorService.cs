using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Cv.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Applications.Services;

public class TailorService : ITailorService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly ICvTailoringService _cvTailoring;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly ILogger<TailorService> _logger;

    public TailorService(
        IApplicationRepository applicationRepository,
        ICvTailoringService cvTailoring,
        IOptions<AgentOptions> agentOptions,
        ILogger<TailorService> logger)
    {
        _applicationRepository = applicationRepository;
        _cvTailoring = cvTailoring;
        _agentOptions = agentOptions;
        _logger = logger;
    }

    public async Task TailorAsync(User user, CancellationToken ct = default)
    {
        var baseCvPath = _agentOptions.Value.BaseCvPath;
        var foundApps = (await _applicationRepository.GetByStatusAsync(ApplicationStatus.Found, ct))
            .Where(a => a.UserId == user.Id).ToList();
        _logger.LogInformation("Found {Count} applications needing CV tailoring", foundApps.Count);

        foreach (var app in foundApps)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var tailoredPath = await _cvTailoring.TailorAsync(app.Job, baseCvPath, ct);
                app.CvPath = tailoredPath ?? baseCvPath;
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.CvTailored, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tailoring CV for application {AppId}", app.Id);
            }
        }
    }
}
