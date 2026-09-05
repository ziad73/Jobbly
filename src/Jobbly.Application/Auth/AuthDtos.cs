using Jobbly.Domain.Enums;

namespace Jobbly.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserDto(Guid Id, string Email, string FullName);