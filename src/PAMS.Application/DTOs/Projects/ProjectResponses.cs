using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.ProjectTeamMembers;
using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Projects;

public sealed record ProjectSummaryResponse
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public Guid? ProjectManagerId { get; init; }
    public string? ProjectManagerEmpCode { get; init; }
    public string? ProjectManagerName { get; init; }
    /// <summary>Lifecycle stage of the project (Upcoming → Active → Completed). This is manually managed and independent of isActive.</summary>
    public ProjectStatus Status { get; init; }
    public bool Billable { get; init; }
    /// <summary>Soft-delete/archive flag. False means the project is deactivated or archived. Independent of Status — a Completed project can still be active (visible), and an Active project can be deactivated.</summary>
    public bool IsActive { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int ResourceCount { get; init; }
}

public sealed record ProjectDetailResponse
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public Guid? ProjectManagerId { get; init; }
    public string? ProjectManagerEmpCode { get; init; }
    public string? ProjectManagerName { get; init; }
    /// <summary>Lifecycle stage of the project (Upcoming → Active → Completed). This is manually managed and independent of isActive.</summary>
    public ProjectStatus Status { get; init; }
    public bool Billable { get; init; }
    /// <summary>Soft-delete/archive flag. False means the project is deactivated or archived. Independent of Status — a Completed project can still be active (visible), and an Active project can be deactivated.</summary>
    public bool IsActive { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ResourceCount { get; init; }
    public IReadOnlyList<AllocationDetailResponse> Allocations { get; init; } = Array.Empty<AllocationDetailResponse>();
    public IReadOnlyList<ProjectTeamMemberResponse> TeamMembers { get; init; } = Array.Empty<ProjectTeamMemberResponse>();
}
