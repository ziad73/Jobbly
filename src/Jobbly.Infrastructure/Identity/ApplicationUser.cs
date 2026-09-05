using Microsoft.AspNetCore.Identity;

namespace Jobbly.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static ApplicationUser Create(string email, string fullName) => new()
    {
        Id = Guid.CreateVersion7(),
        UserName = email,
        Email = email,
        FullName = fullName,
        CreatedAt = DateTime.UtcNow
    };
}