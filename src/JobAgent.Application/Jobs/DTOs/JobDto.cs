using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.DTOs;

public class JobDto
{
    public int Id { get; set; }
    public Platform Platform { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Salary { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Skills { get; set; } = new();
}
