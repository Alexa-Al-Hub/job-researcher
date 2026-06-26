using System.Text;
using System.Text.Json;

namespace JobAgent.Infrastructure.CvServices;

/// <summary>
/// Minimal client for the Anthropic Messages API (https://api.anthropic.com/v1/messages).
/// Single-shot, non-streaming completions — the only shape the CV services need.
/// </summary>
internal static class ClaudeApi
{
    private static readonly HttpClient HttpClient = new();
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string Version = "2023-06-01";

    // Heavier reasoning (CV parsing, tailoring) uses Opus; cheaper, higher-volume
    // calls (scoring, synonyms) use Sonnet.
    public const string OpusModel = "claude-opus-4-8";
    public const string SonnetModel = "claude-sonnet-4-6";

    /// <summary>
    /// Sends a single user prompt and returns the text of the first text block,
    /// or null if the response contains none.
    /// </summary>
    public static async Task<string?> CompleteAsync(
        string apiKey, string model, int maxTokens, string prompt, CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", Version);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await HttpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.GetProperty("type").GetString() == "text")
                return block.GetProperty("text").GetString();
        }

        return null;
    }
}
