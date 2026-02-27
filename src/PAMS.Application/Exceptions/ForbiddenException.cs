namespace PAMS.Application.Exceptions;

/// <summary>
/// Thrown when the current user does not have permission for the operation.
/// Mapped to HTTP 403.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public string ErrorCode { get; }

    public ForbiddenException(string message, string errorCode = "ERR_FORBIDDEN")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
