using Scalar.AspNetCore;
using Jobbly.Application;
using Jobbly.Infrastructure;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Register Services into DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// Enables automatic validation for all Minimal API endpoints
builder.Services.AddValidation();

// Serilog configuration
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration));

var app = builder.Build();

// Apply migrations on startup so the app runs with a single command,
// including inside containers where no separate migration step exists.
// NOTE: fine for local dev and demos - revisit before real production use.
// await app.Services.InitializeDatabaseAsync();

// Add status code pages (so even plain 404s / 500s return a body), With this middleware, you’ll get an actual JSON payload for non-successful status codes.
app.UseStatusCodePages();


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

// app.UseAuthentication();
// app.UseAuthorization(); // validates access permissions for the current authenticated user.


app.Run();
