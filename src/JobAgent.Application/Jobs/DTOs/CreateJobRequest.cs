using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.DTOs;

public record CreateJobRequest
{
    public Platform Platform { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Salary { get; init; }
}
