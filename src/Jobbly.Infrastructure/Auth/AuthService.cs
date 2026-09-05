using Jobbly.Application.Auth;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Identity;
using Jobbly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JobblyDbContext dbContext) : IAuthService
{
    public async Task<CurrentUserDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return null;
        }

        var user = ApplicationUser.Create(request.Email, request.FullName);

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        // Profiles are always present (1:1), so create one at registration.
        dbContext.UserProfiles.Add(UserProfile.Create(user.Id));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<CurrentUserDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return ToDto(user);
    }

    public Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        // No server-side session to clear yet - token revocation arrives with JWT.
        return Task.FromResult(true);
    }

    private static CurrentUserDto ToDto(ApplicationUser user) => new(user.Id, user.Email!, user.FullName);
}