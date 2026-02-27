using PAMS.Domain.Exceptions;

namespace PAMS.Domain.Services;

/// <summary>
/// Core capacity check algorithm (FR-011). Pure function; no I/O.
/// Validates that existingTotal + requestedPercentage does not exceed 100%.
/// </summary>
public sealed class AllocationCapacityService
{
    /// <summary>
    /// Validates that adding a new allocation percentage to the existing total
    /// does not exceed the 100% capacity limit.
    /// </summary>
    /// <param name="existingTotal">Sum of overlapping active allocation percentages.</param>
    /// <param name="requestedPercentage">The new allocation percentage being requested.</param>
    /// <param name="fromDate">Start date of the new allocation (for error context).</param>
    /// <param name="toDate">End date of the new allocation (for error context).</param>
    /// <exception cref="CapacityExceededException">
    /// Thrown when existingTotal + requestedPercentage exceeds 100%.
    /// </exception>
    public void Validate(int existingTotal, int requestedPercentage, DateOnly fromDate, DateOnly? toDate)
    {
        if (existingTotal + requestedPercentage > 100)
        {
            throw new CapacityExceededException(existingTotal, requestedPercentage);
        }
    }
}
