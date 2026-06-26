using System.Text.Json;
using JobAgent.Application.Common;
using JobAgent.Application.Correspondence.DTOs;
using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.CvServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.Email;

public class ClaudeEmailTriageService : IEmailTriageService
{
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly ILogger<ClaudeEmailTriageService> _logger;

    public ClaudeEmailTriageService(
        IOptions<CredentialOptions> credentialOptions,
        ILogger<ClaudeEmailTriageService> logger)
    {
        _credentialOptions = credentialOptions;
        _logger = logger;
    }

    public async Task<TriageResult> ClassifyAsync(
        EmailMessage email, IReadOnlyList<CandidateApplication> candidates, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Storing email without classification.");
            return Unclassified;
        }

        var candidateLines = candidates.Count > 0
            ? string.Join("\n", candidates.Select(c =>
                $"- id={c.ApplicationId}: \"{c.Title}\" at {c.Company} ({c.Platform})"))
            : "(none)";

        var body = email.Body.Length > 4000 ? email.Body[..4000] : email.Body;

        var prompt = $$"""
            You are triaging an email received in a job-search inbox ({{email.Mailbox}}).
            Classify it, and if it is a reply to one of the candidate job applications below, match it.

            Return ONLY a valid JSON object (no markdown, no code blocks):
            {
              "category": "InterviewInvite|Rejection|Question|RecruiterOutreach|Acknowledgement|Other",
              "matchedApplicationId": <id from the candidate list, or null if no clear match>,
              "confidence": <0-100, how confident you are in the match>,
              "suggestedReply": "<short, professional reply ONLY when category is Question or InterviewInvite; otherwise null>"
            }

            Guidance:
            - InterviewInvite: invitation to interview, call, or next stage.
            - Rejection: the application was declined.
            - Question: the recruiter asks the candidate for information or a decision.
            - RecruiterOutreach: a new, unsolicited opportunity (not a reply to an existing application).
            - Acknowledgement: "we received your application" with no action needed.
            - Only set matchedApplicationId to an id that appears in the candidate list.

            CANDIDATE APPLICATIONS:
            {{candidateLines}}

            EMAIL:
            From: {{email.From}}
            Subject: {{email.Subject}}
            Body:
            {{body}}
            """;

        try
        {
            var text = await ClaudeApi.CompleteAsync(apiKey, ClaudeApi.SonnetModel, 512, prompt, ct);
            return string.IsNullOrWhiteSpace(text) ? Unclassified : Parse(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triaging email from {From}", email.From);
            return Unclassified;
        }
    }

    private static TriageResult Unclassified => new(CorrespondenceCategory.Other, null, 0, null);

    private TriageResult Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var category = Enum.TryParse<CorrespondenceCategory>(
                root.GetProperty("category").GetString(), ignoreCase: true, out var cat)
                ? cat
                : CorrespondenceCategory.Other;

            int? matchedId = root.TryGetProperty("matchedApplicationId", out var m) && m.ValueKind == JsonValueKind.Number
                ? m.GetInt32()
                : null;

            var confidence = root.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number
                ? Math.Clamp(c.GetInt32(), 0, 100)
                : 0;

            var reply = root.TryGetProperty("suggestedReply", out var r) && r.ValueKind == JsonValueKind.String
                ? r.GetString()
                : null;

            return new TriageResult(category, matchedId, confidence, reply);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse triage JSON: {Json}", json[..Math.Min(json.Length, 200)]);
            return Unclassified;
        }
    }
}
