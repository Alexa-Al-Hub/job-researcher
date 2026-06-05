using AutoMapper;
using JobAgent.Application.Applications.Interfaces;
using JobAgent.Application.Common;
using JobAgent.Application.Jobs.Interfaces;
using JobAgent.Application.SearchCriteria.Interfaces;
using JobAgent.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobAgent.Application.Jobs.Services;

public class ScrapeService : IScrapeService
{
    private readonly IEnumerable<IScraper> _scrapers;
    private readonly IJobRepository _jobRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ISearchCriteriaRepository _searchCriteriaRepository;
    private readonly IMapper _mapper;
    private readonly IOptions<AgentOptions> _agentOptions;
    private readonly ILogger<ScrapeService> _logger;

    public ScrapeService(
        IEnumerable<IScraper> scrapers,
        IJobRepository jobRepository,
        IApplicationRepository applicationRepository,
        ISearchCriteriaRepository searchCriteriaRepository,
        IMapper mapper,
        IOptions<AgentOptions> agentOptions,
        ILogger<ScrapeService> logger)
    {
        _scrapers = scrapers;
        _jobRepository = jobRepository;
        _applicationRepository = applicationRepository;
        _searchCriteriaRepository = searchCriteriaRepository;
        _mapper = mapper;
        _agentOptions = agentOptions;
        _logger = logger;
    }

    public async Task ScrapeAsync(User user, CancellationToken ct = default)
    {
        var enabledPlatforms = _agentOptions.Value.EnabledPlatforms;
        var activeCriteria = await _searchCriteriaRepository.GetActiveAsync(ct);

        if (activeCriteria.Count == 0)
        {
            _logger.LogError("No active search criteria found.");
            return;
        }

        foreach (var criteria in activeCriteria)
        {
            _logger.LogInformation("Searching: \"{Keywords}\" in \"{Location}\"",
                criteria.Keywords, criteria.Location);

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
    }
}
