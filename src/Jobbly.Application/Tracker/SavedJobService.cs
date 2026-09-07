using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Tracker;

// Tracker use cases over saved jobs. Everything is scoped to the caller's user
// id; foreign ids behave as not-found so one user can't probe another's data.
public sealed class SavedJobService(IJobblyDbContext dbContext)
{
    // List saved jobs for a user, paginated. The canonical job snapshot is embedded
    public async Task<SavedJobListResponse> ListAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.SavedJobs
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var saved = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var snapshots = await FetchSnapshotsAsync(
            saved.Select(s => s.CanonicalJobId).Distinct().ToList(), cancellationToken);

        return new SavedJobListResponse
        {
            Items = saved.Select(s => ToDto(s, snapshots)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    // Save a canonical job for a user. The canonical job must exist and not be archived.
    public async Task<SaveJobAttempt> SaveAsync(Guid userId, Guid canonicalJobId, CancellationToken cancellationToken = default)
    {
        var canonicalExists = await dbContext.CanonicalJobs
            .AnyAsync(c => c.Id == canonicalJobId && !c.IsArchived, cancellationToken);
        if (!canonicalExists)
        {
            return SaveJobAttempt.CanonicalNotFound();
        }

        var alreadySaved = await dbContext.SavedJobs
            .AnyAsync(s => s.UserId == userId && s.CanonicalJobId == canonicalJobId, cancellationToken);
        if (alreadySaved)
        {
            return SaveJobAttempt.Duplicate();
        }

        var saved = SavedJob.Create(userId, canonicalJobId);
        dbContext.SavedJobs.Add(saved);
        await dbContext.SaveChangesAsync(cancellationToken);

        var snapshots = await FetchSnapshotsAsync([canonicalJobId], cancellationToken);
        return SaveJobAttempt.Created(ToDto(saved, snapshots));
    }

    // Patch a saved job's status, notes, and/or follow-up date. The canonical job
    public async Task<SavedJobDto?> PatchAsync(
        Guid userId, Guid id, SavedJobStatus? status, string? notes, DateTime? followUpAt,
        CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedJobs
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
        if (saved is null)
        {
            return null;
        }

        saved.Update(status, notes, followUpAt);
        await dbContext.SaveChangesAsync(cancellationToken);

        var snapshots = await FetchSnapshotsAsync([saved.CanonicalJobId], cancellationToken);
        return ToDto(saved, snapshots);
    }

    // Delete a saved job. Returns false if the saved job was not found.
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var saved = await dbContext.SavedJobs
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, cancellationToken);
        if (saved is null)
        {
            return false;
        }

        dbContext.SavedJobs.Remove(saved);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Latest raw job per canonical id, for the compact snapshot embedded in
    // list/save responses. Archived canonicals still resolve - the tracker
    // keeps tracking jobs the discovery feed hides.
    private async Task<Dictionary<Guid, Job>> FetchSnapshotsAsync(
        List<Guid> canonicalIds, CancellationToken cancellationToken)
    {
        if (canonicalIds.Count == 0)
        {
            return new Dictionary<Guid, Job>();
        }

        var jobs = await dbContext.Jobs
            .AsNoTracking()
            .Where(j => j.CanonicalJobId != null && canonicalIds.Contains(j.CanonicalJobId.Value))
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync(cancellationToken);

        return jobs
            .GroupBy(j => j.CanonicalJobId!.Value)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static SavedJobDto ToDto(SavedJob saved, Dictionary<Guid, Job> snapshots) => new(
        saved.Id,
        saved.CanonicalJobId,
        saved.Status,
        saved.Notes,
        saved.AppliedAt,
        saved.FollowUpAt,
        saved.SavedAt,
        saved.UpdatedAt,
        snapshots.TryGetValue(saved.CanonicalJobId, out var job)
            ? new SavedJobItemDto(
                job.CanonicalJobId!.Value,
                job.Title,
                job.CompanyName,
                job.Location,
                job.RemoteType,
                job.SeniorityLevel,
                job.SourceUrl)
            : null);
}
