using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Persistence.Seed;

public static class EmployeeSeeder
{
    public static async Task SeedAsync(PamsDbContext context, ILogger logger)
    {
        if (await context.Employees.AnyAsync())
        {
            logger.LogDebug("Employees already seeded, skipping");
            return;
        }

        var now = DateTime.UtcNow;

        var hrAdmin = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-001",
            FirstName = "Priya",
            LastName = "Sharma",
            Email = "priya.sharma@pams.local",
            Designation = "HR Manager",
            Role = EmployeeRole.HR,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var pmAlice = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-002",
            FirstName = "Alice",
            LastName = "Morgan",
            Email = "alice.morgan@pams.local",
            Designation = "Project Manager",
            Role = EmployeeRole.ProjectManager,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var staffBob = new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = "EMP-003",
            FirstName = "Bob",
            LastName = "Reynolds",
            Email = "bob.reynolds@pams.local",
            Designation = "Software Engineer",
            Role = EmployeeRole.Staff,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Employees.AddRange(hrAdmin, pmAlice, staffBob);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} test employees (EMP-001, EMP-002, EMP-003)", 3);
    }
}
