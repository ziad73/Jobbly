using System.Text;
using Jobbly.Infrastructure.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Jobbly.Api.Authentication;

public static class AddApiAuthenticationExtensions
{
    /// <summary>
    /// registers JWT bearer authentication 
    /// Adds JWT bearer authentication. Tokens are validated against JwtOptions:
    /// issuer, audience, lifetime and HMAC signature. Claims are NOT remapped
    /// (MapInboundClaims = false) so "sub"/"email"/"name" keep their JWT names.
    /// Role claims keep the JWT name "role" via RoleClaimType, which is what
    /// RequireRole/IsInRole consult for policy checks.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Missing configuration section '{JwtOptions.SectionName}'.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = "role"
                };
            });

        return services;
    }
}
