using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Infrastructure.Appliers;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.CvServices;
using JobAgent.Infrastructure.Persistence;
using JobAgent.Infrastructure.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Persistence
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        // Ensure storage directory exists for SQLite
        var dataSource = connectionString.Replace("Data Source=", "");
        var dir = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISearchCriteriaRepository, SearchCriteriaRepository>();

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

        return services;
    }
}
