using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Employees;

public sealed record EmployeeSummaryResponse
{
    public Guid EmployeeId { get; init; }
    public string EmpCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public EmployeeRole Role { get; init; }
    /// <summary>Whether the employee account is active. Inactive employees cannot be allocated to projects.</summary>
    public bool IsActive { get; init; }
    public int AvailabilityPercentage { get; init; }
    public AllocationStatus AllocationStatus { get; init; }
    public List<string> Skills { get; init; } = [];
}

public sealed record EmployeeDetailResponse
{
    public Guid EmployeeId { get; init; }
    public string EmpCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public EmployeeRole Role { get; init; }
    /// <summary>Whether the employee account is active. Inactive employees cannot be allocated to projects.</summary>
    public bool IsActive { get; init; }
    public string Email { get; init; } = string.Empty;
    public int AvailabilityPercentage { get; init; }
    public AllocationStatus AllocationStatus { get; init; }
    public Guid? ReportsToId { get; init; }
    public string? ReportsToEmpCode { get; init; }
    public string? ReportsToName { get; init; }
    public List<string> Skills { get; init; } = [];
    public List<SkillDetailItem> SkillDetails { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SkillDetailItem
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    /// <summary>Whether the skill is active in the master skill list.</summary>
    public bool IsActive { get; init; }
}
