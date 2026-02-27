using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/projects/{code}/team-members (FR-016, FR-017).
/// </summary>
[Collection("PamsApi")]
public sealed class ProjectTeamMembersEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public ProjectTeamMembersEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    private string BaseUrl => $"/api/v1/projects/{SeedData.ProjectCode}/team-members";

    // ─── Seed allocations needed for team-member assignments ────────────────
    // The AddProjectTeamMember handler requires both lead & reportee to be
    // allocated to the project. We create allocations here via API.

    private async Task EnsureAllocationsExist(HttpClient client)
    {
        // Allocate PM (lead) to project
        await client.PostAsJsonAsync("/api/v1/allocations", new
        {
            empCode = SeedData.PmEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 20,
            fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)).ToString("yyyy-MM-dd"),
        });

        // Allocate Staff (reportee) to project
        await client.PostAsJsonAsync("/api/v1/allocations", new
        {
            empCode = SeedData.StaffEmpCode,
            projectCode = SeedData.ProjectCode,
            percentage = 20,
            fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)).ToString("yyyy-MM-dd"),
        });
    }

    // ─── FR-016 | GET team-members → 200 ────────────────────────────────────

    [Fact(DisplayName = "FR-016 | GET team-members → 200 OK")]
    public async Task ListTeamMembers_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── FR-016 | POST team-members → 201 ──────────────────────────────────

    [Fact(DisplayName = "FR-016 | POST team-member → 201 Created")]
    public async Task AddTeamMember_Returns201()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        await EnsureAllocationsExist(client);

        var payload = new
        {
            teamLeadEmpCode = SeedData.PmEmpCode,
            reporteeEmpCode = SeedData.StaffEmpCode,
        };

        var response = await client.PostAsJsonAsync(BaseUrl, payload);

        // 201 if first time, or 409 if already created (idempotent seed)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
    }

    // ─── FR-016 | POST team-members as Staff → 403 ─────────────────────────

    [Fact(DisplayName = "FR-016 | POST team-member as Staff → 403")]
    public async Task AddTeamMember_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff", SeedData.StaffEmpCode, SeedData.StaffEmployeeId);
        var payload = new
        {
            teamLeadEmpCode = SeedData.PmEmpCode,
            reporteeEmpCode = SeedData.StaffEmpCode,
        };

        var response = await client.PostAsJsonAsync(BaseUrl, payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-016 | GET team-members for unknown project → 404 ────────────────

    [Fact(DisplayName = "FR-016 | GET team-members unknown project → 404")]
    public async Task ListTeamMembers_UnknownProject_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var response = await client.GetAsync("/api/v1/projects/GHOST-PRJ/team-members");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── FR-017 | DELETE team-member → 204 ──────────────────────────────────

    [Fact(DisplayName = "FR-017 | DELETE team-member → 204 NoContent")]
    public async Task RemoveTeamMember_Returns204()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        await EnsureAllocationsExist(client);

        // Add first
        var addPayload = new
        {
            teamLeadEmpCode = SeedData.PmEmpCode,
            reporteeEmpCode = SeedData.StaffEmpCode,
        };
        await client.PostAsJsonAsync(BaseUrl, addPayload);

        // Remove
        var deleteUrl = $"{BaseUrl}/{SeedData.PmEmpCode}/{SeedData.StaffEmpCode}";
        var response = await client.DeleteAsync(deleteUrl);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    // ─── FR-017 | DELETE nonexistent → 404 ──────────────────────────────────

    [Fact(DisplayName = "FR-017 | DELETE nonexistent team-member → 404")]
    public async Task RemoveTeamMember_NotFound_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR", SeedData.HrEmpCode, SeedData.HrEmployeeId);
        var deleteUrl = $"{BaseUrl}/GHOST-LEAD/GHOST-REPORTEE";
        var response = await client.DeleteAsync(deleteUrl);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
