using System.Text;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using JobAgent.Application.Common;
using JobAgent.Application.Correspondence.DTOs;
using JobAgent.Application.Correspondence.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.Email;

/// <summary>
/// Reads mail from one or more Gmail accounts via the Gmail API using the read-only scope.
/// Each account authorizes once (browser consent); its refresh token is cached in the token store.
/// </summary>
public class GmailEmailClient : IEmailClient
{
    private const string ApplicationName = "JobAgent";
    private static readonly string[] Scopes = { GmailService.Scope.GmailReadonly };

    private readonly GmailOptions _options;
    private readonly ILogger<GmailEmailClient> _logger;

    public GmailEmailClient(IOptions<GmailOptions> options, ILogger<GmailEmailClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailMessage>> FetchSinceAsync(
        string mailbox, DateTime since, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.CredentialsPath) || !File.Exists(_options.CredentialsPath))
        {
            _logger.LogWarning("Gmail credentials not found at {Path}; skipping {Mailbox}",
                _options.CredentialsPath, mailbox);
            return Array.Empty<EmailMessage>();
        }

        var service = await CreateServiceAsync(mailbox, ct);

        var listRequest = service.Users.Messages.List("me");
        listRequest.Q = $"after:{since:yyyy/MM/dd} -in:chats";
        listRequest.MaxResults = 100;

        var listResponse = await listRequest.ExecuteAsync(ct);
        if (listResponse.Messages is null)
            return Array.Empty<EmailMessage>();

        var results = new List<EmailMessage>();
        foreach (var reference in listResponse.Messages)
        {
            ct.ThrowIfCancellationRequested();

            var getRequest = service.Users.Messages.Get("me", reference.Id);
            getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            var message = await getRequest.ExecuteAsync(ct);

            results.Add(MapMessage(mailbox, message));
        }

        return results;
    }

    private async Task<GmailService> CreateServiceAsync(string mailbox, CancellationToken ct)
    {
        await using var stream = new FileStream(_options.CredentialsPath!, FileMode.Open, FileAccess.Read);
        var secrets = (await GoogleClientSecrets.FromStreamAsync(stream, ct)).Secrets;

        // The mailbox doubles as the token-store key, so each account caches its own token.
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            mailbox,
            ct,
            new FileDataStore(_options.TokenStoreDir, fullPath: true));

        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    private static EmailMessage MapMessage(string mailbox, Message message)
    {
        var headers = message.Payload?.Headers ?? new List<MessagePartHeader>();

        string Header(string name) => headers
            .FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

        var receivedAt = message.InternalDate.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).UtcDateTime
            : DateTime.UtcNow;

        return new EmailMessage(
            mailbox,
            message.Id,
            message.ThreadId,
            Header("From"),
            Header("Subject"),
            ExtractBody(message.Payload),
            receivedAt);
    }

    /// <summary>Walks the MIME tree and returns plain text (preferred) or stripped HTML.</summary>
    private static string ExtractBody(MessagePart? part)
    {
        if (part is null)
            return "";

        if (!string.IsNullOrEmpty(part.Body?.Data) &&
            (part.MimeType == "text/plain" || part.MimeType == "text/html"))
        {
            var decoded = DecodeBase64Url(part.Body.Data);
            return part.MimeType == "text/html" ? StripHtml(decoded) : decoded;
        }

        if (part.Parts is { Count: > 0 })
        {
            // Prefer text/plain, then fall back to the first part that yields anything.
            var plain = part.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
            if (plain is not null)
            {
                var text = ExtractBody(plain);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            foreach (var child in part.Parts)
            {
                var text = ExtractBody(child);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return "";
    }

    private static string DecodeBase64Url(string data)
    {
        var normalized = data.Replace('-', '+').Replace('_', '/');
        normalized = (normalized.Length % 4) switch
        {
            2 => normalized + "==",
            3 => normalized + "=",
            _ => normalized
        };

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    private static string StripHtml(string html)
        => Regex.Replace(html, "<[^>]+>", " ").Trim();
}
