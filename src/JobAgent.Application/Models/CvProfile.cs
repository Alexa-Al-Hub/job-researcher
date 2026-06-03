namespace JobAgent.Application.Models;

public class CvProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public List<CvSkill> Skills { get; set; } = new();
    public string? SeniorityLevel { get; set; }
    public List<string> PreferredRoles { get; set; } = new();
    public int? YearsOfExperience { get; set; }
}

public class CvSkill
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
}
