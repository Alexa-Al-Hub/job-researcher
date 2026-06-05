using JobAgent.Application.Applications.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Infrastructure.Browser;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Appliers;

public class IndeedApplier : IApplier
{
    public Platform Platform => Platform.Indeed;
    private readonly PlaywrightBrowserFactory _browserFactory;
    private readonly ILogger<IndeedApplier> _logger;

    public IndeedApplier(PlaywrightBrowserFactory browserFactory, ILogger<IndeedApplier> logger)
    {
        _browserFactory = browserFactory;
        _logger = logger;
    }

    public async Task<bool> ApplyAsync(Job job, CancellationToken ct = default)
    {
        var page = await _browserFactory.NewPageAsync();
        try
        {
            await page.GotoAsync(job.Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            var applyButton = await page.QuerySelectorAsync("[data-testid='indeedApply'], .jobsearch-IndeedApplyButton-newDesign");
            if (applyButton == null)
            {
                _logger.LogWarning("No Indeed Easy Apply button found for {Url}", job.Url);
                return false;
            }

            // Indeed Easy Apply flow requires authentication and form filling
            // This is a placeholder for full implementation
            _logger.LogInformation("Indeed apply button found for: {Title}. Full auto-apply not yet implemented.", job.Title);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying to Indeed job {Url}", job.Url);
            return false;
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
