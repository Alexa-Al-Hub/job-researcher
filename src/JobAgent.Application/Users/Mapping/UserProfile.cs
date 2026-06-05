using AutoMapper;
using JobAgent.Application.Users.DTOs;
using JobAgent.Domain.Entities;

namespace JobAgent.Application.Users.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Skills, opt => opt.MapFrom(s =>
                s.UserSkills.Select(us => us.Skill).ToList()));
    }
}
