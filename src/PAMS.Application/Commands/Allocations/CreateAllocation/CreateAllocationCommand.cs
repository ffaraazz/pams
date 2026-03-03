using MediatR;
using PAMS.Application.DTOs.Allocations;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Command to create a new allocation (FR-010).
/// </summary>
public sealed record CreateAllocationCommand : IRequest<AllocationDetailResponse>
{
    public required string ProjectCode { get; init; }
    public required string EmpCode { get; init; }
    public required int Percentage { get; init; }
    public required DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public string? ProjectRole { get; init; }
}
