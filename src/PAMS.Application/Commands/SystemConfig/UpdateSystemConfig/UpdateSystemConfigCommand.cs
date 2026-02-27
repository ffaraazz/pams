using MediatR;
using PAMS.Application.DTOs.SystemConfig;

namespace PAMS.Application.Commands.SystemConfig;

public sealed record UpdateSystemConfigCommand : IRequest<SystemConfigResponse>
{
    public required int MinAllocationPct { get; init; }
    public required int AllocationIncrement { get; init; }
}
