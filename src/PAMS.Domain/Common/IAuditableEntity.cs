namespace PAMS.Domain.Common;

/// <summary>
/// Marks an entity as auditable with created/updated timestamps.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
