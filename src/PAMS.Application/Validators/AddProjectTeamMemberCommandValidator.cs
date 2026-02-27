using FluentValidation;
using PAMS.Application.Commands.ProjectTeamMembers;

namespace PAMS.Application.Validators;

/// <summary>
/// Validates AddProjectTeamMemberCommand per FR-020 acceptance criteria.
/// Self-assignment blocked. Circular detection handled in the handler via TeamLeadValidator.
/// </summary>
public sealed class AddProjectTeamMemberCommandValidator : AbstractValidator<AddProjectTeamMemberCommand>
{
    public AddProjectTeamMemberCommandValidator()
    {
        RuleFor(c => c.ProjectCode)
            .NotEmpty().WithMessage("Project code is required.");

        RuleFor(c => c.TeamLeadEmpCode)
            .NotEmpty().WithMessage("Team lead employee code is required.");

        RuleFor(c => c.ReporteeEmpCode)
            .NotEmpty().WithMessage("Reportee employee code is required.")
            .Must((cmd, reporteeEmpCode) =>
                !string.Equals(cmd.TeamLeadEmpCode, reporteeEmpCode, StringComparison.OrdinalIgnoreCase))
            .WithMessage("An employee cannot be assigned as their own reportee.");
    }
}
