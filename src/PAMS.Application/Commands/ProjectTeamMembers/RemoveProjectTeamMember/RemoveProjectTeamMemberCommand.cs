using MediatR;

namespace PAMS.Application.Commands.ProjectTeamMembers;

/// <summary>
/// Command to remove a team-lead / reportee assignment (FR-020).
/// </summary>
public sealed record RemoveProjectTeamMemberCommand : IRequest<Unit>
{
    public required string ProjectCode { get; init; }
    public required string TeamLeadEmpCode { get; init; }
    public required string ReporteeEmpCode { get; init; }
}
