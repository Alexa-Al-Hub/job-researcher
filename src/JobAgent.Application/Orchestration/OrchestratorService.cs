using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.Users.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Orchestration;

public class OrchestratorService : IOrchestrator
{
    private readonly ICvSyncService _cvSync;
    private readonly IScrapeService _scrapeService;
    private readonly ITailorService _tailorService;
    private readonly IApplyService _applyService;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        ICvSyncService cvSync,
        IScrapeService scrapeService,
        ITailorService tailorService,
        IApplyService applyService,
        IOptions<AgentOptions> agentOptions,
        ILogger<OrchestratorService> logger)
    {
        _cvSync = cvSync;
        _scrapeService = scrapeService;
        _tailorService = tailorService;
        _applyService = applyService;
        _agentOptions = agentOptions;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var options = _agentOptions.Value;

        _logger.LogInformation("Starting orchestrator. DryRun={DryRun}, Platforms={Platforms}",
            options.DryRun, string.Join(", ", options.EnabledPlatforms));

        // Phase 0: Parse CV → create/update User and Skills
        var user = await _cvSync.ParseAndSyncAsync(options.BaseCvPath, ct);
        if (user is null)
        {
            _logger.LogError("No user available. CV parsing failed and no existing user found.");
            return;
        }

        _logger.LogInformation("User: {FirstName} {LastName}", user.FirstName, user.LastName);

        // Phase 1: Scrape
        await _scrapeService.ScrapeAsync(user, ct);

        // Phase 2: Tailor CVs
        await _tailorService.TailorAsync(user, ct);

        // Phase 3: Apply
        await _applyService.ApplyAsync(user, ct);

        _logger.LogInformation("Orchestrator complete.");
    }
}
