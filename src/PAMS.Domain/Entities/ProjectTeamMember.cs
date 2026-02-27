using PAMS.Domain.Common;

namespace PAMS.Domain.Entities;

public class ProjectTeamMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid TeamLeadId { get; set; }
    public Guid ReporteeId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual Project Project { get; set; } = null!;
    public virtual Employee TeamLead { get; set; } = null!;
    public virtual Employee Reportee { get; set; } = null!;
}
