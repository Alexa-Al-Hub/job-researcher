using JobAgent.Application.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobAgent.Console.Worker;

public class AgentHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentHostedService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public AgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Agent starting...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IOrchestrator>();
            await orchestrator.RunAsync(stoppingToken);
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
