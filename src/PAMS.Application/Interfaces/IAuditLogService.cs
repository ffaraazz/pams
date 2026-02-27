namespace PAMS.Application.Interfaces;

/// <summary>
/// Writes entries to the audit_logs table.
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(string action, object? payload = null, CancellationToken ct = default);
}
