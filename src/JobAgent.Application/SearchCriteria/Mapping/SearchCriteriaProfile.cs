using AutoMapper;
using JobAgent.Application.SearchCriteria.DTOs;

namespace JobAgent.Application.SearchCriteria.Mapping;

public class SearchCriteriaProfile : Profile
{
    public SearchCriteriaProfile()
    {
        CreateMap<Domain.Entities.SearchCriteria, SearchCriteriaDto>();
    }
}
