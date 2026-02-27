namespace PAMS.Domain.Entities;

public class Skill
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual ICollection<EmployeeSkill> EmployeeSkills { get; set; } = [];
}
