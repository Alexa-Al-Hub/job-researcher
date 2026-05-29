namespace JobAgent.Domain.Entities;

public class SearchCriteria
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Keywords { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Platforms { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
