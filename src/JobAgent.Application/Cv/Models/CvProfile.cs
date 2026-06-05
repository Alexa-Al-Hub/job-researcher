namespace JobAgent.Application.Cv.Models;

public record CvProfile
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public List<CvSkill> Skills { get; init; } = new();
    public string? SeniorityLevel { get; init; }
    public List<string> PreferredRoles { get; init; } = new();
    public int? YearsOfExperience { get; init; }
}

public record CvSkill(string Name, string Category = "Other");
