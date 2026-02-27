using MediatR;
using PAMS.Application.DTOs.Skills;

namespace PAMS.Application.Commands.Skills;

public sealed record UpdateSkillCommand : IRequest<SkillResponse>
{
    public required Guid SkillId { get; init; }
    public string? SkillName { get; init; }
    public bool? IsActive { get; init; }
}
