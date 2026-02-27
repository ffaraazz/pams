using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PAMS.IntegrationTests.Helpers;
using PAMS.IntegrationTests.Infrastructure;

namespace PAMS.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /api/v1/accounts endpoints (FR-001, FR-002).
/// Uses Testcontainers PostgreSQL + test auth handler.
/// </summary>
[Collection("PamsApi")]
public sealed class AccountsEndpointTests : IAsyncLifetime
{
    private readonly PamsApiFactory _factory;

    public AccountsEndpointTests(PamsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await SeedData.SeedAsync(_factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;

    // ─── FR-001 | POST /api/v1/accounts — create account ────────────────────

    [Fact(DisplayName = "FR-001 | POST accounts → 201 Created")]
    public async Task CreateAccount_ValidPayload_Returns201()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            accountCode = "INT-ACC-CREATE",
            accountName = "Created Account",
            accountType = "Client"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body!.AccountCode.Should().Be("INT-ACC-CREATE");
        body.AccountName.Should().Be("Created Account");
        body.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "FR-001 | POST accounts duplicate → 409 Conflict")]
    public async Task CreateAccount_DuplicateCode_Returns409()
    {
        // Arrange — create first, then duplicate
        var client = _factory.CreateClientWithRole("HR");
        var payload = new
        {
            accountCode = "INT-ACC-DUP",
            accountName = "First",
            accountType = "Internal"
        };

        await client.PostAsJsonAsync("/api/v1/accounts", payload);

        // Act — duplicate
        var response = await client.PostAsJsonAsync("/api/v1/accounts", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "FR-001 | POST accounts as Staff → 403 Forbidden")]
    public async Task CreateAccount_StaffRole_Returns403()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("Staff");
        var payload = new
        {
            accountCode = "INT-ACC-NOPE",
            accountName = "Nope",
            accountType = "Client"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── FR-002 | PUT /api/v1/accounts/{code} — update account ──────────────

    [Fact(DisplayName = "FR-002 | PUT accounts/{code} → 200 OK")]
    public async Task UpdateAccount_ValidPayload_Returns200()
    {
        // Arrange — create a fresh account to update
        var client = _factory.CreateClientWithRole("HR");
        await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            accountCode = "INT-ACC-UPD",
            accountName = "Before Update",
            accountType = "Client"
        });

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/accounts/INT-ACC-UPD", new
        {
            accountCode = "INT-ACC-UPD",
            accountName = "After Update",
            accountType = "Internal"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body!.AccountName.Should().Be("After Update");
    }

    [Fact(DisplayName = "FR-002 | PUT accounts/notfound → 404 NotFound")]
    public async Task UpdateAccount_NotFound_Returns404()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/accounts/NONEXISTENT", new
        {
            accountCode = "NONEXISTENT",
            accountName = "X",
            accountType = "Client"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── GET /api/v1/accounts — list ────────────────────────────────────────

    [Fact(DisplayName = "FR-001 | GET accounts → 200 OK with list")]
    public async Task ListAccounts_Returns200()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");

        // Act
        var response = await client.GetAsync("/api/v1/accounts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/v1/accounts/{code} — detail ──────────────────────────────

    [Fact(DisplayName = "FR-001 | GET accounts/{code} → 200 OK")]
    public async Task GetAccountByCode_Seeded_Returns200()
    {
        // Arrange
        var client = _factory.CreateClientWithRole("HR");

        // Act
        var response = await client.GetAsync($"/api/v1/accounts/{SeedData.AccountCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccountResponse>();
        body!.AccountCode.Should().Be(SeedData.AccountCode);
    }

    // ─── DTO for deserialization ───────────────────────────────────────────

    private sealed record AccountResponse
    {
        public Guid AccountId { get; init; }
        public string AccountCode { get; init; } = "";
        public string AccountName { get; init; } = "";
        public string AccountType { get; init; } = "";
        public bool IsActive { get; init; }
    }
}
