using System.Text.Json;
using JobAgent.Application.Common;
using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeSynonymService : ISynonymService
{
    private readonly ISearchCriteriaRepository _searchCriteriaRepository;
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly ILogger<ClaudeSynonymService> _logger;

    public ClaudeSynonymService(
        ISearchCriteriaRepository searchCriteriaRepository,
        IOptions<CredentialOptions> credentialOptions,
        IOptions<AgentOptions> agentOptions,
        ILogger<ClaudeSynonymService> logger)
    {
        _searchCriteriaRepository = searchCriteriaRepository;
        _credentialOptions = credentialOptions;
        _agentOptions = agentOptions;
        _logger = logger;
    }

    public async Task GenerateAndSaveAsync(User user, IReadOnlyList<string> targetPositions, int? yearsOfExperience = null, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Skipping synonym generation.");
            return;
        }

        var existingCriteria = await _searchCriteriaRepository.GetActiveAsync(ct);
        var existingKeywords = existingCriteria.Select(c => c.Keywords.ToLowerInvariant()).ToHashSet();

        foreach (var position in targetPositions)
        {
            if (existingKeywords.Contains(position.ToLowerInvariant()))
            {
                _logger.LogInformation("Skipping synonym generation for \"{Position}\" — already exists", position);
                continue;
            }

            var synonyms = await GenerateSynonymsAsync(position, yearsOfExperience, ct);
            if (synonyms.Count == 0)
                continue;

            var newCount = 0;
            foreach (var synonym in synonyms)
            {
                if (existingKeywords.Contains(synonym.ToLowerInvariant()))
                    continue;

                var criteria = new Domain.Entities.SearchCriteria
                {
                    UserId = user.Id,
                    Keywords = synonym,
                    Location = existingCriteria.FirstOrDefault()?.Location ?? "Remote",
                    IsActive = true
                };

                await _searchCriteriaRepository.AddAsync(criteria, ct);
                existingKeywords.Add(synonym.ToLowerInvariant());
                newCount++;
            }

            _logger.LogInformation("Generated {Count} new synonyms for \"{Position}\": {Synonyms}",
                newCount, position, string.Join(", ", synonyms));
        }
    }

    private async Task<List<string>> GenerateSynonymsAsync(string position, int? yearsOfExperience, CancellationToken ct)
    {
        var experienceContext = yearsOfExperience.HasValue
            ? $"\nThe candidate has {yearsOfExperience} years of experience. Only include seniority levels appropriate for this experience (e.g. 1-2yr → Junior, 3-5yr → Middle, 5+yr → Senior/Lead)."
            : "";

        var prompt = $$"""
            Generate job title synonyms/variations for the position: "{{position}}"{{experienceContext}}

            Return ONLY a JSON array of strings (no markdown, no code blocks). Include:
            - Common alternative titles for the same role
            - Seniority variations appropriate for the candidate's experience level
            - Tech-specific variations (e.g. ".NET Developer" → "C# Engineer", "Backend .NET Developer")
            - English and common abbreviations

            Return 5-10 most relevant variations. Do NOT include the original title.

            Example: [".NET Software Engineer", "C# Developer", "Backend .NET Developer", "C# Software Engineer"]
            """;

        try
        {
            var text = await ClaudeApi.CompleteAsync(
                _credentialOptions.Value.AnthropicApiKey!, ClaudeApi.SonnetModel, 256, prompt, ct);

            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(text) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating synonyms for \"{Position}\"", position);
            return new List<string>();
        }
    }
}
