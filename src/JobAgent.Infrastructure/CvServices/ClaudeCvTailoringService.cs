using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeCvTailoringService : ICvTailoringService
{
    private static readonly HttpClient HttpClient = new();
    private readonly ICvDocxService _docxService;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly IOptions<CredentialOptions> _credentialOptions;
    private readonly ILogger<ClaudeCvTailoringService> _logger;

    public ClaudeCvTailoringService(
        ICvDocxService docxService,
        IOptions<AgentOptions> agentOptions,
        IOptions<CredentialOptions> credentialOptions,
        ILogger<ClaudeCvTailoringService> logger)
    {
        _docxService = docxService;
        _agentOptions = agentOptions;
        _credentialOptions = credentialOptions;
        _logger = logger;
    }

    public async Task<string?> TailorAsync(Job job, string baseCvPath, CancellationToken ct = default)
    {
        var apiKey = _credentialOptions.Value.AnthropicApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No Anthropic API key configured. Skipping CV tailoring, using base CV.");
            return null;
        }

        if (!File.Exists(baseCvPath))
        {
            _logger.LogWarning("Base CV not found at {Path}. Skipping tailoring.", baseCvPath);
            return null;
        }

        var cvText = _docxService.ReadText(baseCvPath);
        if (string.IsNullOrWhiteSpace(cvText))
        {
            _logger.LogWarning("Base CV is empty. Skipping tailoring.");
            return null;
        }

        try
        {
            var outputDir = _agentOptions.Value.TailoredCvOutputDir;
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, $"cv_{job.Id}_{job.Platform}.docx");

            var prompt = $"""
                You are a professional CV writer. Tailor the following CV to better match this job posting.
                Keep the same structure and truthful information, but emphasize relevant skills and experience.

                JOB TITLE: {job.Title}
                COMPANY: {job.Company}
                DESCRIPTION: {job.Description ?? "N/A"}

                CURRENT CV:
                {cvText}

                Return ONLY the tailored CV text, no explanations.
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
            var tailoredText = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(tailoredText))
            {
                _logger.LogWarning("Claude returned empty response for job {JobId}", job.Id);
                return null;
            }

            _docxService.WriteText(baseCvPath, outputPath, tailoredText);
            _logger.LogInformation("Tailored CV saved to {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tailoring CV via Claude API for job {JobId}", job.Id);
            return null;
        }
    }
}
