using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Jobbly.Application.Jobs;

// Read use case over the deduplicated job feed. Queries raw Job rows, groups
// them by canonical job (one listing per deduplicated posting) and projects the
// representative entity to a client-friendly DTO. Public, no auth.
//
// Npgsql-specific operations (full-text search, location ILIKE) go through the
// IFullTextSearch port so this layer stays free of database coupling.
public sealed class JobSearchService(IFullTextSearch fullTextSearch, IJobblyDbContext dbContext)
{
    public async Task<JobSearchResponse> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page ?? 1);
        var pageSize = Math.Clamp(query.PageSize ?? PageSizeOptions.DefaultPageSize, 1, PageSizeOptions.MaxPageSize);

        // Exclude archived canonicals via a subquery (avoids Include + GroupBy
        // translation issues with EF Core).
        var activeCanonicalIds = dbContext.CanonicalJobs
            .Where(c => !c.IsArchived)
            .Select(c => c.Id);

        var jobs = dbContext.Jobs
            .AsNoTracking()
            .Where(j => j.CanonicalJobId != null && activeCanonicalIds.Contains(j.CanonicalJobId!.Value));

        jobs = ApplyFilters(jobs, query);

        // One listing per canonical job.
        var totalCount = await jobs
            .Select(j => j.CanonicalJobId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Two-query approach: (1) get paginated canonical IDs with correct sort,
        // (2) fetch the full entities. This avoids the EF Core GroupBy + Select(First)
        // translation issue that causes EmptyProjectionMember.
        var pagedIds = await GetPagedCanonicalIds(jobs, query, page, pageSize, cancellationToken);
        var representatives = await FetchByCanonicalIds(jobs, pagedIds, cancellationToken);

        // Second query: fetch SourceCount for the displayed canonicals.
        var canonicalIdSet = representatives.Select(j => j.CanonicalJobId!.Value).Distinct().ToList();
        var sourceCounts = await dbContext.CanonicalJobs
            .Where(c => canonicalIdSet.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.SourceCount, cancellationToken);

        var items = representatives.Select(j => MapListItem(j, sourceCounts)).ToList();

        return new JobSearchResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<JobDetail?> GetByIdAsync(Guid canonicalId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Jobs
            .AsNoTracking()
            .Include(j => j.CanonicalJob)
            .Where(j => j.CanonicalJobId == canonicalId && !j.CanonicalJob!.IsArchived)
            .OrderByDescending(j => j.PostedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return job is null ? null : MapDetail(job);
    }

    // Step 1: Get distinct canonical IDs with sorting and paging applied.
    private async Task<List<Guid>> GetPagedCanonicalIds(
        IQueryable<Job> jobs, JobSearchQuery query, int page, int pageSize, CancellationToken ct)
    {
        var sort = query.Sort ?? JobSearchSort.Relevance;

        if (sort == JobSearchSort.Relevance && !string.IsNullOrWhiteSpace(query.Q))
        {
            // Relevance: rank via FTS server-side, then group and paginate IDs.
            var ranked = fullTextSearch.OrderByRelevance(jobs, query.Q);
            var groupedIds = ranked
                .GroupBy(j => j.CanonicalJobId)
                .Select(g => g.First().CanonicalJobId!.Value);

            return await groupedIds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        // Date / Salary sort: group by canonical, compute the sort aggregate,
        // order, then paginate. GroupBy + Key + Max is reliably translated.
        return sort switch
        {
            JobSearchSort.Salary => await jobs
                .GroupBy(j => j.CanonicalJobId)
                .Select(g => new { Id = g.Key!.Value, MaxSalary = g.Max(j => j.SalaryMax ?? 0) })
                .OrderByDescending(x => x.MaxSalary)
                .Select(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct),
            _ => await jobs
                .GroupBy(j => j.CanonicalJobId)
                .Select(g => new { Id = g.Key!.Value, Latest = g.Max(j => j.PostedAt) })
                .OrderByDescending(x => x.Latest)
                .Select(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
        };
    }

    // Step 2: Fetch full Job entities for the given canonical IDs.
    private static async Task<List<Job>> FetchByCanonicalIds(
        IQueryable<Job> jobs, List<Guid> canonicalIds, CancellationToken ct)
    {
        if (canonicalIds.Count == 0)
        {
            return [];
        }

        return await jobs
            .Where(j => canonicalIds.Contains(j.CanonicalJobId!.Value))
            .ToListAsync(ct);
    }

    private IQueryable<Job> ApplyFilters(IQueryable<Job> jobs, JobSearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            jobs = fullTextSearch.ApplyKeyword(jobs, query.Q);
        }

        if (query.Tags is { Length: > 0 })
        {
            foreach (var tag in query.Tags)
            {
                var normalized = tag.Trim();
                if (normalized.Length > 0)
                {
                    jobs = jobs.Where(j => j.TechStack.Contains(normalized));
                }
            }
        }

        if (query.Seniority is { } seniority)
        {
            jobs = jobs.Where(j => j.SeniorityLevel == seniority);
        }

        if (query.Remote is { } remote)
        {
            jobs = jobs.Where(j => j.RemoteType == remote);
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            jobs = fullTextSearch.ApplyLocation(jobs, query.Location.Trim());
        }

        if (query.SalaryMin is { } salaryMin)
        {
            jobs = jobs.Where(j => j.SalaryMin >= salaryMin);
        }

        if (query.SalaryMax is { } salaryMax)
        {
            jobs = jobs.Where(j => j.SalaryMax <= salaryMax);
        }

        if (!string.IsNullOrWhiteSpace(query.SalaryCurrency))
        {
            jobs = jobs.Where(j => j.SalaryCurrency == query.SalaryCurrency);
        }

        return jobs;
    }

    private static JobListItem MapListItem(Job j, IReadOnlyDictionary<Guid, int> sourceCounts) => new()
    {
        CanonicalJobId = j.CanonicalJobId!.Value,
        Title = j.Title,
        Company = j.CompanyName,
        Location = j.Location,
        RemoteType = j.RemoteType,
        Seniority = j.SeniorityLevel,
        TechStack = j.TechStack,
        SalaryMin = j.SalaryMin,
        SalaryMax = j.SalaryMax,
        SalaryCurrency = j.SalaryCurrency,
        SalaryPeriod = j.SalaryPeriod,
        PostedAtUtc = j.PostedAt,
        SourceCount = sourceCounts.TryGetValue(j.CanonicalJobId!.Value, out var count) ? count : 1,
        SourceUrl = j.SourceUrl
    };

    private static JobDetail MapDetail(Job j) => new()
    {
        CanonicalJobId = j.CanonicalJobId!.Value,
        Title = j.Title,
        Company = j.CompanyName,
        Location = j.Location,
        RemoteType = j.RemoteType,
        Seniority = j.SeniorityLevel,
        EmploymentType = j.EmploymentType,
        TechStack = j.TechStack,
        SalaryMin = j.SalaryMin,
        SalaryMax = j.SalaryMax,
        SalaryCurrency = j.SalaryCurrency,
        SalaryPeriod = j.SalaryPeriod,
        PostedAtUtc = j.PostedAt,
        SourceCount = j.CanonicalJob?.SourceCount ?? 1,
        SourceUrl = j.SourceUrl,
        Overview = j.DescriptionStructured?.TryGetValue("Overview", out var overview) is true ? overview : Truncate(j.DescriptionRaw),
        Requirements = j.Requirements,
        NiceToHaves = j.NiceToHaves,
        Description = j.DescriptionStructured
    };

    private static string? Truncate(string? text, int maxLength = 400)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "…";
    }
}
