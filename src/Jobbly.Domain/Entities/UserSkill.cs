namespace Jobbly.Domain.Entities;

public sealed class UserSkill
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;

    private UserSkill()
    {
    }

    public static UserSkill Create(Guid userId, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        Name = name
    };
}