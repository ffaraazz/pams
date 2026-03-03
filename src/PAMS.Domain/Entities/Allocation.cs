using PAMS.Domain.Common;

namespace PAMS.Domain.Entities;

public class Allocation : IAuditableEntity, ISoftDeletable
{
    public virtual Guid Id { get; set; }
    public virtual Guid EmployeeId { get; set; }
    public virtual Guid ProjectId { get; set; }
    public virtual DateOnly FromDate { get; set; }
    public virtual DateOnly? ToDate { get; set; }
    public virtual int Percentage { get; set; }
    public virtual string? ProjectRole { get; set; }
    public virtual Guid AllocatedById { get; set; }
    public virtual DateTime? DeletedAt { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
    public virtual Project Project { get; set; } = null!;
    public virtual Employee AllocatedBy { get; set; } = null!;
}
