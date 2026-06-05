using AutoMapper;
using AutoMapper.QueryableExtensions;
using JobAgent.Application.Applications.DTOs;
using JobAgent.Application.Applications.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using JobAgent.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace JobAgent.Persistence.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public ApplicationRepository(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<Domain.Entities.Application> CreateForJobAsync(int jobId, int userId, CancellationToken ct = default)
    {
        var application = new Domain.Entities.Application
        {
            JobId = jobId,
            UserId = userId,
            Status = ApplicationStatus.Found,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Applications.Add(application);
        await _db.SaveChangesAsync(ct);

        return application;
    }

    public async Task UpdateStatusAsync(Domain.Entities.Application application, ApplicationStatus newStatus, CancellationToken ct = default)
    {
        var oldStatus = application.Status;
        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;

        _db.Applications.Update(application);

        _db.ApplicationStatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Entities.Application>> GetByStatusAsync(ApplicationStatus status, CancellationToken ct = default)
    {
        return await _db.Applications
            .Include(a => a.Job)
            .Where(a => a.Status == status)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetByStatusAsDtoAsync(ApplicationStatus status, CancellationToken ct = default)
    {
        return await _db.Applications
            .Include(a => a.Job)
            .Where(a => a.Status == status)
            .ProjectTo<ApplicationDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }

    public async Task<int> GetTodayApplicationCountAsync(int userId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Applications.CountAsync(a =>
            a.UserId == userId &&
            a.Status == ApplicationStatus.Applied &&
            a.AppliedAt != null &&
            a.AppliedAt.Value.Date == today, ct);
    }

    public async Task<bool> ExistsForJobAndUserAsync(int jobId, int userId, CancellationToken ct = default)
    {
        return await _db.Applications.AnyAsync(a => a.JobId == jobId && a.UserId == userId, ct);
    }
}
