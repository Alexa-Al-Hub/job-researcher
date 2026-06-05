namespace JobAgent.Application.Common;

public class ScoringOptions
{
    public const string SectionName = "Scoring";

    public int MinScoreThreshold { get; set; } = 60;
    public int MaxConcurrentScoring { get; set; } = 5;
}
