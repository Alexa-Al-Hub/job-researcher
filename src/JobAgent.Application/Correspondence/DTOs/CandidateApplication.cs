namespace JobAgent.Application.Correspondence.DTOs;

/// <summary>An open application an inbound email might be a reply to.</summary>
public record CandidateApplication(int ApplicationId, string Company, string Title, string Platform);
