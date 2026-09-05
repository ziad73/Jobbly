using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Jobbly.Api.OpenApi;

/// <summary>
/// Marks operations that carry IAuthorizeData metadata (anything added via
/// RequireAuthorization) as secured by the "bearerAuth" scheme declared in
/// BearerSecuritySchemeTransformer, so the Scalar UI asks for a token there.
/// </summary>
public sealed class AuthOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (requiresAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearerAuth", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    }
}