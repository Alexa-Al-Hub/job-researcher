using System.Text;
using System.Text.Json;
using JobAgent.Application.Interfaces;
using JobAgent.Application.Models;
using JobAgent.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeCvParsingService : ICvParsingService
{
    private static readonly HttpClient HttpClient = new();
    private readonly ICvDocxService _docxService;
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly ILogger<ClaudeCvParsingService> _logger;

    public ClaudeCvParsingService(
        ICvDocxService docxService,
        IOptions<CredentialOptions> credentialOptions,
        ILogger<ClaudeCvParsingService> logger)
    {
        _docxService = docxService;
        _credentialOptions = credentialOptions;
        _logger = logger;
    }

    public async Task<CvProfile?> ParseAsync(string cvPath, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Skipping CV parsing.");
            return null;
        }

        if (!File.Exists(cvPath))
        {
            _logger.LogWarning("CV file not found at {Path}. Skipping CV parsing.", cvPath);
            return null;
        }

        var cvText = _docxService.ReadText(cvPath);
        if (string.IsNullOrWhiteSpace(cvText))
        {
            _logger.LogWarning("CV file is empty. Skipping CV parsing.");
            return null;
        }

        try
        {
            var prompt = $$"""
                Analyze the following CV/resume and extract structured information.
                Return ONLY a valid JSON object with this exact structure (no markdown, no code blocks):

                {
                  "firstName": "string",
                  "lastName": "string",
                  "email": "string or null",
                  "skills": [
                    { "name": "skill name", "category": "Language|Framework|Tool|Database|Cloud|SoftSkill|Methodology|Other" }
                  ],
                  "seniorityLevel": "Junior|Middle|Senior|Lead|Principal",
                  "preferredRoles": ["role1", "role2"],
                  "yearsOfExperience": number or null
                }

                Rules:
                - Extract ALL technical and soft skills mentioned
                - Categorize each skill appropriately
                - Infer seniority from years of experience and role titles
                - For preferredRoles, extract job titles the person has held or is targeting
                - If email is not found, set to null

                CV TEXT:
                {{cvText}}
                """;

            var requestBody = JsonSerializer.Serialize(new
            {
                model = "@phr-vertex-ai-us/anthropic.claude-opus-4-7",
                max_tokens = 4096,
                messages = new[] { new { role = "user", content = prompt } }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.portkey.ai/v1/chat/completions");
            request.Headers.Add("x-portkey-api-key", apiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await HttpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            var text = choices[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Claude returned empty response for CV parsing.");
                return null;
            }

            var profile = ParseProfileFromJson(text);
            if (profile is null)
            {
                _logger.LogWarning("Failed to parse Claude response into CvProfile.");
                return null;
            }

            _logger.LogInformation(
                "CV parsed: {FirstName} {LastName}, {SkillCount} skills, seniority: {Seniority}",
                profile.FirstName, profile.LastName, profile.Skills.Count, profile.SeniorityLevel);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing CV via Claude API.");
            return null;
        }
    }

    private CvProfile? ParseProfileFromJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<CvProfile>(json, options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize CV profile JSON: {Json}", json[..Math.Min(json.Length, 200)]);
            return null;
        }
    }
}
