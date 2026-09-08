using Hangfire;
using Hangfire.PostgreSql;
using Scalar.AspNetCore;
using Jobbly.Api.Endpoints;
using Jobbly.Api.Middleware;
using Jobbly.Application;
using Jobbly.Infrastructure;
using Jobbly.Infrastructure.BackgroundJobs;
using Jobbly.Api.Authentication;
using Jobbly.Api.Health;
using Jobbly.Api.OpenApi;
using Jobbly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Register Services into DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register JWT bearer authentication and authorization policies into DI container
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthOperationTransformer>();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Enables automatic validation for all Minimal API endpoints
builder.Services.AddValidation();

// Health: /health is liveness (process up), /health/ready gates on Postgres
// and Hangfire storage. No extra packages - checks use the EF DbContext.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<HangfireStorageHealthCheck>("hangfire");

// Serilog configuration
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration));

// Hangfire background job server with Postgres storage (same jobblydb database).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("jobblydb"))));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Apply migrations on startup so the app runs with a single command,
// including inside containers where no separate migration step exists.
// NOTE: fine for local dev and demos - revisit before real production use.
await app.Services.InitializeDatabaseAsync();

// Register recurring ingestion jobs after migrations so Provider rows exist.
HangfireIngestionScheduler.RegisterRecurringJobs(app.Services);

// Add status code pages (so even plain 404s / 500s return a body), With this middleware, you’ll get an actual JSON payload for non-successful status codes.
app.UseStatusCodePages();

// Convert unhandled exceptions into RFC 9457 ProblemDetails
app.UseExceptionHandler();


// ideal order of middleware

// Exception handling middleware
if (app.Environment.IsDevelopment())
{
    // OpenAPI spec file
    app.MapOpenApi("/openapi/v1.yaml");// backend endpoint generator. It compiles your C# endpoints/models into a raw OpenAPI specification file

    // scalar UI faster and more lightweight than Swagger UI
    // frontend interactive UI middleware, renders /openapi/v1.yaml file
    app.MapScalarApiReference(options =>
        // Customizing Scalar UI
        options.WithOpenApiRoutePattern("/openapi/v1.yaml")
        // .WithTheme(ScalarTheme.Kepler)
        );
}

// app.UseHsts();
// app.UseHttpsRedirection(); // we have to get a web server
// app.UseStaticFiles();

// logging
app.UseSerilogRequestLogging();

// app.UseRouting();
// app.UseCors();
// app.UseCors("Frontend");// Apply CORS policy globally on all endpoints

// Authentication: validates a presented bearer token.
app.UseAuthentication();

// Authorization: enforces RequireAuthorization on top of the authenticated identity.
app.UseAuthorization();

// Hangfire dashboard — dev only, Admin role only. Declared after the auth
// middleware so the bearer token populates the request identity first.
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        // restricts access to the Hangfire dashboard to users, admin only
        Authorization = [new HangfireDashboardAuthorizationFilter()]
    });
}

// Health probes (public, unauthenticated - load balancers need them).
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// Hello, world
app.MapGet("temp", () => "Hello, world devvvv");

// Pipeline endpoints
app.MapPipelineEndpoints();

// Job discovery endpoints
app.MapJobEndpoints();

// Auth endpoints (register / login / logout)
app.MapAuthEndpoints();

// Own-profile endpoints (GET/PUT /api/users/me)
app.MapUserEndpoints();

// Saved jobs + application tracker
app.MapSavedJobEndpoints();

// Saved searches + dashboard feed -logged-in user's personal home page with suggested jobs-
app.MapSavedSearchEndpoints();

app.Run();
