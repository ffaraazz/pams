using MediatR;

namespace PAMS.Application.Commands.ProjectTeamMembers;

/// <summary>
/// Command to add a team-lead / reportee assignment on a project (FR-020).
/// </summary>
public sealed record AddProjectTeamMemberCommand : IRequest<Unit>
{
    public required string ProjectCode { get; init; }
    public required string TeamLeadEmpCode { get; init; }
    public required string ReporteeEmpCode { get; init; }
}
