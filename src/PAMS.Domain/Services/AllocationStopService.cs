namespace PAMS.Domain.Services;

/// <summary>
/// Stop-date computation rule (FR-013). Pure function; no I/O.
/// If allocation.fromDate > today → stopDate = today (cancel before start)
/// Else                           → stopDate = today + 1 day (release from tomorrow)
/// </summary>
public sealed class AllocationStopService
{
    /// <summary>
    /// Computes the effective stop date for an allocation.
    /// </summary>
    /// <param name="allocationFromDate">The allocation's start date.</param>
    /// <param name="today">The current date.</param>
    /// <returns>The computed stop date.</returns>
    public DateOnly ComputeStopDate(DateOnly allocationFromDate, DateOnly today)
    {
        if (allocationFromDate > today)
        {
            // Allocation hasn't started yet — cancel it: set toDate = today
            return today;
        }

        // Allocation has started (fromDate <= today) — release from tomorrow
        return today.AddDays(1);
    }
}
