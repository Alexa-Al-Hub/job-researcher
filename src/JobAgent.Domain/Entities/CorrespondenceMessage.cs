using JobAgent.Domain.Enums;

namespace JobAgent.Domain.Entities;

public class CorrespondenceMessage
{
    public int Id { get; set; }

    /// <summary>The mailbox (Gmail account) this message was received in.</summary>
    public string Mailbox { get; set; } = string.Empty;

    /// <summary>Provider message id — unique within a mailbox; used for dedup.</summary>
    public string ExternalMessageId { get; set; } = string.Empty;
    public string? ThreadId { get; set; }

    /// <summary>The application this email relates to, if it could be matched.</summary>
    public int? ApplicationId { get; set; }
    public Application? Application { get; set; }

    public string FromAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }

    public CorrespondenceDirection Direction { get; set; } = CorrespondenceDirection.Inbound;
    public CorrespondenceCategory Category { get; set; } = CorrespondenceCategory.Other;

    /// <summary>LLM confidence (0-100) that this message matches <see cref="ApplicationId"/>.</summary>
    public int MatchConfidence { get; set; }

    /// <summary>LLM-drafted reply. Stored for review only — never sent automatically.</summary>
    public string? SuggestedReply { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
