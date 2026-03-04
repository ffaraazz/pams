using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PAMS.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PAMS.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that spins up a Testcontainers PostgreSQL
/// instance and replaces the real DbContext registration.
/// Also bypasses Keycloak authentication with a test auth handler.
/// </summary>
public sealed class PamsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("pams_test")
        .WithUsername("pams_test")
        .WithPassword("pams_test")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PamsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Remove any existing DbContext registrations (pooled or not)
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(PamsDbContext)
                         || d.ServiceType == typeof(DbContextOptions<PamsDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var d in dbContextDescriptors)
                services.Remove(d);

            // Register test DbContext with the Testcontainers connection string
            services.AddDbContext<PamsDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString(), npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pams");
                }));

            // Replace Authentication with test scheme
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", _ => { });
        });
    }

    /// <summary>
    /// Creates an HttpClient with a specific role claim for test auth.
    /// Defaults empCode and employeeId based on the role using SeedData constants.
    /// </summary>
    public HttpClient CreateClientWithRole(string role, string? empCode = null, Guid? employeeId = null)
    {
        empCode ??= role switch
        {
            "Staff" => Helpers.SeedData.StaffEmpCode,
            "ProjectManager" => Helpers.SeedData.PmEmpCode,
            _ => Helpers.SeedData.HrEmpCode,
        };
        employeeId ??= role switch
        {
            "Staff" => Helpers.SeedData.StaffEmployeeId,
            "ProjectManager" => Helpers.SeedData.PmEmployeeId,
            _ => Helpers.SeedData.HrEmployeeId,
        };
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-EmpCode", empCode);
        client.DefaultRequestHeaders.Add("X-Test-EmployeeId", employeeId.Value.ToString());
        return client;
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await base.DisposeAsync();
    }
}
