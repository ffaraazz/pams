namespace PAMS.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable with a deletedAt timestamp.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
