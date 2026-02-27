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
}
