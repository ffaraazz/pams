using MediatR;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Command to soft-delete an ended allocation (FR-014).
/// Only allocations with toDate &lt; today may be removed.
/// </summary>
public sealed record RemoveAllocationCommand : IRequest<Unit>
{
    public required Guid AllocationId { get; init; }
}
