namespace JobAgent.Application.Common;

public class GmailOptions
{
    public const string SectionName = "Gmail";

    /// <summary>Path to the OAuth desktop-client JSON downloaded from Google Cloud.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>Directory where per-account OAuth tokens are cached.</summary>
    public string TokenStoreDir { get; set; } = ".gmail_tokens";

    /// <summary>Gmail accounts to monitor.</summary>
    public List<string> Accounts { get; set; } = new();

    /// <summary>How far back to look when a mailbox has never been synced.</summary>
    public int LookbackDays { get; set; } = 30;
}
