using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.DTOs;

public class UpdateApplicationStatusRequest
{
    public int ApplicationId { get; set; }
    public ApplicationStatus NewStatus { get; set; }
}
