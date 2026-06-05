using JobAgent.Domain.Enums;

namespace JobAgent.Domain.Entities;

public class Application
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Found;
    public int? Score { get; set; }
    public string? ScoreReason { get; set; }
    public string? CvPath { get; set; }
    public string? Notes { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
}
