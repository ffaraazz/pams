using MediatR;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.ProjectTeamMembers;

public sealed class RemoveProjectTeamMemberCommandHandler
    : IRequestHandler<RemoveProjectTeamMemberCommand, Unit>
{
    private readonly IProjectRepository _projectRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IProjectTeamMemberRepository _teamMemberRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public RemoveProjectTeamMemberCommandHandler(
        IProjectRepository projectRepo,
        IEmployeeRepository employeeRepo,
        IProjectTeamMemberRepository teamMemberRepo,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _projectRepo = projectRepo;
        _employeeRepo = employeeRepo;
        _teamMemberRepo = teamMemberRepo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<Unit> Handle(RemoveProjectTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByCodeAsync(request.ProjectCode, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectCode);

        // PM scope check
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            throw new ForbiddenException(
                "Project Managers can only configure their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        var teamLead = await _employeeRepo.GetByEmpCodeAsync(request.TeamLeadEmpCode, cancellationToken)
            ?? throw new NotFoundException("Employee", request.TeamLeadEmpCode);

        var reportee = await _employeeRepo.GetByEmpCodeAsync(request.ReporteeEmpCode, cancellationToken)
            ?? throw new NotFoundException("Employee", request.ReporteeEmpCode);

        if (!await _teamMemberRepo.ExistsAsync(project.Id, teamLead.Id, reportee.Id, cancellationToken))
            throw new NotFoundException(
                $"Team member assignment not found for project '{request.ProjectCode}'.");

        await _teamMemberRepo.RemoveAsync(project.Id, teamLead.Id, reportee.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("teamMember.removed", new
        {
            project.ProjectCode,
            TeamLeadEmpCode = teamLead.EmpCode,
            ReporteeEmpCode = reportee.EmpCode
        }, cancellationToken);

        return Unit.Value;
    }
}
