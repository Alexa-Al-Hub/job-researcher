using JobAgent.Domain.Enums;

namespace JobAgent.Domain.Entities;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; } = SkillCategory.Other;
    public SkillSource Source { get; set; } = SkillSource.CvParsed;

    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
