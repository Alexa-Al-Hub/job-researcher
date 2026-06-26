namespace JobAgent.Infrastructure.Constants;

public static class ApplierConstants
{
    public const int MaxFormSteps = 8;

    // Generic form controls (Playwright supports :has-text() and [attr*='x' i]).
    public const string FileInputSelector = "input[type='file']";
    public const string FillableFieldSelector =
        "input:not([type='hidden']):not([type='file']):not([type='submit']):not([type='button']):not([type='checkbox']):not([type='radio']), textarea, select";
    public const string FormFieldProbe =
        "[role='dialog'] input, [role='dialog'] textarea, form input:not([type='hidden']), form textarea, iframe[src*='apply']";
    public const string SubmitSelector =
        "button[aria-label*='Submit application' i], button:has-text('Submit application'), button:has-text('Submit'), button[type='submit']";
    public const string NextSelector =
        "button[aria-label*='Continue' i], button:has-text('Continue'), button:has-text('Next'), button:has-text('Review')";

    // Per-platform "apply" buttons (best-effort; will need tuning against the live sites).
    public const string LinkedInApplyButton = "button.jobs-apply-button, .jobs-s-apply button";
    public const string IndeedApplyButton =
        "[data-testid='indeedApply'], #indeedApplyButton, .jobsearch-IndeedApplyButton-newDesign button";
    public const string GlassdoorApplyButton = "[data-test='easyApply'], button.applyButton, a.applyButton";
    public const string DouApplyButton = "a.reply, .b-apply, .vacancy-section a.reply";
}
