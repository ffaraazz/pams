using PAMS.Domain.Entities;

namespace PAMS.Application.DTOs.Allocations;

/// <summary>
/// Detailed allocation response returned after create/update operations.
/// </summary>
public sealed record AllocationDetailResponse
{
    public Guid AllocationId { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmpCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int Percentage { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProjectRole { get; init; }
    public bool Billable { get; init; }
    public bool ProjectBillable { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    /// <summary>Computed allocation timeline status: Active (ongoing), Upcoming (starts in future), or Ended (toDate is past). Not stored — derived from fromDate/toDate vs today.</summary>
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }

    public static string ComputeAllocationStatus(DateOnly fromDate, DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (fromDate > today) return "Upcoming";
        if (toDate.HasValue && toDate.Value < today) return "Ended";
        return "Active";
    }

    /// <summary>
    /// Maps an <see cref="Allocation"/> with loaded navigation properties to a response DTO.
    /// Requires <c>allocation.Employee</c>, <c>allocation.Project</c>, and
    /// <c>allocation.Project.Account</c> to be eagerly loaded.
    /// </summary>
    public static AllocationDetailResponse MapFrom(Allocation allocation)
        => MapFrom(allocation, allocation.Employee, allocation.Project);

    /// <summary>
    /// Maps an <see cref="Allocation"/> to a response DTO using separately loaded
    /// <see cref="Employee"/> and <see cref="Project"/> entities.
    /// </summary>
    public static AllocationDetailResponse MapFrom(Allocation allocation, Employee? employee, Project? project)
    {
        return new AllocationDetailResponse
        {
            AllocationId = allocation.Id,
            EmployeeId = allocation.EmployeeId,
            EmpCode = employee?.EmpCode ?? string.Empty,
            EmployeeName = employee is not null
                ? $"{employee.FirstName} {employee.LastName}" : string.Empty,
            ProjectRole = allocation.ProjectRole,
            ProjectId = allocation.ProjectId,
            ProjectCode = project?.ProjectCode ?? string.Empty,
            ProjectName = project?.ProjectName ?? string.Empty,
            Billable = allocation.Billable,
            ProjectBillable = project?.Billable ?? false,
            AccountCode = project?.Account?.AccountCode ?? string.Empty,
            AccountName = project?.Account?.AccountName ?? string.Empty,
            Percentage = allocation.Percentage,
            FromDate = allocation.FromDate,
            ToDate = allocation.ToDate,
            Status = ComputeAllocationStatus(allocation.FromDate, allocation.ToDate),
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }
}
