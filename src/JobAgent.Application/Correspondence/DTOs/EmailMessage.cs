namespace JobAgent.Application.Correspondence.DTOs;

public record EmailMessage(
    string Mailbox,
    string ExternalMessageId,
    string? ThreadId,
    string From,
    string Subject,
    string Body,
    DateTime ReceivedAt);
