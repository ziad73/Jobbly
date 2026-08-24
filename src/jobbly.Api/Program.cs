using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using jobbly.Application;
using jobbly.Infrastructure;
using jobbly.Api.Endpoints;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Register options pattern configuration sections, then inject it once you need it
// builder.Services.AddOptions<WeatherOptions>()
//     .BindConfiguration(WeatherOptions.SectionName)// bind, type safty
//     .ValidateDataAnnotations()
//     .ValidateOnStart()// apply validation on startup
//     .PostConfigure( options =>
//     {
//         if (String.IsNullOrWhiteSpace(options.Summary))
//         {
//             options.Summary = "No summary provided";
//         }
//     });

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

// Apply migrations and seed on startup so the app runs with a single command,
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
// app.UseSerilogRequestLogging();

// app.UseRouting();
// app.UseCors();
// app.UseCors("Frontend");// Apply CORS policy globally on all endpoints

// app.UseAuthentication();
// app.UseAuthorization(); // validates access permissions for the current authenticated user.


// Custom middleware
// app.MapControllers();

// Minimal APIs
// app.MapMovieEndpoints();
// app.MapWeatherEndpoints();
app.MapTempEndpoints();

// app.MapGet("/",()=> "Hello, world").WithName("Hello");

app.Run();
