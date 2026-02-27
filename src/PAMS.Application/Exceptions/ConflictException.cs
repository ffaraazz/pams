namespace PAMS.Application.Exceptions;

/// <summary>
/// Thrown when a unique constraint would be violated.
/// Mapped to HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public string ErrorCode { get; }

    public ConflictException(string message, string errorCode = "ERR_CONFLICT")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
