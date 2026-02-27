namespace PAMS.Application.Interfaces;

/// <summary>
/// Abstraction for date/time to enable test determinism.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
