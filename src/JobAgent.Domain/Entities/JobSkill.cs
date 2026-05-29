namespace JobAgent.Domain.Entities;

// TODO: JRC-003 — links job to required skills (extracted from job description by AI)
public class JobSkill
{
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
