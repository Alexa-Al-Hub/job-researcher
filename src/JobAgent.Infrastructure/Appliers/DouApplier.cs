using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Abstractions;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Infrastructure.Appliers;

public class DouApplier : BaseApplier
{
    public override Platform Platform => Platform.Dou;
    protected override string ApplyButtonSelector => ApplierConstants.DouApplyButton;

    public DouApplier(
        PlaywrightBrowserFactory browserFactory,
        IApplicationFormService formService,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<DouApplier> logger)
        : base(browserFactory, formService, rateLimitOptions, logger)
    {
    }

    // Many DOU vacancies redirect to an external company site; in that case no on-page
    // form is found and the application is routed to manual follow-up.
}
