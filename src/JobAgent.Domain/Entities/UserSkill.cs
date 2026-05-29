namespace JobAgent.Domain.Entities;

// TODO: JRC-002 — links user to skills extracted from CV
public class UserSkill
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
}
