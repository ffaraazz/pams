using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/projects endpoints (FR-003, FR-004, FR-005).
/// </summary>
[Collection("PamsApi")]
public sealed class ProjectsEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public ProjectsEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-003 | POST /api/v1/projects → 201 ──────────────────────────────

    [Fact(DisplayName = "FR-003 | POST projects → 201 Created")]
    public async Task CreateProject_ValidPayload_Returns201()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            projectCode = "INT-PRJ-NEW",
            projectName = "New Integration Project",
            accountCode = SeedData.AccountCode,
            projectManagerEmpCode = SeedData.PmEmpCode,
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.ProjectCode.Should().Be("INT-PRJ-NEW");
        body.Status.Should().Be("Upcoming");
    }

    [Fact(DisplayName = "FR-003 | POST projects duplicate → 409 Conflict")]
    public async Task CreateProject_DuplicateCode_Returns409()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            projectCode = "INT-PRJ-DUP",
            projectName = "First",
            accountCode = SeedData.AccountCode,
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        };
        await client.PostAsJsonAsync("/api/v1/projects", payload);

        // Act — duplicate
        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "FR-003 | POST projects invalid account → 404")]
    public async Task CreateProject_BadAccount_Returns404()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            projectCode = "INT-PRJ-BAD-ACC",
            projectName = "Bad Account",
            accountCode = "GHOST-ACCOUNT",
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── FR-004 | Billable defaults to true for Client accounts ─────────────

    [Fact(DisplayName = "FR-004 | POST project (Client acc, no billable) → billable=true")]
    public async Task CreateProject_ClientAccount_DefaultsBillableTrue()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            projectCode = "INT-PRJ-BILL",
            projectName = "Default Billable",
            accountCode = SeedData.AccountCode,
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/projects", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.Billable.Should().BeTrue();
    }

    // ─── FR-005 | PUT /api/v1/projects/{code} → 200 ────────────────────────

    [Fact(DisplayName = "FR-005 | PUT projects/{code} → 200 OK")]
    public async Task UpdateProject_ValidPayload_Returns200()
    {
        // Arrange — create a project to update
        var client = _factory.CreateClientWithRole("HR");
        await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = "INT-PRJ-UPD",
            projectName = "Before",
            accountCode = SeedData.AccountCode,
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")
        });

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/projects/INT-PRJ-UPD", new
        {
            projectCode = "INT-PRJ-UPD",
            projectName = "After",
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            status = "Active",
            billable = false
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.ProjectName.Should().Be("After");
    }

    // ─── GET /api/v1/projects — list ────────────────────────────────────────

    [Fact(DisplayName = "FR-003 | GET projects → 200 OK")]
    public async Task ListProjects_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync("/api/v1/projects");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/v1/projects/{code} — detail ──────────────────────────────

    [Fact(DisplayName = "FR-003 | GET projects/{code} → 200 OK")]
    public async Task GetProjectByCode_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync($"/api/v1/projects/{SeedData.ProjectCode}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── PM scope — PM can only update own ──────────────────────────────────

    [Fact(DisplayName = "FR-005 | PUT projects as non-owner PM → 403")]
    public async Task UpdateProject_PmNotOwner_Returns403()
    {
        // Arrange — PM with a different employee ID
        var client = _factory.CreateClientWithRole("ProjectManager", "OTHER-PM",
            Guid.Parse("00000000-0000-0000-0000-999999999999"));

        var response = await client.PutAsJsonAsync($"/api/v1/projects/{SeedData.ProjectCode}", new
        {
            projectCode = SeedData.ProjectCode,
            projectName = "Hijack",
            startDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            status = "Active",
            billable = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ProjectResponse
    {
        public Guid ProjectId { get; init; }
        public string ProjectCode { get; init; } = "";
        public string ProjectName { get; init; } = "";
        public string Status { get; init; } = "";
        public bool Billable { get; init; }
        public bool IsActive { get; init; }
    }
}
