using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.DTOs;

public record UpdateApplicationStatusRequest(int ApplicationId, ApplicationStatus NewStatus);
