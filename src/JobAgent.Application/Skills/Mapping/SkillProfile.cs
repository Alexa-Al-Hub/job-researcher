using AutoMapper;
using JobAgent.Application.Skills.DTOs;
using JobAgent.Domain.Entities;

namespace JobAgent.Application.Skills.Mapping;

public class SkillProfile : Profile
{
    public SkillProfile()
    {
        CreateMap<Skill, SkillDto>();
        CreateMap<SkillDto, Skill>();
    }
}
