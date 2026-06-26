using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Abstractions;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.Appliers;

public class IndeedApplier : BaseApplier
{
    public override Platform Platform => Platform.Indeed;
    protected override string ApplyButtonSelector => ApplierConstants.IndeedApplyButton;

    public IndeedApplier(
        PlaywrightBrowserFactory browserFactory,
        IApplicationFormService formService,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<IndeedApplier> logger)
        : base(browserFactory, formService, rateLimitOptions, logger)
    {
    }

    // Indeed has no credentials in this project; the Easy Apply flow proceeds anonymously
    // and falls back to manual follow-up when it hits a sign-in or screening wall.
}
