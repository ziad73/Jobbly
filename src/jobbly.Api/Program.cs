using Microsoft.EntityFrameworkCore;
using jobbly.Application;
using jobbly.Infrastructure;
using jobbly.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Turn a broken domain rule into a 400 response instead of a 500.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations and seed on startup so the app runs with a single command,
// including inside containers where no separate migration step exists.
// NOTE: fine for local dev and demos - revisit before real production use.
// await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// app.MapMovieEndpoints();

app.Run();
