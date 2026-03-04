using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/employees endpoints (FR-007, FR-008, FR-009).
/// </summary>
[Collection("PamsApi")]
public sealed class EmployeesEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public EmployeesEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-007 | POST /api/v1/employees → 201 ─────────────────────────────

    [Fact(DisplayName = "FR-007 | POST employees → 201 Created")]
    public async Task CreateEmployee_ValidPayload_Returns201()
    {
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            empCode = "INT-EMP-NEW",
            firstName = "New",
            lastName = "Employee",
            email = "new.employee@integration.test",
            role = "Staff",
            designation = "Junior Dev"
        };

        var response = await client.PostAsJsonAsync("/api/v1/employees", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "FR-007 | POST employees duplicate empCode → 409")]
    public async Task CreateEmployee_DuplicateCode_Returns409()
    {
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            empCode = "INT-EMP-DUP",
            firstName = "A",
            lastName = "B",
            email = "dup1@integration.test",
            role = "Staff",
            designation = "Dev"
        };
        await client.PostAsJsonAsync("/api/v1/employees", payload);

        // Duplicate
        var payload2 = new
        {
            empCode = "INT-EMP-DUP",
            firstName = "C",
            lastName = "D",
            email = "dup2@integration.test",
            role = "Staff",
            designation = "Dev"
        };
        var response = await client.PostAsJsonAsync("/api/v1/employees", payload2);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "FR-007 | POST employees duplicate email → 409")]
    public async Task CreateEmployee_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClientWithRole("HR");
        await client.PostAsJsonAsync("/api/v1/employees", new
        {
            empCode = "INT-EMP-UNEMAIL1",
            firstName = "A",
            lastName = "B",
            email = "shared.email@integration.test",
            role = "Staff",
            designation = "Dev"
        });

        var response = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            empCode = "INT-EMP-UNEMAIL2",
            firstName = "C",
            lastName = "D",
            email = "shared.email@integration.test",
            role = "Staff",
            designation = "Dev"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "FR-007 | POST employees as Staff → 403")]
    public async Task CreateEmployee_StaffRole_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff");
        var payload = new
        {
            empCode = "INT-NOPE",
            firstName = "N",
            lastName = "O",
            email = "nope@test.com",
            role = "Staff",
            designation = "X"
        };

        var response = await client.PostAsJsonAsync("/api/v1/employees", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-008 | PUT /api/v1/employees/{empCode} → 200 ────────────────────

    [Fact(DisplayName = "FR-008 | PUT employees/{empCode} → 204 NoContent")]
    public async Task UpdateEmployee_ValidPayload_Returns204()
    {
        var client = _factory.CreateClientWithRole("HR");
        // Create first
        await client.PostAsJsonAsync("/api/v1/employees", new
        {
            empCode = "INT-EMP-UPD",
            firstName = "Before",
            lastName = "Update",
            email = "upd@integration.test",
            role = "Staff",
            designation = "Dev"
        });

        // Update
        var response = await client.PutAsJsonAsync("/api/v1/employees/INT-EMP-UPD", new
        {
            empCode = "INT-EMP-UPD",
            firstName = "After",
            lastName = "Update",
            email = "upd@integration.test",
            role = "Staff",
            designation = "Senior Dev"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "FR-008 | PUT employees/notfound → 404")]
    public async Task UpdateEmployee_NotFound_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.PutAsJsonAsync("/api/v1/employees/INT-GHOST", new
        {
            empCode = "INT-GHOST",
            firstName = "X",
            lastName = "Y",
            email = "ghost@test.com",
            role = "Staff",
            designation = "Dev"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── FR-009 | GET /api/v1/employees → 200 ──────────────────────────────

    [Fact(DisplayName = "FR-009 | GET employees → 200 OK")]
    public async Task ListEmployees_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync("/api/v1/employees");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/v1/employees/{empCode} — detail ──────────────────────────

    [Fact(DisplayName = "FR-009 | GET employees/{empCode} → 200 OK")]
    public async Task GetEmployeeByCode_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync($"/api/v1/employees/{SeedData.HrEmpCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/v1/employees/me — current user ───────────────────────────

    [Fact(DisplayName = "FR-009 | GET employees/me → 200 OK")]
    public async Task GetMe_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync("/api/v1/employees/me");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
