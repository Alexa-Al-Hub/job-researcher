namespace JobAgent.Application.Interfaces;

public interface IOrchestrator
{
    Task RunAsync(CancellationToken ct = default);
}
