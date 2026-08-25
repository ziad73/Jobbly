namespace Jobbly.Domain.Entities;

public sealed class Company
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? Industry { get; private set; }
    public string? SizeRange { get; private set; }
    public string? HqLocation { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Company()
    {
    }

    public static Company Create(
        string name,
        string slug,
        string? website = null,
        string? logoUrl = null,
        string? industry = null,
        string? sizeRange = null,
        string? hqLocation = null,
        string? description = null)
    {
        var now = DateTime.UtcNow;

        return new Company
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug,
            Website = website,
            LogoUrl = logoUrl,
            Industry = industry,
            SizeRange = sizeRange,
            HqLocation = hqLocation,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(
        string? website = null,
        string? logoUrl = null,
        string? industry = null,
        string? sizeRange = null,
        string? hqLocation = null,
        string? description = null)
    {
        Website = website ?? Website;
        LogoUrl = logoUrl ?? LogoUrl;
        Industry = industry ?? Industry;
        SizeRange = sizeRange ?? SizeRange;
        HqLocation = hqLocation ?? HqLocation;
        Description = description ?? Description;
        UpdatedAt = DateTime.UtcNow;
    }
}
