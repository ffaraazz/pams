namespace PAMS.Domain.Exceptions;

/// <summary>
/// Thrown when assigning a reporting line would create a circular chain (FR-007, FR-020).
/// Mapped to HTTP 422 with ERR_CIRCULAR_REPORTING or ERR_CIRCULAR_TEAM_LEAD.
/// </summary>
public sealed class CircularReportingException : DomainException
{
    public CircularReportingException(string message)
        : base(message, "ERR_CIRCULAR_REPORTING")
    {
    }

    public CircularReportingException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}
