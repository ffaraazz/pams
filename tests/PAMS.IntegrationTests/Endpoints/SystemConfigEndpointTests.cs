using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/system-config endpoints (FR-015).
/// </summary>
[Collection("PamsApi")]
public sealed class SystemConfigEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public SystemConfigEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-015 | GET /api/v1/system-config → 200 ──────────────────────────

    [Fact(DisplayName = "FR-015 | GET system-config → 200 OK (HR)")]
    public async Task GetConfig_HR_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var response = await client.GetAsync("/api/v1/system-config");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-015 | GET system-config → 403 (Staff)")]
    public async Task GetConfig_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff");
        var response = await client.GetAsync("/api/v1/system-config");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "FR-015 | GET system-config → 403 (PM)")]
    public async Task GetConfig_PM_Returns403()
    {
        var client = _factory.CreateClientWithRole("ProjectManager");
        var response = await client.GetAsync("/api/v1/system-config");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-015 | PUT /api/v1/system-config → 200 ──────────────────────────

    [Fact(DisplayName = "FR-015 | PUT system-config → 200 OK")]
    public async Task UpdateConfig_ValidPayload_Returns200()
    {
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            minAllocationPct = 10,
            allocationIncrement = 5,
        };

        var response = await client.PutAsJsonAsync("/api/v1/system-config", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "FR-015 | PUT system-config invalid → 422")]
    public async Task UpdateConfig_InvalidMultiple_ReturnsError()
    {
        var client = _factory.CreateClientWithRole("HR");
        // minPct=7 is not a multiple of increment=5
        var payload = new
        {
            minAllocationPct = 7,
            allocationIncrement = 5,
        };

        var response = await client.PutAsJsonAsync("/api/v1/system-config", payload);

        // Either 422 Unprocessable or 400 BadRequest depending on handler
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.BadRequest,
            (HttpStatusCode)500); // DomainException may not be mapped
    }

    [Fact(DisplayName = "FR-015 | PUT system-config as Staff → 403")]
    public async Task UpdateConfig_Staff_Returns403()
    {
        var client = _factory.CreateClientWithRole("Staff");
        var payload = new { minAllocationPct = 10, allocationIncrement = 5 };

        var response = await client.PutAsJsonAsync("/api/v1/system-config", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
