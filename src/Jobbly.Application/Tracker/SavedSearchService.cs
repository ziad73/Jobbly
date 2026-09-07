using System.Text.Json;
using Jobbly.Application.Common;
using Jobbly.Application.Jobs;
using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Tracker;

// Saved-search use cases. Criteria are stored as JSON and rehydrated to a
// JobSearchQuery on run, so the dashboard feed is literally the same search
// pipeline as GET /api/jobs. Scoped to the caller like SavedJobService.
public sealed class SavedSearchService(IJobblyDbContext dbContext, JobSearchService search)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Lists all saved searches for a user, most recent first.
    public async Task<IReadOnlyList<SavedSearchDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var searches = await dbContext.SavedSearches
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return searches.Select(ToDto).ToList();
    }

    // Create a new saved search. The criteria are stored as JSON and rehydrated
    public async Task<SavedSearchDto> CreateAsync(
        Guid userId, string name, JobSearchQuery criteria, CancellationToken cancellationToken = default)
    {
        var saved = SavedSearch.Create(userId, name, JsonSerializer.Serialize(criteria, Json));
        dbContext.SavedSearches.Add(saved);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(saved);
    }

    // Patch a saved search's name and/or criteria. The criteria are stored as JSON and rehydrated
    public async Task<SavedSearchDto?> PatchAsync(
        Guid userId, Guid id, string? name, JobSearchQuery? criteria, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
        if (saved is null)
        {
            return null;
        }

        saved.Update(name, criteria is null ? null : JsonSerializer.Serialize(criteria, Json));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(saved);
    }

    // Delete a saved search. Returns false if the saved search was not found.
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedSearches
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
        if (saved is null)
        {
            return false;
        }

        dbContext.SavedSearches.Remove(saved);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Executes a saved search -Search Filters- on search jobs pipeline and returns current matches jobs.
    // Returns null if the saved search was not found or if the stored criteria are invalid.

    // What it's used for: powering the dashboard feed -logged-in user's personal home page with suggested jobs- — letting a user re-run a previously saved filter set and see current matching jobs, instead of retyping the same GET /api/jobs query manually.
    public async Task<JobSearchResponse?> RunAsync(
        Guid userId, Guid id, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedSearches
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
        if (saved is null)
        {
            return null;
        }

        JobSearchQuery? criteria;
        try
        {
            criteria = JsonSerializer.Deserialize<JobSearchQuery>(saved.CriteriaJson, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (criteria is null)
        {
            return null;
        }

        return await search.SearchAsync(criteria with { Page = page, PageSize = pageSize }, cancellationToken);
    }

    private static SavedSearchDto ToDto(SavedSearch saved) => new(
        saved.Id,
        saved.Name,
        JsonSerializer.Deserialize<JobSearchQuery>(saved.CriteriaJson, Json)!,
        saved.CreatedAt,
        saved.UpdatedAt);
}
