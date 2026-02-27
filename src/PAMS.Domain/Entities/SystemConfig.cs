namespace PAMS.Domain.Entities;

/// <summary>
/// System configuration aggregate. Maps to key-value rows in the DB
/// but is presented as a single object in the domain.
/// Properties are virtual to support test substitution.
/// </summary>
public class SystemConfig
{
    public Guid Id { get; set; }
    public virtual int MinAllocationPercentage { get; set; } = 25;
    public virtual int AllocationIncrement { get; set; } = 5;
    public DateTime UpdatedAt { get; set; }
}
