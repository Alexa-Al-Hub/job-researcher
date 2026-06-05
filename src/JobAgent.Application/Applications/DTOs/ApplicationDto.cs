using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.DTOs;

public record ApplicationDto
{
    public int Id { get; init; }
    public int JobId { get; init; }
    public string JobTitle { get; init; } = string.Empty;
    public int UserId { get; init; }
    public ApplicationStatus Status { get; init; }
    public string? CvPath { get; init; }
    public DateTime? AppliedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
