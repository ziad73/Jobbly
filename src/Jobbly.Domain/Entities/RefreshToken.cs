namespace Jobbly.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAtUtc = expiresAtUtc,
        CreatedAtUtc = DateTime.UtcNow
    };

    /// <summary>Computed - neither revoked nor expired.</summary>
    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void Revoke(string reason)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        RevokedReason = reason;
    }

    public void MarkReplacedBy(Guid replacementId) => ReplacedByTokenId = replacementId;
}