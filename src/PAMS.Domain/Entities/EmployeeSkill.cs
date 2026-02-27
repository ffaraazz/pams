namespace PAMS.Domain.Entities;

public class EmployeeSkill
{
    public Guid EmployeeId { get; set; }
    public Guid SkillId { get; set; }

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
    public virtual Skill Skill { get; set; } = null!;
}
