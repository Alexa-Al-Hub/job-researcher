namespace JobAgent.Application.SearchCriteria.DTOs;

public class SearchCriteriaDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Keywords { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Platforms { get; set; } = new();
    public bool IsActive { get; set; }
}
