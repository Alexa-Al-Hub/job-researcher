using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.DTOs;

public class ApplicationDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? CvPath { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
