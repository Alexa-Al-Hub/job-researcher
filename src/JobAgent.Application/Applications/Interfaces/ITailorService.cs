namespace JobAgent.Application.Applications.Interfaces;

public interface ITailorService
{
    Task TailorAsync(CancellationToken ct = default);
}
