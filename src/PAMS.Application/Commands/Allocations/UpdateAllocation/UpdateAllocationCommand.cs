using MediatR;
using PAMS.Application.DTOs.Allocations;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Command to update an allocation's percentage or date range (FR-012).
/// </summary>
public sealed record UpdateAllocationCommand : IRequest<AllocationDetailResponse>
{
    public required Guid AllocationId { get; init; }
    public required DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public required int Percentage { get; init; }
    public string? ProjectRole { get; init; }
    public bool? Billable { get; init; }
}
