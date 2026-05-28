using JobAgent.Domain.Enums;

namespace JobAgent.Domain.Entities;

public class Job
{
    public int Id { get; set; }
    public Platform Platform { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Salary { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Found;
    public string? CvPath { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
