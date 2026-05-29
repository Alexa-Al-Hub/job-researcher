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
    public int? SearchCriteriaId { get; set; }
    public SearchCriteria? SearchCriteria { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
