namespace PAMS.Application.DTOs.Common;

/// <summary>
/// Optional request body for export endpoints.
/// Contains column mappings: key = field name (camelCase matching response DTO property), value = display label.
/// Only mapped fields appear in the export. If null/empty, all default columns are included.
/// </summary>
public sealed record ExportRequest
{
    /// <summary>
    /// Column name mappings. Key is the camelCase field name (e.g., "empCode"),
    /// value is the display label (e.g., "Employee Code").
    /// Only fields present in this map will appear in the export.
    /// If null or empty, all default columns are included.
    /// </summary>
    public Dictionary<string, string>? Columns { get; init; }
}
