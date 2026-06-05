using JobAgent.Application.Skills.DTOs;

namespace JobAgent.Application.Users.DTOs;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    List<SkillDto> Skills);
