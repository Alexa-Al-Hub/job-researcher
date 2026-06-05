using JobAgent.Application.Interfaces;
using JobAgent.Application.Options;
using JobAgent.Application.Services;
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

    // Persistence (DbContext, repositories)
    builder.Services.AddPersistence(builder.Configuration);

    // Infrastructure (scrapers, appliers, CV services)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Application services
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
