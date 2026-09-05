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
                var response = await auth.RegisterAsync(request, ct);
                return response is null
                    ? Results.Conflict(new { message = "A user with this email already exists." })
                    : Results.Created("/api/users/me", response);
            })
            .WithName("RegisterUser")
            .WithSummary("Create a user account")
            .WithDescription("Registers a new user (auto-creating their 1:1 profile) and returns an access + refresh token pair.");

        group.MapPost("/login",
            async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
            {
                var response = await auth.LoginAsync(request, ct);
                return response is null
                    ? Results.Unauthorized()
                    : Results.Ok(response);
            })
            .WithName("LoginUser")
            .WithSummary("Sign in with email & password")
            .WithDescription("Verifies credentials and returns an access + refresh token pair.");

        group.MapPost("/refresh",
            async (RefreshRequest request, IAuthService auth, CancellationToken ct) =>
            {
                var response = await auth.RefreshAsync(request.RefreshToken, ct);
                return response is null
                    ? Results.Unauthorized()
                    : Results.Ok(response);
            })
            .WithName("RefreshTokens")
            .WithSummary("Rotate a refresh token")
            .WithDescription("Exchanges a valid refresh token for a new access + refresh pair. Replaying a revoked token revokes all the user's sessions (suspected theft).");

        group.MapPost("/logout",
            async (LogoutRequest request, IAuthService auth, CancellationToken ct) =>
            {
                await auth.LogoutAsync(request.RefreshToken, ct);
                return Results.NoContent();
            })
            .WithName("LogoutUser")
            .WithSummary("Revoke a refresh token")
            .WithDescription("Revokes the supplied refresh token so it can no longer be used to obtain new access tokens. Idempotent.");

        return app;
    }
}