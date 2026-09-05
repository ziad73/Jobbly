using Jobbly.Application.Users;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Users;

public sealed class UserProfileService(JobblyDbContext dbContext) : IUserProfileService
{
    public async Task<UserProfileDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return profile is null ? null : ToDto(profile);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        profile.Update(
            request.Title,
            request.SeniorityLevel,
            request.ExperienceYears,
            request.TechStack,
            request.Locations,
            request.RemotePreference,
            request.SalaryExpectationMin,
            request.SalaryExpectationMax,
            request.SalaryCurrency,
            request.SalaryPeriod);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task ReplaceSkillsAsync(Guid userId, IReadOnlyList<string> skills, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserSkills
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);

        dbContext.UserSkills.RemoveRange(existing);

        dbContext.UserSkills.AddRange(skills
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .Select(name => UserSkill.Create(userId, name.Trim())));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UserProfileDto ToDto(UserProfile profile) => new(
        profile.UserId,
        profile.Title,
        profile.SeniorityLevel,
        profile.ExperienceYears,
        profile.TechStack,
        profile.Locations,
        profile.RemotePreference,
        profile.SalaryExpectationMin,
        profile.SalaryExpectationMax,
        profile.SalaryCurrency,
        profile.SalaryPeriod);
}