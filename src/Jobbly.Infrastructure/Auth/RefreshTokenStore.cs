using System.Security.Cryptography;
using System.Text;
using Jobbly.Domain.Entities;
using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Infrastructure.Auth;

public sealed class RefreshTokenStore(JobblyDbContext dbContext)
{
    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == HashToken(token), cancellationToken);

    public void Add(RefreshToken token) => dbContext.RefreshTokens.Add(token);

    public async Task RevokeAllActiveAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var active = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(reason);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}