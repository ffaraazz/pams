using MediatR;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.Domain.Services;

namespace PAMS.Application.Commands.Employees;

/// <summary>
/// Handles CreateEmployeeCommand (FR-007).
/// Flow: uniqueness checks → reporting chain validation → persist → audit.
/// </summary>
public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Guid>
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepo,
        ISkillRepository skillRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _employeeRepo = employeeRepo;
        _skillRepo = skillRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<Guid> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Uniqueness checks
        if (await _employeeRepo.ExistsByEmpCodeAsync(request.EmpCode, cancellationToken))
            throw new ConflictException(
                $"Employee code '{request.EmpCode}' is already in use.",
                "ERR_EMPCODE_EXISTS");

        if (await _employeeRepo.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ConflictException(
                $"Email '{request.Email}' is already in use.",
                "ERR_EMAIL_EXISTS");

        // Reporting chain validation
        Guid? reportsToId = null;
        if (!string.IsNullOrWhiteSpace(request.ReportsToEmpCode))
        {
            var manager = await _employeeRepo.GetByEmpCodeAsync(request.ReportsToEmpCode, cancellationToken)
                ?? throw new NotFoundException("Employee", request.ReportsToEmpCode);
            reportsToId = manager.Id;
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = request.EmpCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Designation = request.Designation,
            Role = request.Role,
            ReportsToId = reportsToId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add skills
        foreach (var skillId in request.SkillIds)
        {
            var skill = await _skillRepo.GetByIdAsync(skillId, cancellationToken);
            if (skill is not null)
            {
                employee.EmployeeSkills.Add(new EmployeeSkill
                {
                    EmployeeId = employee.Id,
                    SkillId = skillId
                });
            }
        }

        await _employeeRepo.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("employee.created", new
        {
            employee.Id,
            employee.EmpCode,
            employee.Role
        }, cancellationToken);

        return employee.Id;
    }
}
