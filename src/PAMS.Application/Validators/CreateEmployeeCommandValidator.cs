using FluentValidation;
using PAMS.Application.Commands.Employees;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Validators;

/// <summary>
/// Validates CreateEmployeeCommand per FR-007 acceptance criteria.
/// </summary>
public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator(IEmployeeRepository employeeRepo)
    {
        RuleFor(c => c.EmpCode)
            .NotEmpty().WithMessage("Employee code is required.")
            .MustAsync(async (empCode, ct) =>
            {
                var exists = await employeeRepo.ExistsByEmpCodeAsync(empCode, ct);
                return !exists;
            })
            .WithErrorCode("ERR_EMPCODE_EXISTS")
            .WithMessage("Employee code already exists.");

        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(@"^[^\s@]+@[^\s@]+\.[^\s@]+$").WithMessage("Email must be a valid email address.")
            .MustAsync(async (email, ct) =>
            {
                var exists = await employeeRepo.ExistsByEmailAsync(email, ct);
                return !exists;
            })
            .WithMessage("Email already exists.");

        RuleFor(c => c.Role)
            .IsInEnum().WithMessage("Role must be one of: HR, ProjectManager, Staff.");

        RuleFor(c => c.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(150).WithMessage("Designation must not exceed 150 characters.");
    }
}
