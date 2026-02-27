using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Infrastructure.Persistence;

namespace PAMS.IntegrationTests.Helpers;

/// <summary>
/// Provides test data seeding for integration tests.
/// Seeds known employees, accounts, projects, and skills for deterministic tests.
/// </summary>
public static class SeedData
{
    // ─── Well-known IDs ─────────────────────────────────────────────────────

    public static readonly Guid HrEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PmEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid StaffEmployeeId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid AccountId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid ProjectId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid SkillCSharpId = Guid.Parse("00000000-0000-0000-0000-000000000030");
    public static readonly Guid SkillSqlId = Guid.Parse("00000000-0000-0000-0000-000000000031");

    public const string HrEmpCode = "INT-HR-001";
    public const string PmEmpCode = "INT-PM-001";
    public const string StaffEmpCode = "INT-STAFF-001";
    public const string AccountCode = "INT-ACC-001";
    public const string ProjectCode = "INT-PRJ-001";

    /// <summary>
    /// Seeds foundation test data. Call during fixture or test setup.
    /// Idempotent — skips if data already exists.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PamsDbContext>();

        // Ensure schema/migrations applied
        await db.Database.MigrateAsync();

        // Skip if already seeded
        if (await db.Set<Employee>().AnyAsync(e => e.EmpCode == HrEmpCode))
            return;

        // Skills
        db.Set<Skill>().AddRange(
            new Skill { Id = SkillCSharpId, SkillName = "C# Integration", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Skill { Id = SkillSqlId, SkillName = "SQL Integration", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        // Employees
        var hrEmployee = new Employee
        {
            Id = HrEmployeeId,
            EmpCode = HrEmpCode,
            FirstName = "HR",
            LastName = "Admin",
            Email = "hr.integration@test.com",
            Designation = "HR Manager",
            Role = EmployeeRole.HR,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pmEmployee = new Employee
        {
            Id = PmEmployeeId,
            EmpCode = PmEmpCode,
            FirstName = "PM",
            LastName = "Alice",
            Email = "pm.integration@test.com",
            Designation = "Project Manager",
            Role = EmployeeRole.ProjectManager,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var staffEmployee = new Employee
        {
            Id = StaffEmployeeId,
            EmpCode = StaffEmpCode,
            FirstName = "Staff",
            LastName = "Bob",
            Email = "staff.integration@test.com",
            Designation = "Developer",
            Role = EmployeeRole.Staff,
            ReportsToId = PmEmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Set<Employee>().AddRange(hrEmployee, pmEmployee, staffEmployee);

        // Account
        var account = new Account
        {
            Id = AccountId,
            AccountCode = AccountCode,
            AccountName = "Integration Test Account",
            AccountType = AccountType.Client,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Set<Account>().Add(account);

        // Project
        var project = new Project
        {
            Id = ProjectId,
            ProjectCode = ProjectCode,
            ProjectName = "Integration Test Project",
            AccountId = AccountId,
            ProjectManagerId = PmEmployeeId,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Set<Project>().Add(project);

        await db.SaveChangesAsync();
    }
}
