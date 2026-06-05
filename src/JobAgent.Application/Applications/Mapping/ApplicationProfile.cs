using AutoMapper;
using JobAgent.Application.Applications.DTOs;

namespace JobAgent.Application.Applications.Mapping;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<Domain.Entities.Application, ApplicationDto>()
            .ForMember(d => d.JobTitle, opt => opt.MapFrom(s => s.Job.Title));
    }
}
