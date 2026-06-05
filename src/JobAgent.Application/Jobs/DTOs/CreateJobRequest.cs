using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.DTOs;

public record CreateJobRequest(
    Platform Platform,
    string Title,
    string Company,
    string Url,
    string? Description = null,
    string? Salary = null);
