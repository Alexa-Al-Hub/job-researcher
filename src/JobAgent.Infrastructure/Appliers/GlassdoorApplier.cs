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

public class GlassdoorApplier : BaseApplier
{
    public override Platform Platform => Platform.Glassdoor;
    protected override string ApplyButtonSelector => ApplierConstants.GlassdoorApplyButton;

    private readonly IOptions<CredentialOptions> _credentials;

    public GlassdoorApplier(
        PlaywrightBrowserFactory browserFactory,
        IApplicationFormService formService,
        IOptions<CredentialOptions> credentials,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<GlassdoorApplier> logger)
        : base(browserFactory, formService, rateLimitOptions, logger)
    {
        _credentials = credentials;
    }

    protected override bool CanExecute()
    {
        var creds = _credentials.Value;
        if (string.IsNullOrEmpty(creds.GlassdoorEmail) || string.IsNullOrEmpty(creds.GlassdoorPassword))
        {
            Logger.LogWarning("Glassdoor applier skipped — credentials not configured");
            return false;
        }
        return true;
    }

    protected override async Task BeforeApplyAsync(IPage page, CancellationToken ct)
    {
        var creds = _credentials.Value;
        await page.GotoAsync(GlassdoorLoginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var emailInput = await page.QuerySelectorAsync(GlassdoorEmailSelector);
        if (emailInput != null)
        {
            await emailInput.FillAsync(creds.GlassdoorEmail!);
            var continueBtn = await page.QuerySelectorAsync(GlassdoorEmailSubmitSelector);
            if (continueBtn != null)
                await continueBtn.ClickAsync();
            await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        }

        var passwordInput = await page.QuerySelectorAsync(GlassdoorPasswordSelector);
        if (passwordInput != null)
        {
            await passwordInput.FillAsync(creds.GlassdoorPassword!);
            var signInBtn = await page.QuerySelectorAsync(GlassdoorPasswordSubmitSelector);
            if (signInBtn != null)
                await signInBtn.ClickAsync();
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(RateLimit.MinDelayBetweenRequestsMs, ct);
        Logger.LogInformation("Glassdoor applier: login complete");
    }
}
