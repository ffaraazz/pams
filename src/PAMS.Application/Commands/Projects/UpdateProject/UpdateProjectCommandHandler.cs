using MediatR;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Projects;

public sealed class UpdateProjectCommandHandler
    : IRequestHandler<UpdateProjectCommand, ProjectDetailResponse>
{
    private readonly IProjectRepository _projectRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepo,
        IEmployeeRepository employeeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _projectRepo = projectRepo;
        _employeeRepo = employeeRepo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<ProjectDetailResponse> Handle(
        UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepo.GetByCodeAsync(request.ProjectCode, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectCode);

        // PM scope enforcement
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            throw new ForbiddenException(
                "Project Managers can only update their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        Employee? pm = null;
        if (!string.IsNullOrWhiteSpace(request.ProjectManagerEmpCode))
        {
            pm = await _employeeRepo.GetByEmpCodeAsync(request.ProjectManagerEmpCode, cancellationToken)
                ?? throw new NotFoundException("Employee", request.ProjectManagerEmpCode);
        }

        project.ProjectName = request.ProjectName;
        project.ProjectManagerId = pm?.Id;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.Billable = request.Billable;
        if (request.IsActive.HasValue)
            project.IsActive = request.IsActive.Value;
        project.UpdatedAt = DateTime.UtcNow;

        _projectRepo.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("project.updated", new
        {
            project.Id,
            project.ProjectCode
        }, cancellationToken);

        var account = project.Account;
        return new ProjectDetailResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            ProjectManagerId = pm?.Id ?? project.ProjectManagerId,
            ProjectManagerEmpCode = pm?.EmpCode ?? project.ProjectManager?.EmpCode,
            ProjectManagerName = pm is not null
                ? $"{pm.FirstName} {pm.LastName}"
                : project.ProjectManager is not null
                    ? $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}"
                    : null,
            Status = project.Status,
            Billable = project.Billable,
            IsActive = project.IsActive,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
