using Jobbly.Domain.Entities;

namespace Jobbly.Application.Jobs;

// Port for the provider/database-specific query operations used by search. The
// table and generated tsvector live in Postgres (Npgsql), so the real
// implementation lives in Infrastructure. Keeping it here as an interface keeps
// the Application layer free of Npgsql types.
public interface IFullTextSearch
{
    IQueryable<Job> ApplyKeyword(IQueryable<Job> jobs, string query);
    IQueryable<Job> ApplyLocation(IQueryable<Job> jobs, string location);
    IQueryable<Job> OrderByRelevance(IQueryable<Job> jobs, string query);
}
