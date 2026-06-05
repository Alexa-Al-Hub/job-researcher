using System.Text;
using System.Text.Json;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeJobScoringService : IJobScoringService
{
    private static readonly HttpClient HttpClient = new();
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly ILogger<ClaudeJobScoringService> _logger;

    public ClaudeJobScoringService(
        IOptions<CredentialOptions> credentialOptions,
        ILogger<ClaudeJobScoringService> logger)
    {
        _credentialOptions = credentialOptions;
        _logger = logger;
    }

    public async Task<(int Score, string Reason)> ScoreAsync(Job job, IReadOnlyList<Skill> userSkills, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Using fallback scoring.");
            return FallbackScore(job, userSkills);
        }

        var skillList = string.Join(", ", userSkills.Select(s => s.Name));
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
            - 90-100: Perfect match, all key skills present
            - 70-89: Strong match, most skills present
            - 50-69: Partial match, some relevant skills
            - 30-49: Weak match, few relevant skills
            - 0-29: Poor match, almost no overlap

            CANDIDATE SKILLS:
            {{skillList}}

            JOB POSTING:
            {{jobDescription}}
            """;

        try
        {
            var requestBody = JsonSerializer.Serialize(new
            {
                model = "@phr-vertex-ai-us/anthropic.claude-sonnet-4-5-20250514",
                max_tokens = 256,
                messages = new[] { new { role = "user", content = prompt } }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.portkey.ai/v1/chat/completions");
            request.Headers.Add("x-portkey-api-key", apiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await HttpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(text))
                return FallbackScore(job, userSkills);

            return ParseScoreResponse(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scoring job {JobId} via Claude API", job.Id);
            return FallbackScore(job, userSkills);
        }
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
