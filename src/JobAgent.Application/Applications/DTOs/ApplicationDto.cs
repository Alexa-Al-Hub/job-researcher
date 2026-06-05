using JobAgent.Domain.Enums;

namespace JobAgent.Application.Applications.DTOs;

public record ApplicationDto(
    int Id,
    int JobId,
    string JobTitle,
    int UserId,
    ApplicationStatus Status,
    string? CvPath,
    DateTime? AppliedAt,
    DateTime CreatedAt);
