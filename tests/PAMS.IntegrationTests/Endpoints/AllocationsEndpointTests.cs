using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/allocations endpoints (FR-010, FR-011, FR-012).
/// </summary>
[Collection("PamsApi")]
public sealed class AllocationsEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public AllocationsEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── Helper ─────────────────────────────────────────────────────────────

    private object AllocationPayload(
        string empCode = "INT-STAFF-001",
        string projectCode = "INT-PRJ-001",
        int percentage = 25,
        string? fromDate = null) => new
        {
            empCode,
            projectCode,
            percentage,
            fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
        };

    // ─── FR-010 | POST /api/v1/allocations → 201 ───────────────────────────

    [Fact(DisplayName = "FR-010 | POST allocations → 201 Created (HR)")]
    public async Task CreateAllocation_HR_Returns201()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var payload = new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 10,
            fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(100)).ToString("yyyy-MM-dd"),
        };

        var response = await client.PostAsJsonAsync("/api/v1/allocations", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "FR-010 | POST allocations → 201 Created (PM)")]
    public async Task CreateAllocation_PM_Returns201()
    {
        var client = _factory.CreateClientWithRole("ProjectManager", SeedData.PmEmpCode, SeedData.PmEmployeeId);
        var payload = new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 10,
            fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(200)).ToString("yyyy-MM-dd"),
        };

        var response = await client.PostAsJsonAsync("/api/v1/allocations", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "FR-010 | POST allocations as Staff → 403")]
    public async Task CreateAllocation_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff", SeedData.StaffEmpCode, SeedData.StaffEmployeeId);
        var payload = AllocationPayload();

        var response = await client.PostAsJsonAsync("/api/v1/allocations", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-011 | GET /api/v1/allocations/{id} → 200 ───────────────────────

    [Fact(DisplayName = "FR-011 | GET allocations/{id} → 200 OK")]
    public async Task GetAllocationById_AfterCreation_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var payload = new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 5,
            fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(300)).ToString("yyyy-MM-dd"),
        };

        var created = await client.PostAsJsonAsync("/api/v1/allocations", payload);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = created.Headers.Location;
        location.Should().NotBeNull();

        var response = await client.GetAsync(location);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-011 | GET allocations/{nonexistent} → 404")]
    public async Task GetAllocationById_NotFound_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync($"/api/v1/allocations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── FR-012 | PUT /api/v1/allocations/{id} → 200 ───────────────────────

    [Fact(DisplayName = "FR-012 | PUT allocations/{id} → 200 OK")]
    public async Task UpdateAllocation_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var from = DateOnly.FromDateTime(DateTime.Today.AddDays(400));
        var to = DateOnly.FromDateTime(DateTime.Today.AddDays(500));
        var createPayload = new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 10,
            fromDate = from.ToString("yyyy-MM-dd"),
            toDate = to.ToString("yyyy-MM-dd"),
        };

        var created = await client.PostAsJsonAsync("/api/v1/allocations", createPayload);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = created.Headers.Location;
        location.Should().NotBeNull();

        // Update percentage
        var updatePayload = new
        {
            fromDate = from.ToString("yyyy-MM-dd"),
            toDate = to.ToString("yyyy-MM-dd"),
            percentage = 15,
        };
        var response = await client.PutAsJsonAsync(location!.ToString(), updatePayload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── FR-012 | PATCH stop → 200 ─────────────────────────────────────────

    [Fact(DisplayName = "FR-012 | PATCH allocations/{id} stop → 200 OK")]
    public async Task StopAllocation_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
        var to = DateOnly.FromDateTime(DateTime.Today.AddDays(600));
        var createPayload = new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 5,
            fromDate = from.ToString("yyyy-MM-dd"),
            toDate = to.ToString("yyyy-MM-dd"),
        };

        var created = await client.PostAsJsonAsync("/api/v1/allocations", createPayload);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = created.Headers.Location;
        location.Should().NotBeNull();

        // Stop
        var stopPayload = new { action = "stop" };
        var response = await client.PatchAsJsonAsync(location!.ToString(), stopPayload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Capacity check ────────────────────────────────────────────────────

    [Fact(DisplayName = "FR-010 | GET capacity-check → 200 OK")]
    public async Task CapacityCheck_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var from = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var response = await client.GetAsync(
            $"/api/v1/allocations/capacity-check?empCode={SeedData.StaffEmpCode}&fromDate={from}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-010 | GET capacity-check unknown emp → 404")]
    public async Task CapacityCheck_UnknownEmployee_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var from = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var response = await client.GetAsync(
            $"/api/v1/allocations/capacity-check?empCode=GHOST-999&fromDate={from}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
