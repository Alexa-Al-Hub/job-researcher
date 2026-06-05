namespace JobAgent.Application.Cv.Models;

public record CvProfile(
    string FirstName,
    string LastName,
    string? Email,
    List<CvSkill> Skills,
    string? SeniorityLevel = null,
    List<string>? PreferredRoles = null,
    int? YearsOfExperience = null);

public record CvSkill(string Name, string Category = "Other");
