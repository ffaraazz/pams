namespace PAMS.Application.Exceptions;

/// <summary>
/// Thrown when a requested entity is not found. Mapped to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public string ErrorCode { get; } = "ERR_NOT_FOUND";

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
