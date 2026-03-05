namespace PAMS.Application.Exceptions;

/// <summary>
/// Thrown when an invalid sort field is specified. Mapped to HTTP 400.
/// </summary>
public sealed class InvalidSortException : Exception
{
    public string Field { get; }
    public IReadOnlyList<string> AllowedFields { get; }

    public InvalidSortException(string field, IEnumerable<string> allowedFields)
        : base($"Invalid sort field '{field}'. Allowed fields: {string.Join(", ", allowedFields)}")
    {
        Field = field;
        AllowedFields = allowedFields.ToList().AsReadOnly();
    }
}
