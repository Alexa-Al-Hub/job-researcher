using System.Text.Json;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeJobScoringService : IJobScoringService
{
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly ILogger<ClaudeJobScoringService> _logger;

    public ClaudeJobScoringService(
        IOptions<CredentialOptions> credentialOptions,
        ILogger<ClaudeJobScoringService> logger)
    {
        _credentialOptions = credentialOptions;
        _logger = logger;
    }

    public Task<(int Score, string Reason)> ScoreAsync(Job job, IReadOnlyList<Skill> userSkills, int? yearsOfExperience = null, CancellationToken ct = default)
    {
        // ===== AI USAGE MINIMIZED =====
        // Scoring otherwise calls Claude Sonnet once per scraped job, with the full job
        // description as input. Disabled to keep token usage minimal; the free keyword-based
        // FallbackScore is used instead. To re-enable: put `async` back on the signature
        // above, delete this early return, and remove the /* */ around the original body.
        _logger.LogInformation("AI scoring skipped (token-saving mode) for \"{Title}\" — using keyword fallback", job.Title);
        return Task.FromResult(FallbackScore(job, userSkills));

        /*
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Using fallback scoring.");
            return FallbackScore(job, userSkills);
        }

        var skillList = string.Join(", ", userSkills.Select(s => s.Name));
        var experienceInfo = yearsOfExperience.HasValue
            ? $"\n\nCANDIDATE EXPERIENCE: {yearsOfExperience} years"
            : "";
        var jobDescription = !string.IsNullOrWhiteSpace(job.Description)
            ? job.Description
            : $"Title: {job.Title}, Company: {job.Company}";

        var prompt = $$"""
            Score how well this candidate matches the job posting. Return ONLY a valid JSON object (no markdown, no code blocks):

            {
              "score": <number 0-100>,
              "reason": "<one sentence explanation>"
            }

            Rules:
            - 90-100: Perfect match, all key skills present, experience level matches
            - 70-89: Strong match, most skills present, experience within range
            - 50-69: Partial match, some relevant skills or slight experience mismatch
            - 30-49: Weak match, few relevant skills or significant experience gap
            - 0-29: Poor match, almost no overlap
            - If the job requires significantly more experience than the candidate has, lower the score
            - If the job level (Senior/Lead) doesn't match the candidate's experience, account for that

            CANDIDATE SKILLS:
            {{skillList}}{{experienceInfo}}

            JOB POSTING:
            {{jobDescription}}
            """;

        try
        {
            var text = await ClaudeApi.CompleteAsync(apiKey, ClaudeApi.SonnetModel, 256, prompt, ct);

            if (string.IsNullOrWhiteSpace(text))
                return FallbackScore(job, userSkills);

            return ParseScoreResponse(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scoring job {JobId} via Claude API", job.Id);
            return FallbackScore(job, userSkills);
        }
        */
    }

    private static (int Score, string Reason) FallbackScore(Job job, IReadOnlyList<Skill> userSkills)
    {
        var titleLower = job.Title.ToLowerInvariant();
        var descLower = (job.Description ?? "").ToLowerInvariant();
        var combined = $"{titleLower} {descLower}";

        var matchCount = userSkills.Count(s => combined.Contains(s.Name.ToLowerInvariant()));
        var score = userSkills.Count > 0
            ? (int)(matchCount / (double)userSkills.Count * 100)
            : 50;

        return (score, $"Keyword match: {matchCount}/{userSkills.Count} skills found in job text");
    }

    private (int Score, string Reason) ParseScoreResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var score = doc.RootElement.GetProperty("score").GetInt32();
            var reason = doc.RootElement.GetProperty("reason").GetString() ?? "No reason provided";
            return (Math.Clamp(score, 0, 100), reason);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse scoring JSON: {Json}", json[..Math.Min(json.Length, 200)]);
            return (50, "Failed to parse AI response, defaulting to 50");
        }
    }
}
