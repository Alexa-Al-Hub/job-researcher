using JobAgent.Application.Skills.DTOs;

namespace JobAgent.Application.Users.DTOs;

public record UserDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<SkillDto> Skills { get; init; } = new();
}
