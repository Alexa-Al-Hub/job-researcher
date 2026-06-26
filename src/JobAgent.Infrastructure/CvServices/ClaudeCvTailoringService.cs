using JobAgent.Application.Common;
using JobAgent.Application.Cv.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.CvServices;

public class ClaudeCvTailoringService : ICvTailoringService
{
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

    public Task<string?> TailorAsync(Job job, string baseCvPath, CancellationToken ct = default)
    {
        // ===== AI USAGE MINIMIZED =====
        // CV tailoring is the single most token-expensive step in the pipeline:
        // Claude Opus (~5x Sonnet pricing), up to 4096 output tokens, plus the full base
        // CV and job description as input — run once for EVERY job that passes scoring.
        // It is disabled here to keep token usage minimal; the base CV is used unchanged.
        // To re-enable: put `async` back on the signature above, delete this early return,
        // and remove the /* */ around the original body below.
        _logger.LogInformation("CV tailoring skipped (token-saving mode) for \"{Title}\" — using base CV", job.Title);
        return Task.FromResult<string?>(null);

        /*
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

            var tailoredText = await ClaudeApi.CompleteAsync(apiKey, ClaudeApi.OpusModel, 4096, prompt, ct);

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
        */
    }
}
