using FluentValidation;
using PAMS.Application.Commands.Allocations;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Validators;

public sealed class UpdateAllocationCommandValidator : AbstractValidator<UpdateAllocationCommand>
{
    public UpdateAllocationCommandValidator(ISystemConfigRepository configRepo)
    {
        RuleFor(c => c.AllocationId)
            .NotEmpty().WithMessage("Allocation ID is required.");

        RuleFor(c => c.FromDate)
            .NotEqual(default(DateOnly)).WithMessage("From date is required.");

        RuleFor(c => c.ToDate)
            .Must((cmd, toDate) => toDate is null || toDate >= cmd.FromDate)
            .WithMessage("To date must be greater than or equal to from date.");

        RuleFor(c => c.Percentage)
            .MustAsync(async (pct, ct) =>
            {
                var config = await configRepo.GetAsync(ct);
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
                        var config = await configRepo.GetAsync(ct);
                        return pct % config.AllocationIncrement == 0;
                    })
                    .WithMessage("Percentage must be a multiple of the configured increment.");
            });
    }
}
