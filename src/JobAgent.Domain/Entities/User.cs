namespace JobAgent.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SearchCriteria> SearchCriteria { get; set; } = new List<SearchCriteria>();
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
