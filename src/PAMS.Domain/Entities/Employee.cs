using PAMS.Domain.Common;
using PAMS.Domain.Enums;

namespace PAMS.Domain.Entities;

public class Employee : IAuditableEntity
{
    public virtual Guid Id { get; set; }
    public virtual string EmpCode { get; set; } = string.Empty;
    public virtual string FirstName { get; set; } = string.Empty;
    public virtual string LastName { get; set; } = string.Empty;
    public virtual string Email { get; set; } = string.Empty;
    public virtual string Designation { get; set; } = string.Empty;
    public virtual EmployeeRole Role { get; set; }
    public virtual Guid? ReportsToId { get; set; }
    public virtual bool IsActive { get; set; } = true;
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime UpdatedAt { get; set; }

    // Navigation
    public virtual Employee? ReportsTo { get; set; }
    public virtual ICollection<Employee> DirectReports { get; set; } = [];
    public virtual ICollection<Allocation> Allocations { get; set; } = [];
    public virtual ICollection<EmployeeSkill> EmployeeSkills { get; set; } = [];
    public virtual ICollection<Project> ManagedProjects { get; set; } = [];
}
