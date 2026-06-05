using JobAgent.Application.Skills.DTOs;

namespace JobAgent.Application.Users.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<SkillDto> Skills { get; set; } = new();
}
