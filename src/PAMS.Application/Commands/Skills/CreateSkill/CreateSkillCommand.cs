using MediatR;
using PAMS.Application.DTOs.Skills;

namespace PAMS.Application.Commands.Skills;

public sealed record CreateSkillCommand : IRequest<SkillResponse>
{
    public required string SkillName { get; init; }
}
