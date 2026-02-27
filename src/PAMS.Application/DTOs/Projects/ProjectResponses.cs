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
    public ProjectStatus Status { get; init; }
    public bool Billable { get; init; }
    public bool IsActive { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
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
    public ProjectStatus Status { get; init; }
    public bool Billable { get; init; }
    public bool IsActive { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
