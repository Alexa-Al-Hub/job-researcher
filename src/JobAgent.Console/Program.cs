using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Applications.Services;
using JobAgent.Application.Common;
using JobAgent.Application.Correspondence.Interfaces;
using JobAgent.Application.Correspondence.Services;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.Jobs.Mapping;
using JobAgent.Application.Jobs.Services;
using JobAgent.Application.Orchestration;
using JobAgent.Application.Users.Interfaces;
using JobAgent.Application.Users.Services;
using JobAgent.Console.Worker;
using JobAgent.Infrastructure;
using JobAgent.Persistence;
using JobAgent.Persistence.Context;
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
    builder.Services.Configure<CredentialOptions>(builder.Configuration.GetSection(CredentialOptions.SectionName));
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
    builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection(ScoringOptions.SectionName));
    builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection(GmailOptions.SectionName));
    builder.Services.Configure<ApplicantProfileOptions>(builder.Configuration.GetSection(ApplicantProfileOptions.SectionName));

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(JobProfile).Assembly);

    // Persistence (DbContext, repositories)
    builder.Services.AddPersistence(builder.Configuration);

    // Infrastructure (scrapers, appliers, CV services)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Application services
    builder.Services.AddScoped<ICvSyncService, CvSyncService>();
    builder.Services.AddScoped<IScrapeService, ScrapeService>();
    builder.Services.AddScoped<IDescriptionService, DescriptionService>();
    builder.Services.AddScoped<IScoringService, ScoringService>();
    builder.Services.AddScoped<ITailorService, TailorService>();
    builder.Services.AddScoped<IApplyService, ApplyService>();
    builder.Services.AddScoped<ICorrespondenceService, CorrespondenceService>();
    builder.Services.AddScoped<IOrchestrator, OrchestratorService>();

    // Hosted service
    builder.Services.AddHostedService<AgentHostedService>();

    var host = builder.Build();

    // Auto-migrate database
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
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
