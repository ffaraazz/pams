using PAMS.Domain.Common;
using PAMS.Domain.Enums;

namespace PAMS.Domain.Entities;

public class Project : IAuditableEntity
{
    public virtual Guid Id { get; set; }
    public virtual string ProjectCode { get; set; } = string.Empty;
    public virtual string ProjectName { get; set; } = string.Empty;
    public virtual Guid AccountId { get; set; }
    public virtual Guid? ProjectManagerId { get; set; }
    public virtual DateOnly StartDate { get; set; }
    public virtual DateOnly? EndDate { get; set; }
    public virtual ProjectStatus Status { get; set; } = ProjectStatus.Upcoming;
    public virtual bool Billable { get; set; } = true;
    public virtual bool IsActive { get; set; } = true;
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual Account Account { get; set; } = null!;
    public virtual Employee? ProjectManager { get; set; }
    public virtual ICollection<Allocation> Allocations { get; set; } = [];
    public virtual ICollection<ProjectTeamMember> TeamMembers { get; set; } = [];
}
