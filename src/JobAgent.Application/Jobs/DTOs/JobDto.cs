using JobAgent.Domain.Enums;

namespace JobAgent.Application.Jobs.DTOs;

public record JobDto(
    int Id,
    Platform Platform,
    string Title,
    string Company,
    string Url,
    string? Description,
    string? Salary,
    DateTime CreatedAt,
    List<string> Skills);
