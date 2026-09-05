using Jobbly.Application.Users;

namespace Jobbly.Application.Users;

/// <summary>
/// Own-profile use cases. Identity (and the bearer token that authenticates it)
/// is plumbed in a later pass - for now the user id is supplied explicitly.
/// </summary>
public interface IUserProfileService
{
    Task<UserProfileDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    Task ReplaceSkillsAsync(Guid userId, IReadOnlyList<string> skills, CancellationToken cancellationToken = default);
}