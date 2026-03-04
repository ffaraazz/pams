namespace PAMS.Domain.Enums;

/// <summary>
/// Computed allocation band for an employee in a date window. Not stored — derived at runtime.
/// </summary>
public enum AllocationStatus
{
    /// <summary>0% allocated — fully available.</summary>
    Bench,
    /// <summary>Between 1% and 99% allocated — partially available.</summary>
    Partial,
    /// <summary>100% allocated on at least one day in the window.</summary>
    Full
}
