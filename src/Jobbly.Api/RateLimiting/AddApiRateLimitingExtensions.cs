using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobbly.Api.RateLimiting;

public static class AddApiRateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string PipelinePolicy = "pipeline";

    /// <summary>
    /// Fixed-window limiters, partitioned by client IP. A loose global limiter
    /// covers every endpoint; the auth group and the expensive pipeline trigger
    /// get strict per-endpoint policies instead (an endpoint with its own policy
    /// is exempt from the global limiter). Rejections are 429 with a small JSON
    /// body. No extra packages - built on System.Threading.RateLimiting.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // The framework default rejection is 503; be explicit as well as
            // handling OnRejected so the status survives future refactors.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"message":"Too many requests. Try again later."}""",
                    cancellationToken);
            };

            // Backstop for floods and runaway loops. 100/min/IP is generous for
            // legitimate use (including health probes) while bounding abuse.
            // QueueLimit = 0 everywhere: reject immediately rather than queueing
            // and adding tail latency under load.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Login/register are the brute-force surface: 10 tries/min/IP is
            // plenty for humans and useless for password spraying.
            options.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            // One trigger fans out to hundreds of provider calls plus DB writes,
            // so manual runs are throttled even for admins.
            options.AddPolicy(PipelinePolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        });

        return services;
    }

    // Client IP lowercased for case-insensitive IPv6 comparison. Requests without
    // a remote address (unix sockets, some proxies) share the "unknown" bucket
    // rather than bypassing limits entirely.
    private static string PartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() is { } ip && !string.IsNullOrWhiteSpace(ip)
            ? ip.ToLower(CultureInfo.InvariantCulture)
            : "unknown";
}