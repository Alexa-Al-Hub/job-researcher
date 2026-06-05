namespace JobAgent.Application.SearchCriteria.DTOs;

public record SearchCriteriaDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Keywords { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public List<string> Platforms { get; init; } = new();
    public bool IsActive { get; init; }
}
