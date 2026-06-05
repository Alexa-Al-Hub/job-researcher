using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;

namespace JobAgent.UnitTest.Domain;

public class SkillTests
{
    [Fact]
    public void Skill_DefaultSource_IsCvParsed()
    {
        var skill = new Skill { Name = "C#" };

        skill.Source.Should().Be(SkillSource.CvParsed);
    }

    [Fact]
    public void Skill_DefaultCategory_IsOther()
    {
        var skill = new Skill { Name = "C#" };

        skill.Category.Should().Be(SkillCategory.Other);
    }

    [Fact]
    public void Skill_CanSetCategory()
    {
        var skill = new Skill
        {
            Name = "PostgreSQL",
            Category = SkillCategory.Database,
            Source = SkillSource.CvParsed
        };

        skill.Category.Should().Be(SkillCategory.Database);
        skill.Name.Should().Be("PostgreSQL");
    }
}
