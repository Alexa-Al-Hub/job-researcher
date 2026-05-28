namespace JobAgent.Application.Options;

public class CredentialOptions
{
    public const string SectionName = "Credentials";

    public string? AnthropicApiKey { get; set; }
    public string? LinkedInEmail { get; set; }
    public string? LinkedInPassword { get; set; }
    public string? GlassdoorEmail { get; set; }
    public string? GlassdoorPassword { get; set; }
}
