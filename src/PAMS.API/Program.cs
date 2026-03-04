using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PAMS.API.Extensions;
using PAMS.Application.Interfaces;
using PAMS.Infrastructure.Extensions;
using PAMS.Infrastructure.Persistence;
using PAMS.Infrastructure.Persistence.Seed;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, loggerConfig) =>
        loggerConfig.ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.File("logs/pams-.log", rollingInterval: RollingInterval.Day));

    // Infrastructure (DbContext, Repositories, Services)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    builder.Services.AddInfrastructure(connectionString);

    // CurrentUserService registered in Infrastructure, but needs HttpContextAccessor from API
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, PAMS.Infrastructure.Services.CurrentUserService>();

    // MediatR + validation pipeline
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(PAMS.Application.Commands.Allocations.CreateAllocationCommand).Assembly);
        cfg.AddOpenBehavior(typeof(PAMS.Application.Behaviors.ValidationBehavior<,>));
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<PAMS.Application.Validators.CreateAllocationCommandValidator>();

    // API services (Auth, Policies, CORS, Health Checks)
    builder.Services.AddApiServices(builder.Configuration);

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();

    var keycloakAuthority = builder.Configuration["KeycloakSettings:Authority"]!;
    // ExternalAuthority is the browser-reachable Keycloak URL (for Swagger UI token requests).
    // Falls back to Authority when running outside Docker (e.g. localhost dev).
    var swaggerAuthority = builder.Configuration["KeycloakSettings:ExternalAuthority"] ?? keycloakAuthority;
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "PAMS API",
            Version = "v1",
            Description = "Project Allocation Management System.\n\n" +
                "All endpoints require a Keycloak-issued OIDC Bearer token.\n" +
                "Dates use ISO 8601 `YYYY-MM-DD` format. Timestamps use UTC.\n\n" +
                "**Pagination:** List endpoints support `page` (default 1) and `limit` (default 10, max 100).\n" +
                "**Code-based identifiers:** Path parameters use business codes (`accountCode`, `projectCode`, `empCode`).\n" +
                "**Roles:** HR (full access), ProjectManager (scoped to own projects), Staff (own profile)."
        });

        // Include XML comments from controllers
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);

        // Map DateOnly to OpenAPI string+date format with example
        options.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "string",
            Format = "date",
            Example = new Microsoft.OpenApi.Any.OpenApiString(DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"))
        });

        // OAuth2 Resource Owner Password flow — login with username/password in Swagger
        options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
            Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
            {
                Password = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{swaggerAuthority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "OpenID" },
                        { "profile", "Profile" }
                    }
                }
            }
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "oauth2"
                    }
                },
                new[] { "openid", "profile" }
            }
        });
    });

    var app = builder.Build();

    // Auto-migrate + seed
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PamsDbContext>();
        await db.Database.MigrateAsync();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var seedLogger = loggerFactory.CreateLogger("Seeder");

        // Production-safe seeds (always run)
        await SystemConfigSeeder.SeedAsync(db, seedLogger);
        await SkillSeeder.SeedAsync(db, seedLogger);

        // Development-only seeds (test users + demo data)
        if (app.Environment.IsDevelopment())
        {
            await EmployeeSeeder.SeedAsync(db, seedLogger);
            await DemoDataSeeder.SeedAsync(db, seedLogger);
        }
    }

    // Middleware pipeline
    app.UsePamsMiddleware();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PAMS API v1");
            options.RoutePrefix = "swagger";
            options.OAuthClientId("pams-web");
            options.OAuthUsePkce();
        });
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for WebApplicationFactory in integration tests
public partial class Program;
