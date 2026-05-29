using JobAgent.Domain.Enums;

namespace JobAgent.Application.Options;

public class AgentOptions
{
    public const string SectionName = "Agent";

    public bool DryRun { get; set; } = true;
    public int MaxApplicationsPerDay { get; set; } = 50;
    public List<Platform> EnabledPlatforms { get; set; } = new() { Platform.Dou, Platform.Indeed };
    public string BaseCvPath { get; set; } = "cv/base_cv.docx";
    public List<string> TargetVacancies { get; set; } = new() { ".NET Developer" };
    public string TailoredCvOutputDir { get; set; } = "cv/tailored";
}
