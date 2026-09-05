using Jobbly.Application.Auth;

namespace Jobbly.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register",
            async (RegisterRequest request, IAuthService auth, CancellationToken ct) =>
            {
                var user = await auth.RegisterAsync(request, ct);
                return user is null
                    ? Results.Conflict(new { message = "A user with this email already exists." })
                    : Results.Created($"/api/users/me", user);
            })
            .WithName("RegisterUser")
            .WithSummary("Create a user account")
            .WithDescription("Registers a new user and auto-creates their profile (1:1). Returns the created user.");

        group.MapPost("/login",
            async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
            {
                var user = await auth.LoginAsync(request, ct);
                return user is null
                    ? Results.Unauthorized()
                    : Results.Ok(user);
            })
            .WithName("LoginUser")
            .WithSummary("Verify credentials")
            .WithDescription("Verifies an email/password pair against the identity store. Token issuance (JWT) lands in a later pass.");

        group.MapPost("/logout",
            async (IAuthService auth, CancellationToken ct) =>
            {
                await auth.LogoutAsync(ct);
                return Results.NoContent();
            })
            .WithName("LogoutUser")
            .WithSummary("Log out (no-op today)")
            .WithDescription("Placeholder - server-side token revocation arrives with JWT support.");

        return app;
    }
}