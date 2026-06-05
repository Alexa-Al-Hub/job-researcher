namespace JobAgent.Application.Orchestration;

public interface IOrchestrator
{
    Task RunAsync(CancellationToken ct = default);
}
