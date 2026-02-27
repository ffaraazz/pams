namespace PAMS.Domain.Exceptions;

/// <summary>
/// Base domain exception. All domain-specific exceptions derive from this.
/// Mapped to HTTP 422 by ExceptionHandlerMiddleware.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "ERR_DOMAIN")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
