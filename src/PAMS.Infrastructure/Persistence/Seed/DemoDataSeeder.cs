using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;

namespace PAMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds realistic demo data for development/testing.
/// This seeder is ONLY called in the Development environment.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(PamsDbContext context, ILogger logger)
    {
        if (await context.Accounts.AnyAsync())
        {
            logger.LogDebug("Demo data already seeded, skipping");
            return;
        }

        var now = DateTime.UtcNow;

        // ── Ensure employees exist (EmployeeSeeder runs before this) ──
        var emp001 = await context.Employees.FirstAsync(e => e.EmpCode == "EMP-001");
        var emp002 = await context.Employees.FirstAsync(e => e.EmpCode == "EMP-002");
        var emp003 = await context.Employees.FirstAsync(e => e.EmpCode == "EMP-003");

        // ── Additional employees ──────────────────────────────────────
        var emp004 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-004",
            FirstName = "David",
            LastName = "Chen",
            Email = "david.chen@pams.local",
            Designation = "Senior Developer",
            Role = EmployeeRole.Staff,
            ReportsToId = emp002.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp005 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-005",
            FirstName = "Sarah",
            LastName = "Johnson",
            Email = "sarah.johnson@pams.local",
            Designation = "Tech Lead",
            Role = EmployeeRole.Staff,
            ReportsToId = emp002.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp006 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-006",
            FirstName = "Mike",
            LastName = "Wilson",
            Email = "mike.wilson@pams.local",
            Designation = "Junior Developer",
            Role = EmployeeRole.Staff,
            ReportsToId = emp005.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp007 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-007",
            FirstName = "Emily",
            LastName = "Davis",
            Email = "emily.davis@pams.local",
            Designation = "QA Engineer",
            Role = EmployeeRole.Staff,
            ReportsToId = emp002.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp008 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-008",
            FirstName = "James",
            LastName = "Taylor",
            Email = "james.taylor@pams.local",
            Designation = "DevOps Engineer",
            Role = EmployeeRole.Staff,
            ReportsToId = emp001.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp009 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-009",
            FirstName = "Rachel",
            LastName = "Martinez",
            Email = "rachel.martinez@pams.local",
            Designation = "Project Manager",
            Role = EmployeeRole.ProjectManager,
            ReportsToId = emp001.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var emp010 = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-010",
            FirstName = "Tom",
            LastName = "Brown",
            Email = "tom.brown@pams.local",
            Designation = "Full Stack Developer",
            Role = EmployeeRole.Staff,
            ReportsToId = emp009.Id,
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Employees.AddRange(emp004, emp005, emp006, emp007, emp008, emp009, emp010);
        await context.SaveChangesAsync();

        // ── Employee skills ───────────────────────────────────────────
        var skills = await context.Skills.ToDictionaryAsync(s => s.SkillName, s => s.Id);

        var employeeSkills = new (Employee emp, string[] skillNames)[]
        {
            (emp001, ["Agile", "Scrum"]),
            (emp002, ["C#", ".NET", "Azure", "Agile"]),
            (emp003, ["C#", ".NET", "Java", "SQL", "PostgreSQL"]),
            (emp004, ["C#", ".NET", "SQL", "Docker"]),
            (emp005, ["C#", ".NET", "React", "TypeScript", "Azure"]),
            (emp006, ["JavaScript", "React", "TypeScript"]),
            (emp007, ["Python", "Agile", "Scrum"]),
            (emp008, ["Docker", "Kubernetes", "AWS", "CI/CD"]),
            (emp009, ["Python", "AWS", "Docker", "Agile", "CI/CD"]),
            (emp010, ["C#", ".NET", "Angular", "SQL"]),
        };

        foreach (var (emp, skillNames) in employeeSkills)
        {
            foreach (var name in skillNames)
            {
                if (skills.TryGetValue(name, out var skillId))
                {
                    context.EmployeeSkills.Add(new EmployeeSkill
                    {
                        EmployeeId = emp.Id,
                        SkillId = skillId
                    });
                }
            }
        }
        await context.SaveChangesAsync();

        // ── Accounts ──────────────────────────────────────────────────
        var accMsft = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = "ACC-MSFT",
            AccountName = "Microsoft",
            AccountType = AccountType.Client,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var accGoog = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = "ACC-GOOG",
            AccountName = "Google",
            AccountType = AccountType.Client,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var accStar = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = "ACC-STAR",
            AccountName = "Startup Alpha",
            AccountType = AccountType.Client,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var accBank = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = "ACC-BANK",
            AccountName = "Global Bank",
            AccountType = AccountType.Client,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var accIntl = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = "ACC-INTL",
            AccountName = "Internal Projects",
            AccountType = AccountType.Internal,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Accounts.AddRange(accMsft, accGoog, accStar, accBank, accIntl);
        await context.SaveChangesAsync();

        // ── Projects ──────────────────────────────────────────────────
        var prjAzureMig = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-AZURE-MIG",
            ProjectName = "Azure Cloud Migration",
            AccountId = accMsft.Id,
            ProjectManagerId = emp002.Id,
            StartDate = new DateOnly(2025, 1, 15),
            EndDate = new DateOnly(2026, 6, 30),
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjTeamsInt = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-TEAMS-INT",
            ProjectName = "Teams Integration",
            AccountId = accMsft.Id,
            ProjectManagerId = emp002.Id,
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 12, 31),
            Status = ProjectStatus.Completed,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjGcpApp = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-GCP-APP",
            ProjectName = "GCP App Modernization",
            AccountId = accGoog.Id,
            ProjectManagerId = emp009.Id,
            StartDate = new DateOnly(2025, 3, 1),
            EndDate = new DateOnly(2026, 3, 31),
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjAiPoc = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-AI-POC",
            ProjectName = "AI/ML Proof of Concept",
            AccountId = accGoog.Id,
            ProjectManagerId = emp009.Id,
            StartDate = new DateOnly(2025, 9, 1),
            EndDate = null,
            Status = ProjectStatus.Active,
            Billable = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjMvp = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-MVP",
            ProjectName = "MVP Development",
            AccountId = accStar.Id,
            ProjectManagerId = emp002.Id,
            StartDate = new DateOnly(2025, 4, 1),
            EndDate = new DateOnly(2025, 10, 31),
            Status = ProjectStatus.Completed,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjPaySys = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-PAY-SYS",
            ProjectName = "Payment System Revamp",
            AccountId = accBank.Id,
            ProjectManagerId = emp009.Id,
            StartDate = new DateOnly(2025, 7, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjFraud = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-FRAUD-DET",
            ProjectName = "Fraud Detection Engine",
            AccountId = accBank.Id,
            ProjectManagerId = emp009.Id,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 9, 30),
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var prjEcom = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PRJ-ECOM",
            ProjectName = "E-Commerce Platform",
            AccountId = accIntl.Id,
            ProjectManagerId = emp002.Id,
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2025, 3, 31),
            Status = ProjectStatus.Completed,
            Billable = false,
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Projects.AddRange(prjAzureMig, prjTeamsInt, prjGcpApp, prjAiPoc,
            prjMvp, prjPaySys, prjFraud, prjEcom);
        await context.SaveChangesAsync();

        // ── Allocations ───────────────────────────────────────────────
        var allocations = new Allocation[]
        {
            // David Chen: 50% Azure + 50% Teams = Full
            new() { Id = Guid.NewGuid(), EmployeeId = emp004.Id, ProjectId = prjAzureMig.Id,
                FromDate = new(2025, 6, 1), ToDate = new(2026, 6, 30), Percentage = 50,
                ProjectRole = "Senior Developer", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), EmployeeId = emp004.Id, ProjectId = prjTeamsInt.Id,
                FromDate = new(2025, 6, 1), ToDate = new(2025, 12, 31), Percentage = 50,
                ProjectRole = "Developer", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },

            // Sarah Johnson: 75% Azure + 25% Teams = Full
            new() { Id = Guid.NewGuid(), EmployeeId = emp005.Id, ProjectId = prjAzureMig.Id,
                FromDate = new(2025, 3, 1), ToDate = new(2026, 6, 30), Percentage = 75,
                ProjectRole = "Tech Lead", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), EmployeeId = emp005.Id, ProjectId = prjTeamsInt.Id,
                FromDate = new(2025, 8, 1), ToDate = new(2025, 12, 31), Percentage = 25,
                ProjectRole = "Architect", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },

            // Mike Wilson: 100% GCP = Full
            new() { Id = Guid.NewGuid(), EmployeeId = emp006.Id, ProjectId = prjGcpApp.Id,
                FromDate = new(2025, 4, 1), ToDate = new(2026, 3, 31), Percentage = 100,
                ProjectRole = "Frontend Developer", Billable = true,
                AllocatedById = emp009.Id, CreatedAt = now, UpdatedAt = now },

            // Emily Davis: 50% AI POC + 25% Fraud Det = Partial
            new() { Id = Guid.NewGuid(), EmployeeId = emp007.Id, ProjectId = prjAiPoc.Id,
                FromDate = new(2025, 9, 1), ToDate = null, Percentage = 50,
                ProjectRole = "QA Lead", Billable = false,
                AllocatedById = emp009.Id, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), EmployeeId = emp007.Id, ProjectId = prjFraud.Id,
                FromDate = new(2026, 1, 1), ToDate = new(2026, 9, 30), Percentage = 25,
                ProjectRole = "QA Engineer", Billable = true,
                AllocatedById = emp009.Id, CreatedAt = now, UpdatedAt = now },

            // James Taylor: 25% Azure + 25% Payment = Partial (50% available)
            new() { Id = Guid.NewGuid(), EmployeeId = emp008.Id, ProjectId = prjAzureMig.Id,
                FromDate = new(2025, 6, 1), ToDate = new(2026, 6, 30), Percentage = 25,
                ProjectRole = "DevOps Engineer", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), EmployeeId = emp008.Id, ProjectId = prjPaySys.Id,
                FromDate = new(2025, 8, 1), ToDate = new(2026, 12, 31), Percentage = 25,
                ProjectRole = "DevOps Engineer", Billable = true,
                AllocatedById = emp009.Id, CreatedAt = now, UpdatedAt = now },

            // Bob Staff: 100% MVP (ended) + 50% Payment System (current)
            new() { Id = Guid.NewGuid(), EmployeeId = emp003.Id, ProjectId = prjMvp.Id,
                FromDate = new(2025, 4, 1), ToDate = new(2025, 10, 31), Percentage = 100,
                ProjectRole = "Backend Developer", Billable = true,
                AllocatedById = emp002.Id, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), EmployeeId = emp003.Id, ProjectId = prjPaySys.Id,
                FromDate = new(2026, 1, 15), ToDate = new(2026, 12, 31), Percentage = 50,
                ProjectRole = "Developer", Billable = true,
                AllocatedById = emp009.Id, CreatedAt = now, UpdatedAt = now },

            // Tom Brown (inactive): E-Commerce (soft-deleted)
            new() { Id = Guid.NewGuid(), EmployeeId = emp010.Id, ProjectId = prjEcom.Id,
                FromDate = new(2024, 6, 1), ToDate = new(2025, 3, 31), Percentage = 100,
                ProjectRole = "Full Stack Developer", Billable = false,
                AllocatedById = emp002.Id, DeletedAt = now, CreatedAt = now, UpdatedAt = now },
        };

        context.Allocations.AddRange(allocations);
        await context.SaveChangesAsync();

        // ── Project team members ──────────────────────────────────────
        var teamMembers = new ProjectTeamMember[]
        {
            // Azure Migration: Sarah leads David and James
            new() { Id = Guid.NewGuid(), ProjectId = prjAzureMig.Id,
                TeamLeadId = emp005.Id, ReporteeId = emp004.Id, CreatedAt = now },
            new() { Id = Guid.NewGuid(), ProjectId = prjAzureMig.Id,
                TeamLeadId = emp005.Id, ReporteeId = emp008.Id, CreatedAt = now },
            // Teams Integration: Sarah leads David
            new() { Id = Guid.NewGuid(), ProjectId = prjTeamsInt.Id,
                TeamLeadId = emp005.Id, ReporteeId = emp004.Id, CreatedAt = now },
        };

        context.ProjectTeamMembers.AddRange(teamMembers);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded demo data: {Employees} employees, {Accounts} accounts, {Projects} projects, " +
            "{Allocations} allocations, {TeamMembers} team members",
            7, 5, 8, allocations.Length, teamMembers.Length);
    }
}
