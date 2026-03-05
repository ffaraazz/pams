namespace PAMS.Application.Helpers;

public static class SortHelper
{
    public static (string PropertyName, bool IsDescending) Parse(
        string? sort, string defaultField, bool defaultDescending,
        IReadOnlyDictionary<string, string> allowedFields)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return (defaultField, defaultDescending);

        bool isDescending = false;
        var field = sort.Trim();

        if (field.StartsWith('-'))
        {
            isDescending = true;
            field = field[1..];
        }

        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException($"Invalid sort field. Allowed fields: {string.Join(", ", allowedFields.Keys)}");

        // Case-insensitive lookup
        var match = allowedFields.FirstOrDefault(
            kv => kv.Key.Equals(field, StringComparison.OrdinalIgnoreCase));

        if (match.Key is null)
            throw new ArgumentException($"Invalid sort field '{field}'. Allowed fields: {string.Join(", ", allowedFields.Keys)}");

        return (match.Value, isDescending);
    }
}
