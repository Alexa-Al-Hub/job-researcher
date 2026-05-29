namespace JobAgent.Domain.Entities;

// TODO: JRC-002 — populated by AI CV parsing (extract skills from base_cv.docx)
public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}
