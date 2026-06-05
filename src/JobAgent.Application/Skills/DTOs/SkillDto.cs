using JobAgent.Domain.Enums;

namespace JobAgent.Application.Skills.DTOs;

public record SkillDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public SkillCategory Category { get; init; }
    public SkillSource Source { get; init; }
}
