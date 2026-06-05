namespace JobAgent.Infrastructure.Constants;

public static class ScraperConstants
{
    // DOU
    public const string DouBaseUrl = "https://jobs.dou.ua/vacancies/?search=";
    public const string DouVacancySelector = ".l-vacancy";
    public const string DouMoreButtonSelector = ".more-btn a";
    public const string DouTitleSelector = ".vt";
    public const string DouCompanySelector = ".company";
    public const string DouSalarySelector = ".salary";
    public const int DouWaitTimeout = 10000;

    // Indeed
    public const string IndeedBaseUrl = "https://www.indeed.com/jobs";
    public const string IndeedCardSelector = ".job_seen_beacon, .jobsearch-ResultsList > li";
    public const string IndeedTitleSelector = "h2.jobTitle a, .jobTitle > a";
    public const string IndeedCompanySelector = "[data-testid='company-name'], .companyName";
    public const string IndeedSalarySelector = "[data-testid='attribute_snippet_testid'], .salary-snippet-container";
    public const string IndeedOrigin = "https://www.indeed.com";

    // LinkedIn
    public const string LinkedInBaseUrl = "https://www.linkedin.com/jobs/search/";
    public const string LinkedInLoginUrl = "https://www.linkedin.com/login";
    public const string LinkedInOrigin = "https://www.linkedin.com";
    public const string LinkedInCardSelector = ".jobs-search-results__list-item, .job-card-container";
    public const string LinkedInTitleSelector = ".job-card-list__title, .job-card-container__link";
    public const string LinkedInCompanySelector = ".job-card-container__primary-description, .artdeco-entity-lockup__subtitle";
    public const string LinkedInLinkSelector = "a.job-card-list__title, a.job-card-container__link";
    public const string LinkedInUsernameSelector = "#username";
    public const string LinkedInPasswordSelector = "#password";
    public const string LinkedInSubmitSelector = "[data-litms-control-urn='login-submit']";
    public const int LinkedInScrollCount = 3;
    public const int LinkedInLoginTimeout = 30000;

    // Glassdoor
    public const string GlassdoorBaseUrl = "https://www.glassdoor.com/Job/jobs.htm";
    public const string GlassdoorLoginUrl = "https://www.glassdoor.com/profile/login_input.htm";
    public const string GlassdoorOrigin = "https://www.glassdoor.com";
    public const string GlassdoorCardSelector = "[data-test='jobListing'], .JobsList_jobListItem__wjTHv, li.react-job-listing";
    public const string GlassdoorTitleSelector = "[data-test='job-title'], a.jobTitle";
    public const string GlassdoorCompanySelector = "[data-test='emp-name'], .EmployerProfile_compactEmployerName__LE242";
    public const string GlassdoorSalarySelector = "[data-test='detailSalary'], .salary-estimate";
    public const string GlassdoorEmailSelector = "#inlineUserEmail, [name='username'], input[type='email']";
    public const string GlassdoorPasswordSelector = "#inlineUserPassword, [name='password'], input[type='password']";
    public const string GlassdoorEmailSubmitSelector = "[data-test='email-form-button'], button[type='submit']";
    public const string GlassdoorPasswordSubmitSelector = "[data-test='password-form-button'], button[type='submit']";
    public const string GlassdoorNextPageSelector = "[data-test='pagination-next'], button[aria-label='Next']";
    public const int GlassdoorMaxPages = 3;
}
