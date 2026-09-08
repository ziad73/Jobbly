using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Jobbly.Application.Common;
using Jobbly.Domain.Entities;
using Jobbly.Domain.Enums;
using Jobbly.Infrastructure.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jobbly.Tests;

public sealed class HealthMonitorTests
{
    // Minimal async-capable DbSet fake (the classic EF testing pattern): no
    // provider, no model building, just LINQ over a list.
    private sealed class FakeDbSet<T>(IEnumerable<T> data) : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>
        where T : class
    {
        private readonly List<T> _data = data.ToList();

        public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        Type IQueryable.ElementType => typeof(T);
        Expression IQueryable.Expression => _data.AsQueryable().Expression;
        IQueryProvider IQueryable.Provider => new FakeQueryProvider(_data.AsQueryable().Provider);

        public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new FakeEnumerator<T>(_data.GetEnumerator());

        public override IEntityType EntityType => null!;
    }

    private sealed class FakeQueryProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => inner.CreateQuery(expression);

        // Wrap in FakeQueryable so further Queryable.* chaining (Where, OrderBy
        // - and EF's own AsNoTracking wrappers) keeps the async facet instead
        // of degrading to plain EnumerableQuery.
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new FakeQueryable<TElement>(inner.CreateQuery<TElement>(expression));

        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        // EF calls this with TResult = Task<T>: run synchronously, rewrap.
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var syncResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
                .MakeGenericMethod(resultType)
                .Invoke(inner, [expression]);
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [syncResult])!;
        }
    }

    private sealed class FakeQueryable<T>(IQueryable<T> inner) : IQueryable<T>, IAsyncEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        Type IQueryable.ElementType => typeof(T);
        Expression IQueryable.Expression => inner.Expression;
        IQueryProvider IQueryable.Provider => new FakeQueryProvider(inner.Provider);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new FakeEnumerator<T>(inner.GetEnumerator());
    }

    private sealed class FakeEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public T Current => inner.Current;
    }

    private sealed class FakeDb(FakeDbSet<Provider> providers) : IJobblyDbContext
    {
        public DbSet<Provider> Providers => providers;
        public DbSet<Company> Companies => new FakeDbSet<Company>([]);
        public DbSet<Job> Jobs => new FakeDbSet<Job>([]);
        public DbSet<CanonicalJob> CanonicalJobs => new FakeDbSet<CanonicalJob>([]);
        public DbSet<PipelineRun> PipelineRuns => new FakeDbSet<PipelineRun>([]);
        public DbSet<UserProfile> UserProfiles => new FakeDbSet<UserProfile>([]);
        public DbSet<UserSkill> UserSkills => new FakeDbSet<UserSkill>([]);
        public DbSet<RefreshToken> RefreshTokens => new FakeDbSet<RefreshToken>([]);
        public DbSet<SavedJob> SavedJobs => new FakeDbSet<SavedJob>([]);
        public DbSet<SavedSearch> SavedSearches => new FakeDbSet<SavedSearch>([]);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class CapturingLogger : ILogger<PipelineHealthMonitor>
    {
        public List<string> Messages { get; } = [];

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add($"{logLevel}: {formatter(state, exception)}");

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private static Provider MakeProvider(string slug, int intervalMinutes = 180)
        => Provider.Create($"{slug}-name", slug, IntegrationType.PublicApi, "https://example.com/", intervalMinutes);

    [Fact]
    public async Task WarnsOnFailingAndStaleProviders()
    {
        var now = DateTime.UtcNow;

        var failing = MakeProvider("failing");
        failing.MarkFailed("boom", now);
        var stale = MakeProvider("stale");
        stale.MarkSynced(now.AddHours(-10));
        var healthy = MakeProvider("healthy");
        healthy.MarkSynced(now);

        var db = new FakeDb(new FakeDbSet<Provider>([failing, stale, healthy]));
        var logger = new CapturingLogger();

        await new PipelineHealthMonitor(db, logger).CheckAsync();

        Assert.Contains(logger.Messages, m => m.StartsWith("Warning") && m.Contains("failing"));
        Assert.Contains(logger.Messages, m => m.StartsWith("Warning") && m.Contains("stale"));
        Assert.DoesNotContain(logger.Messages, m => m.StartsWith("Warning") && m.Contains("healthy"));
        Assert.Contains(logger.Messages, m => m.StartsWith("Information") && m.Contains("3 provider(s)"));
    }

    [Fact]
    public async Task WarnsWhenNoProvidersRegistered()
    {
        var db = new FakeDb(new FakeDbSet<Provider>([]));
        var logger = new CapturingLogger();

        await new PipelineHealthMonitor(db, logger).CheckAsync();

        Assert.Contains(logger.Messages, m => m.Contains("no active providers"));
    }
}