using System.Text.Json;
using PAMS.Application.Interfaces;
using PAMS.Domain.Entities;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Services;

/// <summary>
/// Writes audit log entries. Never throws to caller.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly PamsDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditLogService(PamsDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorId = _currentUser.EmployeeId,
                Action = action,
                EntityType = ExtractEntityType(action),
                Payload = payload is not null ? JsonSerializer.Serialize(payload) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Audit logging must never throw to the caller
        }
    }

    private static string ExtractEntityType(string action)
    {
        // action format: "entity.verb" e.g. "allocation.created"
        var dotIndex = action.IndexOf('.');
        return dotIndex > 0 ? action[..dotIndex] : action;
    }
}
