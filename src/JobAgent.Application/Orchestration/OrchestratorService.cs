using AutoMapper;
using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Cv.Interfaces;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Application.Skills.Interfaces;
using JobAgent.Application.Users.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Orchestration;

public class OrchestratorService : IOrchestrator
{
    private readonly IEnumerable<IScraper> _scrapers;
    private readonly IEnumerable<IApplier> _appliers;
    private readonly IJobRepository _jobRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISearchCriteriaRepository _searchCriteriaRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ICvTailoringService _cvTailoring;
    private readonly ICvParsingService _cvParsing;
    private readonly IMapper _mapper;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly IOptions<RateLimitOptions> _rateLimitOptions;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        IEnumerable<IScraper> scrapers,
        IEnumerable<IApplier> appliers,
        IJobRepository jobRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        ISearchCriteriaRepository searchCriteriaRepository,
        ISkillRepository skillRepository,
        ICvTailoringService cvTailoring,
        ICvParsingService cvParsing,
        IMapper mapper,
        IOptions<AgentOptions> agentOptions,
        IOptions<RateLimitOptions> rateLimitOptions,
        ILogger<OrchestratorService> logger)
    {
        _scrapers = scrapers;
        _appliers = appliers;
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _searchCriteriaRepository = searchCriteriaRepository;
        _skillRepository = skillRepository;
        _cvTailoring = cvTailoring;
        _cvParsing = cvParsing;
        _mapper = mapper;
        _agentOptions = agentOptions;
        _rateLimitOptions = rateLimitOptions;
        _logger = logger;
    }

    // TODO: JRC-008 — split into separate services (ScrapeService, CvTailoringService, ApplyService) to respect SRP
    public async Task RunAsync(CancellationToken ct = default)
    {
        var options = _agentOptions.Value;
        var enabledPlatforms = options.EnabledPlatforms;

        _logger.LogInformation("Starting orchestrator. DryRun={DryRun}, Platforms={Platforms}",
            options.DryRun, string.Join(", ", enabledPlatforms));

        // Phase 0: Parse CV → create/update User and Skills
        var user = await ParseCvAndSyncUserAsync(options.BaseCvPath, ct);

        if (user is null)
        {
            _logger.LogError("No user available. CV parsing failed and no existing user found.");
            return;
        }

        // TODO: JRC-004 — SearchCriteria created from CV parsing (preferred roles + locations)
        var activeCriteria = await _searchCriteriaRepository.GetActiveAsync(ct);

        if (activeCriteria.Count == 0)
        {
            _logger.LogError("No active search criteria found. Run CV parsing first (JRC-002).");
            return;
        }

        _logger.LogInformation("User: {FirstName} {LastName}, Active criteria: {Count}",
            user.FirstName, user.LastName, activeCriteria.Count);

        // Phase 1: Scrape — iterate all active criteria
        foreach (var criteria in activeCriteria)
        {
            _logger.LogInformation("Searching: \"{Keywords}\" in \"{Location}\"",
                criteria.Keywords, criteria.Location);

            // Use per-criteria platforms if specified, otherwise fall back to global config
            var platforms = criteria.Platforms.Count > 0
                ? _scrapers.Where(s => criteria.Platforms.Contains(s.Platform.ToString()))
                : _scrapers.Where(s => enabledPlatforms.Contains(s.Platform));

            foreach (var scraper in platforms)
            {
                ct.ThrowIfCancellationRequested();
                _logger.LogInformation("Scraping {Platform} for \"{Keywords}\"...",
                    scraper.Platform, criteria.Keywords);

                try
                {
                    var jobRequests = await scraper.ScrapeAsync(criteria.Keywords, criteria.Location, ct);
                    var newCount = 0;

                    foreach (var request in jobRequests)
                    {
                        if (await _jobRepository.ExistsByUrlAsync(request.Url, ct))
                            continue;

                        var job = _mapper.Map<Job>(request);
                        job.SearchCriteriaId = criteria.Id;
                        await _jobRepository.AddAsync(job, ct);

                        await _applicationRepository.CreateForJobAsync(job.Id, user.Id, ct);
                        newCount++;
                    }

                    _logger.LogInformation("Scraped {Total} jobs from {Platform}, {New} new",
                        jobRequests.Count, scraper.Platform, newCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping {Platform}", scraper.Platform);
                }
            }
        }

        // Phase 2: Tailor CVs
        // TODO: JRC-003 — score jobs against user skill profile before tailoring
        var foundApps = await _applicationRepository.GetByStatusAsync(ApplicationStatus.Found, ct);
        _logger.LogInformation("Found {Count} applications needing CV tailoring", foundApps.Count);

        foreach (var app in foundApps)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var tailoredPath = await _cvTailoring.TailorAsync(app.Job, options.BaseCvPath, ct);
                app.CvPath = tailoredPath ?? options.BaseCvPath;
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.CvTailored, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tailoring CV for application {AppId}", app.Id);
            }
        }

        // Phase 3: Apply
        var todayCount = await _applicationRepository.GetTodayApplicationCountAsync(user.Id, ct);
        var remaining = options.MaxApplicationsPerDay - todayCount;

        if (remaining <= 0)
        {
            _logger.LogWarning("Daily application limit reached ({Max})", options.MaxApplicationsPerDay);
            return;
        }

        var appsToApply = await _applicationRepository.GetByStatusAsync(ApplicationStatus.CvTailored, ct);
        var activeAppliers = _appliers.Where(a => enabledPlatforms.Contains(a.Platform))
            .ToDictionary(a => a.Platform);

        var applied = 0;
        foreach (var app in appsToApply)
        {
            if (applied >= remaining)
            {
                _logger.LogWarning("Reached daily limit during apply phase");
                break;
            }

            ct.ThrowIfCancellationRequested();

            if (!activeAppliers.TryGetValue(app.Job.Platform, out var applier))
            {
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
                continue;
            }

            if (options.DryRun)
            {
                _logger.LogInformation("[DRY RUN] Would apply to: {Title} at {Company} ({Platform})",
                    app.Job.Title, app.Job.Company, app.Job.Platform);
                continue;
            }

            try
            {
                var success = await applier.ApplyAsync(app.Job, ct);
                if (success)
                {
                    app.AppliedAt = DateTime.UtcNow;
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.Applied, ct);
                    applied++;
                }
                else
                {
                    await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
                }

                await Task.Delay(_rateLimitOptions.Value.DelayBetweenApplicationsMs, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying to job {JobId}", app.JobId);
                await _applicationRepository.UpdateStatusAsync(app, ApplicationStatus.ManualFollowUp, ct);
            }
        }

        _logger.LogInformation("Orchestrator complete. Applied to {Count} jobs today (total: {Total})",
            applied, todayCount + applied);
    }

    private async Task<User?> ParseCvAndSyncUserAsync(string baseCvPath, CancellationToken ct)
    {
        var profile = await _cvParsing.ParseAsync(baseCvPath, ct);

        if (profile is null)
        {
            _logger.LogWarning("CV parsing returned no result. Falling back to existing user.");
            var users = await _userRepository.GetAllAsync(ct);
            return users.FirstOrDefault();
        }

        // Upsert user
        var email = profile.Email ?? $"{profile.FirstName}.{profile.LastName}@unknown.local".ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            user = new User
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = email
            };
            await _userRepository.AddAsync(user, ct);
            _logger.LogInformation("Created user: {FirstName} {LastName} ({Email})",
                user.FirstName, user.LastName, user.Email);
        }
        else
        {
            user.FirstName = profile.FirstName;
            user.LastName = profile.LastName;
            await _userRepository.UpdateAsync(user, ct);
            _logger.LogInformation("Updated user: {FirstName} {LastName}", user.FirstName, user.LastName);
        }

        // Sync skills: remove old CvParsed skills, add new ones
        var oldSkills = await _skillRepository.GetBySourceAsync(SkillSource.CvParsed, ct);
        if (oldSkills.Count > 0)
        {
            await _skillRepository.RemoveRangeAsync(oldSkills, ct);
            _logger.LogInformation("Removed {Count} old CvParsed skills", oldSkills.Count);
        }

        var newSkills = profile.Skills.Select(s => new Skill
        {
            Name = s.Name,
            Category = Enum.TryParse<SkillCategory>(s.Category, ignoreCase: true, out var cat)
                ? cat
                : SkillCategory.Other,
            Source = SkillSource.CvParsed
        }).ToList();

        if (newSkills.Count > 0)
        {
            await _skillRepository.AddRangeAsync(newSkills, ct);

            // Link skills to user via UserSkills
            var userSkills = newSkills.Select(s => new UserSkill
            {
                UserId = user.Id,
                SkillId = s.Id
            });

            user.UserSkills = userSkills.ToList();
            await _userRepository.UpdateAsync(user, ct);

            _logger.LogInformation("Added {Count} skills from CV parsing", newSkills.Count);
        }

        return user;
    }
}
