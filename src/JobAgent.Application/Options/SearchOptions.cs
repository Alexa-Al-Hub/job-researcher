namespace JobAgent.Application.Options;

public class SearchOptions
{
    public const string SectionName = "Search";

    public string Keywords { get; set; } = ".NET Developer";
    public string Location { get; set; } = "Ukraine";
    public List<string> BonusKeywords { get; set; } = new();
}
