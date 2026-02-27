using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/skills endpoints (FR-013, FR-014).
/// </summary>
[Collection("PamsApi")]
public sealed class SkillsEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public SkillsEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-013 | GET /api/v1/skills → 200 ─────────────────────────────────

    [Fact(DisplayName = "FR-013 | GET skills → 200 OK (any auth)")]
    public async Task ListSkills_AnyRole_Returns200()
    {
        var client = _factory.CreateClientWithRole("Staff");
        var response = await client.GetAsync("/api/v1/skills");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-013 | GET skills?isActive=true → 200 with filter")]
    public async Task ListSkills_ActiveFilter_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync("/api/v1/skills?isActive=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── FR-014 | POST /api/v1/skills → 201 ────────────────────────────────

    [Fact(DisplayName = "FR-014 | POST skills → 201 Created (HR)")]
    public async Task CreateSkill_HR_Returns201()
    {
        var client = _factory.CreateClientWithRole("HR");
        var payload = new { skillName = "Integration Skill " + Guid.NewGuid().ToString()[..6] };

        var response = await client.PostAsJsonAsync("/api/v1/skills", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact(DisplayName = "FR-014 | POST skills duplicate name → 409")]
    public async Task CreateSkill_DuplicateName_Returns409()
    {
        var client = _factory.CreateClientWithRole("HR");
        var name = "DupSkill-" + Guid.NewGuid().ToString()[..6];
        await client.PostAsJsonAsync("/api/v1/skills", new { skillName = name });

        var response = await client.PostAsJsonAsync("/api/v1/skills", new { skillName = name });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "FR-014 | POST skills as Staff → 403")]
    public async Task CreateSkill_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff");
        var payload = new { skillName = "Forbidden Skill" };

        var response = await client.PostAsJsonAsync("/api/v1/skills", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-014 | PUT /api/v1/skills/{id} → 200 ────────────────────────────

    [Fact(DisplayName = "FR-014 | PUT skills/{id} → 200 OK")]
    public async Task UpdateSkill_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.PutAsJsonAsync(
            $"/api/v1/skills/{SeedData.SkillCSharpId}",
            new { skillName = "C# Updated " + Guid.NewGuid().ToString()[..4] });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-014 | PUT skills/{nonexistent} → 404")]
    public async Task UpdateSkill_NotFound_Returns404()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.PutAsJsonAsync(
            $"/api/v1/skills/{Guid.NewGuid()}",
            new { skillName = "Ghost" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "FR-014 | PUT skills deactivate → 200")]
    public async Task UpdateSkill_Deactivate_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.PutAsJsonAsync(
            $"/api/v1/skills/{SeedData.SkillSqlId}",
            new { isActive = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
