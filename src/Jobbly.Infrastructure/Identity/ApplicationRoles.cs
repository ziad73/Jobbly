namespace Jobbly.Infrastructure.Identity;

public static class ApplicationRoles
{
    /// <summary>Default role, assigned automatically at registration.</summary>
    public const string User = "User";

    /// <summary>Elevated role, assigned manually. Gates the ingestion pipeline and Hangfire dashboard.</summary>
    public const string Admin = "Admin";
}