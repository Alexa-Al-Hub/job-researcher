namespace JobAgent.Application.SearchCriteria.DTOs;

public record SearchCriteriaDto(
    int Id,
    int UserId,
    string Keywords,
    string Location,
    List<string> Platforms,
    bool IsActive);
