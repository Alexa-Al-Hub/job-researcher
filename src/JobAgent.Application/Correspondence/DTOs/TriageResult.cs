using JobAgent.Domain.Enums;

namespace JobAgent.Application.Correspondence.DTOs;

public record TriageResult(
    CorrespondenceCategory Category,
    int? MatchedApplicationId,
    int Confidence,
    string? SuggestedReply);
