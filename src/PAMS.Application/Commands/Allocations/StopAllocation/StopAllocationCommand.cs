using MediatR;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Command to stop an active allocation (FR-013).
/// Sets toDate based on AllocationStopService rules.
/// </summary>
public sealed record StopAllocationCommand : IRequest<Unit>
{
    public required Guid AllocationId { get; init; }
}
