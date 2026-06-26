using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Abstractions;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using static JobAgent.Infrastructure.Constants.ScraperConstants;

namespace JobAgent.Infrastructure.Appliers;

public class LinkedInApplier : BaseApplier
{
    public override Platform Platform => Platform.LinkedIn;
    protected override string ApplyButtonSelector => ApplierConstants.LinkedInApplyButton;

    private readonly IOptions<CredentialOptions> _credentials;

    public LinkedInApplier(
        PlaywrightBrowserFactory browserFactory,
        IApplicationFormService formService,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<LinkedInApplier> logger)
        : base(browserFactory, formService, rateLimitOptions, logger)
    {
        _credentials = credentials;
    }

    protected override bool CanExecute()
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.LinkedInEmail) || string.IsNullOrEmpty(creds.LinkedInPassword))
        {
            Logger.LogWarning("LinkedIn applier skipped — credentials not configured");
            return false;
        }
        return true;
    }

    protected override async Task BeforeApplyAsync(IPage page, CancellationToken ct)
    {
        var creds = _credentials.Value;
        await page.GotoAsync(LinkedInLoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.FillAsync(LinkedInUsernameSelector, creds.LinkedInEmail!);
        await page.FillAsync(LinkedInPasswordSelector, creds.LinkedInPassword!);
        await page.ClickAsync(LinkedInSubmitSelector);
        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = LinkedInLoginTimeout });
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        Logger.LogInformation("LinkedIn applier: logged in as {Email}", creds.LinkedInEmail);
    }
}
