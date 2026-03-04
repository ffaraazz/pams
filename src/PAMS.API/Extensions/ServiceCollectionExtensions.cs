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

            });

        // Authorization policies — DB-driven via ICurrentUserService.Role
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PAMS.API.Authorization.DbRoleAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy("HROnly", policy => policy.Requirements.Add(
                new PAMS.API.Authorization.DbRoleRequirement(PAMS.Domain.Enums.EmployeeRole.HR)))
            .AddPolicy("CanAllocate", policy => policy.Requirements.Add(
                new PAMS.API.Authorization.DbRoleRequirement(PAMS.Domain.Enums.EmployeeRole.HR, PAMS.Domain.Enums.EmployeeRole.ProjectManager)))
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


}
