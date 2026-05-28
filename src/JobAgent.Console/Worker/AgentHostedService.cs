using JobAgent.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobAgent.Console.Worker;

public class AgentHostedService : BackgroundService
{
    private readonly IOrchestrator _orchestrator;
    private readonly ILogger<AgentHostedService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AgentHostedService(
        IOrchestrator orchestrator,
        ILogger<AgentHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _orchestrator = orchestrator;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Agent starting...");

        try
        {
            await _orchestrator.RunAsync(stoppingToken);
            _logger.LogInformation("Job Agent completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job Agent was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job Agent failed with unhandled exception.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
