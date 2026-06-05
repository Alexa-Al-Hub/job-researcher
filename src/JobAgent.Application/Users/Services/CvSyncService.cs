using JobAgent.Application.Cv.Interfaces;
using JobAgent.Application.Skills.Interfaces;
using JobAgent.Application.Users.Interfaces;
using JobAgent.Domain.Entities;
using JobAgent.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace JobAgent.Application.Users.Services;

public class CvSyncService : ICvSyncService
{
    private readonly ICvParsingService _cvParsing;
    private readonly IUserRepository _userRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<CvSyncService> _logger;

    public CvSyncService(
        ICvParsingService cvParsing,
        IUserRepository userRepository,
        ISkillRepository skillRepository,
        ILogger<CvSyncService> logger)
    {
        _cvParsing = cvParsing;
        _userRepository = userRepository;
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<User?> ParseAndSyncAsync(string baseCvPath, CancellationToken ct = default)
    {
        var profile = await _cvParsing.ParseAsync(baseCvPath, ct);

        if (profile is null)
        {
            _logger.LogWarning("CV parsing returned no result. Falling back to existing user.");
            var users = await _userRepository.GetAllAsync(ct);
            return users.FirstOrDefault();
        }

        var email = profile.Email ?? $"{profile.FirstName}.{profile.LastName}@unknown.local".ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            user = new User
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = email,
                YearsOfExperience = profile.YearsOfExperience
            };
            await _userRepository.AddAsync(user, ct);
            _logger.LogInformation("Created user: {FirstName} {LastName} ({Email})",
                user.FirstName, user.LastName, user.Email);
        }
        else
        {
            user.FirstName = profile.FirstName;
            user.LastName = profile.LastName;
            user.YearsOfExperience = profile.YearsOfExperience;
            await _userRepository.UpdateAsync(user, ct);
            _logger.LogInformation("Updated user: {FirstName} {LastName}", user.FirstName, user.LastName);
        }

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

            user.UserSkills = newSkills.Select(s => new UserSkill
            {
                UserId = user.Id,
                SkillId = s.Id
            }).ToList();

            await _userRepository.UpdateAsync(user, ct);
            _logger.LogInformation("Added {Count} skills from CV parsing", newSkills.Count);
        }

        return user;
    }
}
