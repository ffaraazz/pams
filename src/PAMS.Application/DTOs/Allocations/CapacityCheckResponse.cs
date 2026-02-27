using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Allocations;

public sealed record CapacityCheckResponse
{
    public string EmpCode { get; init; } = string.Empty;
    public DateOnly WindowFrom { get; init; }
    public DateOnly? WindowTo { get; init; }
    public int CurrentTotalPercentage { get; init; }
    public int AvailablePercentage { get; init; }
    public AllocationStatus AllocationStatus { get; init; }
}
