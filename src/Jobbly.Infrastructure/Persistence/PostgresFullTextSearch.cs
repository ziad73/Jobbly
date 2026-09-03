using Jobbly.Application.Jobs;
using Jobbly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Jobbly.Infrastructure.Persistence;

// Implements the Npgsql-specific parts of job search: full-text matching/ranking
// against the generated "SearchVector" tsvector, and case-insensitive location
// matching. Lives in Infrastructure because Npgsql is a persistence concern.
public sealed class PostgresFullTextSearch : IFullTextSearch
{
    // Searches for jobs whose title or description matches the given query,
    public IQueryable<Job> ApplyKeyword(IQueryable<Job> jobs, string query)
    {
        var tsQuery = BuildTsQueryText(query);
        return jobs.Where(j => EF.Property<NpgsqlTsVector>(j, "SearchVector")
            .Matches(EF.Functions.ToTsQuery("english", tsQuery)));
    }

    // Searches for jobs whose location contains the given string, case-insensitively.
    public IQueryable<Job> ApplyLocation(IQueryable<Job> jobs, string location)
    {
        var pattern = $"%{location}%";
        return jobs.Where(j => j.Location != null && EF.Functions.ILike(j.Location, pattern));
    }

    public IQueryable<Job> OrderByRelevance(IQueryable<Job> jobs, string query)
    {
        var tsQuery = BuildTsQueryText(query);
        return jobs.OrderByDescending(j => EF.Property<NpgsqlTsVector>(j, "SearchVector")
            .Rank(EF.Functions.ToTsQuery("english", tsQuery)));
    }

    // Joins the client's plain words into a single to_tsquery string. Words are
    // AND-ed together; apostrophes are escaped to keep the SQL literal valid.
    private static string BuildTsQueryText(string raw)
    {
        var words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" & ", words.Select(w => w.Replace("'", "''")));
    }
}
