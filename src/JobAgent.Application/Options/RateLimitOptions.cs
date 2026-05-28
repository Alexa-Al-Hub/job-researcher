namespace JobAgent.Application.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int MinDelayBetweenRequestsMs { get; set; } = 2000;
    public int MaxDelayBetweenRequestsMs { get; set; } = 5000;
    public int DelayBetweenApplicationsMs { get; set; } = 10000;
}
