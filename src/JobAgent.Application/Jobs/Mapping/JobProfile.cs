using AutoMapper;
using JobAgent.Application.Jobs.DTOs;
using JobAgent.Domain.Entities;

namespace JobAgent.Application.Jobs.Mapping;

public class JobProfile : Profile
{
    public JobProfile()
    {
        CreateMap<Job, JobDto>()
            .ForMember(d => d.Skills, opt => opt.MapFrom(s =>
                s.JobSkills.Select(js => js.Skill.Name).ToList()));

        CreateMap<CreateJobRequest, Job>();
    }
}
