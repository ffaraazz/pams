using FluentValidation;
using PAMS.Application.Commands.Allocations;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Validators;

/// <summary>
/// Validates CreateAllocationCommand per FR-010 acceptance criteria.
/// Checks percentage min/max/increment, date constraints, required fields.
/// </summary>
public sealed class CreateAllocationCommandValidator : AbstractValidator<CreateAllocationCommand>
{
    private readonly ISystemConfigRepository _configRepo;

    public CreateAllocationCommandValidator(ISystemConfigRepository configRepo)
    {
        _configRepo = configRepo;

        RuleFor(c => c.ProjectCode)
            .NotEmpty().WithMessage("Project code is required.");

        RuleFor(c => c.EmpCode)
            .NotEmpty().WithMessage("Employee code is required.");

        RuleFor(c => c.FromDate)
            .NotEqual(default(DateOnly)).WithMessage("From date is required.");

        RuleFor(c => c.ToDate)
            .Must((cmd, toDate) => toDate is null || toDate >= cmd.FromDate)
            .WithMessage("To date must be greater than or equal to from date.");

        RuleFor(c => c.Percentage)
            .MustAsync(async (pct, ct) =>
            {
                var config = await _configRepo.GetAsync(ct);
                return pct >= config.MinAllocationPercentage;
            })
            .WithMessage("Percentage must be at least the configured minimum.")
            .DependentRules(() =>
            {
                RuleFor(c => c.Percentage)
                    .LessThanOrEqualTo(100)
                    .WithMessage("Percentage cannot exceed 100%.");

                RuleFor(c => c.Percentage)
                    .MustAsync(async (pct, ct) =>
                    {
                        var config = await _configRepo.GetAsync(ct);
                        return pct % config.AllocationIncrement == 0;
                    })
                    .WithMessage("Percentage must be a multiple of the configured increment.");
            });
    }
}
