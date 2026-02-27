using System.Net;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/dashboard endpoints (FR-018, FR-019).
/// </summary>
[Collection("PamsApi")]
public sealed class DashboardEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public DashboardEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-018 | GET /api/v1/dashboard/project-view ────────────────────────

    [Fact(DisplayName = "FR-018 | GET dashboard/project-view → 200 (HR)")]
    public async Task ProjectView_HR_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/project-view");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-018 | GET dashboard/project-view → 200 (PM)")]
    public async Task ProjectView_PM_Returns200()
    {
        var client = _factory.CreateClientWithRole("ProjectManager", SeedData.PmEmpCode, SeedData.PmEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/project-view");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-018 | GET dashboard/project-view → 403 (Staff)")]
    public async Task ProjectView_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff", SeedData.StaffEmpCode, SeedData.StaffEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/project-view");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-019 | GET /api/v1/dashboard/employee-view ───────────────────────

    [Fact(DisplayName = "FR-019 | GET dashboard/employee-view → 200 (HR)")]
    public async Task EmployeeView_HR_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/employee-view");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-019 | GET dashboard/employee-view → 200 (PM)")]
    public async Task EmployeeView_PM_Returns200()
    {
        var client = _factory.CreateClientWithRole("ProjectManager", SeedData.PmEmpCode, SeedData.PmEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/employee-view");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-019 | GET dashboard/employee-view → 403 (Staff)")]
    public async Task EmployeeView_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff", SeedData.StaffEmpCode, SeedData.StaffEmployeeId);
        var response = await client.GetAsync("/api/v1/dashboard/employee-view");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
