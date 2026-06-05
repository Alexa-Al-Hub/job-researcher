using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Application.Users.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Orchestration;

public class OrchestratorService : IOrchestrator
{
    private readonly ICvSyncService _cvSync;
    private readonly ISynonymService _synonymService;
    private readonly IScrapeService _scrapeService;
    private readonly IScoringService _scoringService;
    private readonly ITailorService _tailorService;
    private readonly IApplyService _applyService;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        ICvSyncService cvSync,
        ISynonymService synonymService,
        IScrapeService scrapeService,
        IScoringService scoringService,
        ITailorService tailorService,
        IApplyService applyService,
        IOptions<AgentOptions> agentOptions,
        ILogger<OrchestratorService> logger)
    {
        _cvSync = cvSync;
        _synonymService = synonymService;
        _scrapeService = scrapeService;
        _scoringService = scoringService;
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

        // Phase 0.5: Generate search synonyms from target positions
        await _synonymService.GenerateAndSaveAsync(user, options.TargetVacancies, user.YearsOfExperience, ct);

        // Discovery pipeline (Scrape → Score → Tailor) runs in parallel with Apply pipeline
        var discoveryTask = DiscoverAsync(user, ct);
        var applyTask = _applyService.ApplyAsync(user, ct);

        await Task.WhenAll(discoveryTask, applyTask);

        _logger.LogInformation("Orchestrator complete.");
    }

    private async Task DiscoverAsync(User user, CancellationToken ct)
    {
        await _scrapeService.ScrapeAsync(user, ct);
        await _scoringService.ScoreAsync(user, ct);
        await _tailorService.TailorAsync(user, ct);
    }
}
