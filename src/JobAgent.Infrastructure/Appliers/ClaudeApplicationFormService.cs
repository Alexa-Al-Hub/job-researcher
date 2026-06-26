using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Entities;
using JobAgent.Infrastructure.CvServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.Appliers;

public class ClaudeApplicationFormService : IApplicationFormService
{
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly IOptions<ApplicantProfileOptions> _profileOptions;
    private readonly ILogger<ClaudeApplicationFormService> _logger;

    public ClaudeApplicationFormService(
        IOptions<CredentialOptions> credentialOptions,
        IOptions<ApplicantProfileOptions> profileOptions,
        ILogger<ClaudeApplicationFormService> logger)
    {
        _credentialOptions = credentialOptions;
        _profileOptions = profileOptions;
        _logger = logger;
    }

    public async Task<string?> AnswerAsync(
        string question, IReadOnlyList<string>? options, Job job, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Cannot answer form question.");
            return null;
        }

        var optionText = options is { Count: > 0 }
            ? $"Choose EXACTLY one of these options, copied verbatim: {string.Join(" | ", options)}"
            : "Provide a concise, truthful answer.";

        var prompt = $$"""
            You are filling out a job application form on behalf of the candidate below.
            Answer the screening question truthfully using ONLY the candidate facts.
            If the facts do not let you answer truthfully, respond with exactly: UNKNOWN.
            {{optionText}}

            CANDIDATE FACTS:
            {{BuildProfileText()}}

            JOB: {{job.Title}} at {{job.Company}}

            QUESTION: {{question}}

            Respond with ONLY the answer text (or UNKNOWN). No preamble, no explanation.
            """;

        try
        {
            var answer = (await ClaudeApi.CompleteAsync(apiKey, ClaudeApi.SonnetModel, 256, prompt, ct))?.Trim();
            if (string.IsNullOrWhiteSpace(answer) ||
                answer.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering form question: {Question}", question);
            return null;
        }
    }

    private string BuildProfileText()
    {
        var p = _profileOptions.Value;
        var lines = new List<string>
        {
            $"Full name: {p.FullName}",
            $"Email: {p.Email}",
            $"Phone: {p.Phone}",
            $"Location: {p.Location}",
            $"Work authorization: {p.WorkAuthorization}",
            $"Requires visa sponsorship: {(p.RequiresVisaSponsorship ? "yes" : "no")}",
            $"Willing to relocate: {(p.WillRelocate ? "yes" : "no")}",
            $"Salary expectation: {p.SalaryExpectation}",
            $"Notice period: {p.NoticePeriod}",
            $"Years of experience: {(p.YearsOfExperience?.ToString() ?? "")}",
            $"LinkedIn: {p.LinkedInUrl}",
            $"Portfolio: {p.PortfolioUrl}",
            $"Additional notes: {p.AdditionalNotes}"
        };

        return string.Join("\n", lines.Where(l => !l.EndsWith(": ")));
    }
}
