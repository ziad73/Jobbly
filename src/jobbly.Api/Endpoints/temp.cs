namespace jobbly.Api.Endpoints;

public static class TempEndpoints
{
    public static void MapTempEndpoints(this IEndpointRouteBuilder routes)
    {
      var tempGroup=routes.MapGroup("/api/temp").WithTags("temp");
      
      tempGroup.MapGet("/",(ILogger<Program> logger)=>{
         logger.LogInformation("log: Hello, world");
         return Results.Ok("Hello, world");}         
      ).WithName("Hello");
    }
}
