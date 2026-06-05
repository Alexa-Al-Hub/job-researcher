using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Cv.Interfaces;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Infrastructure.Appliers;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.CvServices;
using JobAgent.Infrastructure.Scrapers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Browser
        services.AddSingleton<PlaywrightBrowserFactory>();

        // Scrapers
        services.AddScoped<IScraper, DouScraper>();
        services.AddScoped<IScraper, IndeedScraper>();
        services.AddScoped<IScraper, LinkedInScraper>();
        services.AddScoped<IScraper, GlassdoorScraper>();

        // Appliers
        services.AddScoped<IApplier, DouApplier>();
        services.AddScoped<IApplier, IndeedApplier>();
        services.AddScoped<IApplier, LinkedInApplier>();
        services.AddScoped<IApplier, GlassdoorApplier>();

        // CV Services
        services.AddSingleton<ICvDocxService, DocxCvService>();
        services.AddScoped<ICvTailoringService, ClaudeCvTailoringService>();
        services.AddScoped<ICvParsingService, ClaudeCvParsingService>();
        services.AddScoped<IJobScoringService, ClaudeJobScoringService>();

        return services;
    }
}
