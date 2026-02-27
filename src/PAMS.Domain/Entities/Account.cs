using PAMS.Domain.Common;
using PAMS.Domain.Enums;

namespace PAMS.Domain.Entities;

public class Account : IAuditableEntity
{
    public virtual Guid Id { get; set; }
    public virtual string AccountCode { get; set; } = string.Empty;
    public virtual string AccountName { get; set; } = string.Empty;
    public virtual AccountType AccountType { get; set; }
    public virtual bool IsActive { get; set; } = true;
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<Project> Projects { get; set; } = [];
}
