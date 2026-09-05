using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Jobbly.Api.OpenApi;

/// <summary>
/// Declares the bearer HTTP security scheme in the OpenAPI document so clients
/// (Scalar etc.) can present an access token. Endpoints opt in per-request once
/// authorization lands; the scheme itself is inert otherwise.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["bearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the access token returned by /api/auth/login or /api/auth/register."
        };

        return Task.CompletedTask;
    }
}