using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Accounts;

public sealed record AccountSummaryResponse
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    /// <summary>Whether the account is active. Inactive accounts cannot have new projects created under them.</summary>
    public bool IsActive { get; init; }
    public int TotalActiveProjects { get; init; }
    public int TotalInactiveProjects { get; init; }
    public int TotalActiveEmployees { get; init; }
    public int TotalInactiveEmployees { get; init; }
}

public sealed record AccountDetailResponse
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    /// <summary>Whether the account is active. Inactive accounts cannot have new projects created under them.</summary>
    public bool IsActive { get; init; }
    public int TotalActiveProjects { get; init; }
    public int TotalInactiveProjects { get; init; }
    public int TotalActiveEmployees { get; init; }
    public int TotalInactiveEmployees { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
