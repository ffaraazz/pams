using MediatR;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Employees;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ISkillRepository _skillRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateEmployeeCommandHandler(
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

    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepo.GetByEmpCodeAsync(request.EmpCode, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmpCode);

        // Email uniqueness (if changed)
        if (!string.Equals(employee.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _employeeRepo.ExistsByEmailAsync(request.Email, cancellationToken))
                throw new ConflictException(
                    $"Email '{request.Email}' is already in use.",
                    "ERR_EMAIL_EXISTS");
        }

        // Reporting chain validation
        Guid? reportsToId = null;
        if (!string.IsNullOrWhiteSpace(request.ReportsToEmpCode))
        {
            var manager = await _employeeRepo.GetByEmpCodeAsync(request.ReportsToEmpCode, cancellationToken)
                ?? throw new NotFoundException("Employee", request.ReportsToEmpCode);
            reportsToId = manager.Id;

            if (await _employeeRepo.WouldCreateCircularReportingAsync(employee.Id, manager.Id, cancellationToken))
                throw new CircularReportingException(
                    "Setting this reportsTo would create a circular reporting chain.");
        }

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Designation = request.Designation;
        employee.Role = request.Role;
        employee.ReportsToId = reportsToId;
        if (request.IsActive.HasValue)
            employee.IsActive = request.IsActive.Value;
        employee.UpdatedAt = DateTime.UtcNow;

        // Update skills — clear and re-add
        employee.EmployeeSkills.Clear();
        foreach (var skillId in request.SkillIds)
        {
            employee.EmployeeSkills.Add(new EmployeeSkill
            {
                EmployeeId = employee.Id,
                SkillId = skillId
            });
        }

        _employeeRepo.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("employee.updated", new
        {
            employee.Id,
            employee.EmpCode
        }, cancellationToken);

        return Unit.Value;
    }
}
