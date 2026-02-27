using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Dashboard;

// ── Project View ──────────────────────────────────────────────────────────────

public sealed record ProjectViewResponse
{
    public List<ProjectViewAccountGroup> Accounts { get; init; } = [];
    public ProjectViewFilters FiltersApplied { get; init; } = new();
}

public sealed record ProjectViewAccountGroup
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public AccountType AccountType { get; init; }
    public List<ProjectViewItem> Projects { get; init; } = [];
}

public sealed record ProjectViewItem
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public bool Billable { get; init; }
    public string? ProjectManagerName { get; init; }
    public bool IsCurrentUserPM { get; init; }
    public List<Allocations.AllocationDetailResponse> Allocations { get; init; } = [];
}

public sealed record ProjectViewFilters
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool IncludeEnded { get; init; }
}

// ── Employee View ─────────────────────────────────────────────────────────────

public sealed record EmployeeViewResponse
{
    public List<EmployeeViewItem> Data { get; init; } = [];
    public Common.PaginationMeta Pagination { get; init; } = new();
}

public sealed record EmployeeViewItem
{
    public Guid EmployeeId { get; init; }
    public string EmpCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public EmployeeRole Role { get; init; }
    public int AvailabilityPercentage { get; init; }
    public AllocationStatus AllocationStatus { get; init; }
    public List<string> Skills { get; init; } = [];
    public List<Allocations.AllocationDetailResponse> Allocations { get; init; } = [];
}
