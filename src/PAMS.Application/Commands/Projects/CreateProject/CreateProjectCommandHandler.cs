using MediatR;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Projects;

public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, ProjectDetailResponse>
{
    private readonly IProjectRepository _projectRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepo,
        IAccountRepository accountRepo,
        IEmployeeRepository employeeRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _projectRepo = projectRepo;
        _accountRepo = accountRepo;
        _employeeRepo = employeeRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<ProjectDetailResponse> Handle(
        CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _projectRepo.CodeExistsAsync(request.ProjectCode, cancellationToken))
            throw new ConflictException(
                $"Project code '{request.ProjectCode}' is already in use.",
                "ERR_PROJECT_CODE_EXISTS");

        var account = await _accountRepo.GetByCodeAsync(request.AccountCode, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountCode);

        Employee? pm = null;
        if (!string.IsNullOrWhiteSpace(request.ProjectManagerEmpCode))
        {
            pm = await _employeeRepo.GetByEmpCodeAsync(request.ProjectManagerEmpCode, cancellationToken)
                ?? throw new NotFoundException("Employee", request.ProjectManagerEmpCode);
        }

        // Billable defaults to true for Client accounts
        var billable = request.Billable ?? (account.AccountType == AccountType.Client);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = request.ProjectCode,
            ProjectName = request.ProjectName,
            AccountId = account.Id,
            ProjectManagerId = pm?.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ProjectStatus.Upcoming,
            Billable = billable,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectRepo.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("project.created", new
        {
            project.Id,
            project.ProjectCode,
            project.AccountId
        }, cancellationToken);

        return new ProjectDetailResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            ProjectManagerId = pm?.Id,
            ProjectManagerEmpCode = pm?.EmpCode,
            ProjectManagerName = pm is not null ? $"{pm.FirstName} {pm.LastName}" : null,
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
