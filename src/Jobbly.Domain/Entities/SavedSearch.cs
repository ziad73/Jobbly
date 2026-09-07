namespace Jobbly.Domain.Entities;

public sealed class SavedSearch
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    public string Name { get; private set; } = null!;
    public string CriteriaJson { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SavedSearch()
    {
    }

    public static SavedSearch Create(Guid userId, string name, string criteriaJson)
    {
        var now = DateTime.UtcNow;

        return new SavedSearch
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name,
            CriteriaJson = criteriaJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string? name = null, string? criteriaJson = null)
    {
        Name = name ?? Name;
        CriteriaJson = criteriaJson ?? CriteriaJson;
        UpdatedAt = DateTime.UtcNow;
    }
}