namespace JobAgent.Application.Common;

/// <summary>
/// Facts the LLM uses to answer application screening questions truthfully.
/// Fill these in appsettings (or user-secrets) — empty fields just mean the agent
/// will leave related questions unanswered and fall back to manual follow-up.
/// </summary>
public class ApplicantProfileOptions
{
    public const string SectionName = "ApplicantProfile";

    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Location { get; set; } = "";
    public string WorkAuthorization { get; set; } = "";
    public bool RequiresVisaSponsorship { get; set; }
    public bool WillRelocate { get; set; }
    public string SalaryExpectation { get; set; } = "";
    public string NoticePeriod { get; set; } = "";
    public int? YearsOfExperience { get; set; }
    public string LinkedInUrl { get; set; } = "";
    public string PortfolioUrl { get; set; } = "";

    /// <summary>Free-form extra facts the LLM may draw on for unusual questions.</summary>
    public string AdditionalNotes { get; set; } = "";
}
