using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.DTOs;

public record JobDto
{
    public int Id { get; init; }
    public Platform Platform { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Salary { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<string> Skills { get; init; } = new();
}
