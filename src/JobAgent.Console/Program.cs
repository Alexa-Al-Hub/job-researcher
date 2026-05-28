using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Application.Services;
using JobAgent.Console.Worker;
using JobAgent.Infrastructure.Appliers;
using JobAgent.Infrastructure.Browser;
using JobAgent.Infrastructure.CvServices;
using JobAgent.Infrastructure.Persistence;
using JobAgent.Infrastructure.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/job-agent-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog();

    // Options
    builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));
    builder.Services.Configure<SearchOptions>(builder.Configuration.GetSection(SearchOptions.SectionName));
    builder.Services.Configure<CredentialOptions>(builder.Configuration.GetSection(CredentialOptions.SectionName));
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));

    // Persistence
    var dbPath = builder.Configuration.GetSection("Agent")["DatabasePath"] ?? "storage/jobs.db";
    Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? "storage");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
    builder.Services.AddScoped<IJobRepository, JobRepository>();

    // Browser
    builder.Services.AddSingleton<PlaywrightBrowserFactory>();

    // Scrapers
    builder.Services.AddScoped<IScraper, DouScraper>();
    builder.Services.AddScoped<IScraper, IndeedScraper>();
    builder.Services.AddScoped<IScraper, LinkedInScraper>();
    builder.Services.AddScoped<IScraper, GlassdoorScraper>();

    // Appliers
    builder.Services.AddScoped<IApplier, DouApplier>();
    builder.Services.AddScoped<IApplier, IndeedApplier>();
    builder.Services.AddScoped<IApplier, LinkedInApplier>();
    builder.Services.AddScoped<IApplier, GlassdoorApplier>();

    // CV Services
    builder.Services.AddSingleton<ICvDocxService, DocxCvService>();
    builder.Services.AddScoped<ICvTailoringService, ClaudeCvTailoringService>();

    // Orchestrator
    builder.Services.AddScoped<IOrchestrator, OrchestratorService>();

    // Hosted service
    builder.Services.AddHostedService<AgentHostedService>();

    var host = builder.Build();

    // Auto-migrate database
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Job Agent terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
