using JobAgent.Domain.Enums;

namespace JobAgent.Application.Skills.DTOs;

public record SkillDto(int Id, string Name, SkillCategory Category, SkillSource Source);
