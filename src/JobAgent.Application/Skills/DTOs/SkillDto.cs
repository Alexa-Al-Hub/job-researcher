using JobAgent.Domain.Enums;

namespace JobAgent.Application.Skills.DTOs;

public class SkillDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }
    public SkillSource Source { get; set; }
}
