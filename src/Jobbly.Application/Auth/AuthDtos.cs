using System.ComponentModel.DataAnnotations;

namespace Jobbly.Application.Auth;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(254)] string Email,
    [property: Required, MinLength(8), MaxLength(128)] string Password,
    [property: Required, MinLength(1), MaxLength(100)] string FullName);

public sealed record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public sealed record RefreshRequest(
    [property: Required] string RefreshToken);

public sealed record LogoutRequest(
    [property: Required] string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    CurrentUserDto User);

public sealed record CurrentUserDto(Guid Id, string Email, string FullName);

public enum RegisterFailureKind
{
    EmailTaken,
    InvalidInput
}

public sealed record RegisterAttempt(AuthResponse? Response, RegisterFailureKind? Failure, IReadOnlyList<string>? Errors)
{
    public static RegisterAttempt Succeeded(AuthResponse response) => new(response, null, null);

    public static RegisterAttempt EmailTaken() => new(null, RegisterFailureKind.EmailTaken, null);

    public static RegisterAttempt Invalid(IReadOnlyList<string> errors) => new(null, RegisterFailureKind.InvalidInput, errors);
}