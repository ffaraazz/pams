using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PAMS.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Authentication — Keycloak OIDC (PAMS is resource server only)
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["KeycloakSettings:Authority"];
                options.Audience = configuration["KeycloakSettings:Audience"];
                options.RequireHttpsMetadata = bool.Parse(
                    configuration["KeycloakSettings:RequireHttpsMetadata"] ?? "true");

                // When running in Docker, Authority is the internal URL (http://keycloak:8080/...)
                // but the browser obtains tokens from the external URL (http://localhost:8080/...).
                // Accept both issuers so tokens work in all environments.
                var externalAuthority = configuration["KeycloakSettings:ExternalAuthority"];
                if (!string.IsNullOrEmpty(externalAuthority))
                {
                    options.TokenValidationParameters.ValidIssuers =
                    [
                        configuration["KeycloakSettings:Authority"]!,
                        externalAuthority
                    ];
                }

                // Map Keycloak realm_access.roles to ClaimTypes.Role
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        MapKeycloakRolesToClaims(context);
                        return Task.CompletedTask;
                    }
                };
            });

        // Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("HROnly", policy => policy.RequireRole("HR"))
            .AddPolicy("CanAllocate", policy => policy.RequireRole("HR", "ProjectManager"))
            .AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Health checks
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!);

        // HttpContextAccessor for CurrentUserService
        services.AddHttpContextAccessor();

        return services;
    }

    private static void MapKeycloakRolesToClaims(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity) return;

        // Extract roles from realm_access.roles
        var realmAccess = context.Principal.FindFirst("realm_access");
        if (realmAccess is null) return;

        try
        {
            using var doc = JsonDocument.Parse(realmAccess.Value);
            if (doc.RootElement.TryGetProperty("roles", out var roles) &&
                roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrWhiteSpace(roleName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Malformed realm_access claim — skip role mapping
        }
    }
}
