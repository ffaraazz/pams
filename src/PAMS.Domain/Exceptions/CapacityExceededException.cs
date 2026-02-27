namespace PAMS.Domain.Exceptions;

/// <summary>
/// Thrown when an allocation would exceed 100% capacity for an employee (FR-011).
/// Mapped to HTTP 422 with ERR_CAPACITY_EXCEEDED.
/// </summary>
public sealed class CapacityExceededException : DomainException
{
    public int CurrentTotal { get; }
    public int Requested { get; }
    public int Available { get; }

    public CapacityExceededException(int currentTotal, int requested)
        : base(
            $"Capacity exceeded: current total is {currentTotal}%, requested {requested}%, available {100 - currentTotal}%.",
            "ERR_CAPACITY_EXCEEDED")
    {
        CurrentTotal = currentTotal;
        Requested = requested;
        Available = 100 - currentTotal;
    }
}
