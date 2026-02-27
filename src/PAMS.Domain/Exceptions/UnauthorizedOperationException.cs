namespace PAMS.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is not permitted for the current user.
/// Mapped to HTTP 403 by ExceptionHandlerMiddleware.
/// </summary>
public sealed class UnauthorizedOperationException : DomainException
{
    public UnauthorizedOperationException(string message)
        : base(message, "ERR_UNAUTHORIZED")
    {
    }
}
