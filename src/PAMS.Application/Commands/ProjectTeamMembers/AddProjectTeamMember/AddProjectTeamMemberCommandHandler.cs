using MediatR;
using PAMS.Application.DTOs.ProjectTeamMembers;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.Domain.Services;

namespace PAMS.Application.Commands.ProjectTeamMembers;

public sealed class AddProjectTeamMemberCommandHandler
    : IRequestHandler<AddProjectTeamMemberCommand, Unit>
{
    private readonly IProjectRepository _projectRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IProjectTeamMemberRepository _teamMemberRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public AddProjectTeamMemberCommandHandler(
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

    public async Task<Unit> Handle(AddProjectTeamMemberCommand request, CancellationToken cancellationToken)
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

        // Duplicate check
        if (await _teamMemberRepo.ExistsAsync(project.Id, teamLead.Id, reportee.Id, cancellationToken))
            throw new ConflictException(
                "This team-lead / reportee assignment already exists for this project.",
                "ERR_DUPLICATE_TEAM_MEMBER");

        // Circular check
        if (await _teamMemberRepo.WouldCreateCircularLeadershipAsync(
            project.Id, teamLead.Id, reportee.Id, cancellationToken))
            throw new CircularReportingException(
                "Circular reporting detected: the reportee is already a Team Lead of the specified team lead on this project.",
                "ERR_CIRCULAR_TEAM_LEAD");

        var member = new ProjectTeamMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            TeamLeadId = teamLead.Id,
            ReporteeId = reportee.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _teamMemberRepo.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("teamMember.added", new
        {
            member.Id,
            project.ProjectCode,
            TeamLeadEmpCode = teamLead.EmpCode,
            ReporteeEmpCode = reportee.EmpCode
        }, cancellationToken);

        return Unit.Value;
    }
}
