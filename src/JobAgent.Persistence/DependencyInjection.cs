using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Application.Skills.Interfaces;
using JobAgent.Application.Users.Interfaces;
using JobAgent.Persistence.Context;
using JobAgent.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobAgent.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var provider = configuration.GetValue<string>("ConnectionStrings:DatabaseProvider") ?? "Sqlite";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                // Ensure storage directory exists for SQLite
                var dataSource = connectionString.Replace("Data Source=", "");
                var dir = Path.GetDirectoryName(dataSource);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISearchCriteriaRepository, SearchCriteriaRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ICorrespondenceRepository, CorrespondenceRepository>();

        return services;
    }
}
