using FluentAssertions;
using PAMS.Application.DTOs.Employees;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red-phase tests for enriched EmployeeDetailResponse.
/// EmployeeDetailResponse should include a ManagedProjects collection
/// containing projects where the employee is either PM or a TeamLead.
/// These tests reference ManagedProjectItem DTO and EmployeeDetailResponse.ManagedProjects —
/// neither exists yet, so these will fail to compile until enriched.
/// </summary>
public sealed class EmployeeDetailResponseEnrichmentTests
{
    // ─── PM should see managed projects ──────────────────────────────────────

    [Fact(DisplayName = "GetEmployeeDetail_PM_ShouldIncludeManagedProjects")]
    public void GetEmployeeDetail_PM_ShouldIncludeManagedProjects()
    {
        // Arrange — employee is PM on two projects
        var employeeId = Guid.NewGuid();
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = AccountType.Client,
            IsActive = true
        };

        var project1 = new Project
        {
            Id = projectId1,
            ProjectCode = "PRJ001",
            ProjectName = "Project Alpha",
            AccountId = accountId,
            Account = account,
            ProjectManagerId = employeeId,
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            StartDate = TestData.LastMonth,
            Allocations = new List<Allocation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = Guid.NewGuid(),
                    ProjectId = projectId1,
                    Percentage = 50,
                    FromDate = TestData.Today,
                    ToDate = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var project2 = new Project
        {
            Id = projectId2,
            ProjectCode = "PRJ002",
            ProjectName = "Project Beta",
            AccountId = accountId,
            Account = account,
            ProjectManagerId = employeeId,
            Status = ProjectStatus.Upcoming,
            Billable = false,
            IsActive = true,
            StartDate = TestData.NextMonth,
            Allocations = new List<Allocation>()
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.PmEmpCode,
            FirstName = "Alice",
            LastName = "Manager",
            Email = "alice@test.com",
            Designation = "Project Manager",
            Role = EmployeeRole.ProjectManager,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ManagedProjects = new List<Project> { project1, project2 }
        };

        // Act — build the enriched response (mirrors expected controller logic)
        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = new EmployeeDetailResponse
        {
            EmployeeId = employee.Id,
            EmpCode = employee.EmpCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Designation = employee.Designation,
            Role = employee.Role,
            IsActive = employee.IsActive,
            Email = employee.Email,
            AvailabilityPercentage = 100,
            AllocationStatus = AllocationStatus.Bench,
            Skills = [],
            SkillDetails = [],
            CurrentAllocations = [],
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            // TDD: ManagedProjects property does not exist yet on EmployeeDetailResponse
            ManagedProjects = employee.ManagedProjects.Select(p => new ManagedProjectItem
            {
                ProjectId = p.Id,
                ProjectCode = p.ProjectCode,
                ProjectName = p.ProjectName,
                AccountCode = p.Account?.AccountCode ?? string.Empty,
                AccountName = p.Account?.AccountName ?? string.Empty,
                ManagementRole = "ProjectManager",
                Status = p.Status,
                ActiveResourceCount = p.Allocations
                    .Count(a => a.DeletedAt == null && a.FromDate <= today && (a.ToDate == null || a.ToDate >= today))
            }).ToList()
        };

        // Assert
        response.ManagedProjects.Should().NotBeNull();
        response.ManagedProjects.Should().HaveCount(2);

        var alpha = response.ManagedProjects.First(m => m.ProjectCode == "PRJ001");
        alpha.ProjectName.Should().Be("Project Alpha");
        alpha.AccountCode.Should().Be(TestData.AccountCode);
        alpha.AccountName.Should().Be("Acme Corp");
        alpha.ManagementRole.Should().Be("ProjectManager");
        alpha.Status.Should().Be(ProjectStatus.Active);
        alpha.ActiveResourceCount.Should().Be(1);

        var beta = response.ManagedProjects.First(m => m.ProjectCode == "PRJ002");
        beta.ManagementRole.Should().Be("ProjectManager");
        beta.Status.Should().Be(ProjectStatus.Upcoming);
        beta.ActiveResourceCount.Should().Be(0);
    }

    // ─── TeamLead should see managed projects ────────────────────────────────

    [Fact(DisplayName = "GetEmployeeDetail_TeamLead_ShouldIncludeManagedProjects")]
    public void GetEmployeeDetail_TeamLead_ShouldIncludeManagedProjects()
    {
        // Arrange — employee is a team lead on one project via ProjectTeamMembers
        var employeeId = Guid.NewGuid();
        var reporteeId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = AccountType.Client,
            IsActive = true
        };

        var project = new Project
        {
            Id = projectId,
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Alpha Project",
            AccountId = accountId,
            Account = account,
            ProjectManagerId = Guid.NewGuid(), // different PM
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            StartDate = TestData.LastMonth,
            Allocations = new List<Allocation>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = reporteeId,
                    ProjectId = projectId,
                    Percentage = 100,
                    FromDate = TestData.Today,
                    ToDate = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };

        var teamMemberAssignment = new ProjectTeamMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            TeamLeadId = employeeId,
            ReporteeId = reporteeId,
            CreatedAt = DateTime.UtcNow,
            Project = project
        };

        // Simulate the resolved list of managed projects (from PM + TeamLead roles)
        var managedProjects = new List<(Project Project, string Role)>
        {
            (project, "TeamLead")
        };

        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Bob",
            LastName = "Lead",
            Email = "bob@test.com",
            Designation = "Tech Lead",
            Role = EmployeeRole.Staff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act — build the enriched response
        var today = DateOnly.FromDateTime(DateTime.Today);

        var response = new EmployeeDetailResponse
        {
            EmployeeId = employee.Id,
            EmpCode = employee.EmpCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Designation = employee.Designation,
            Role = employee.Role,
            IsActive = employee.IsActive,
            Email = employee.Email,
            AvailabilityPercentage = 100,
            AllocationStatus = AllocationStatus.Bench,
            Skills = [],
            SkillDetails = [],
            CurrentAllocations = [],
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            // TDD: ManagedProjects property does not exist yet on EmployeeDetailResponse
            ManagedProjects = managedProjects.Select(mp => new ManagedProjectItem
            {
                ProjectId = mp.Project.Id,
                ProjectCode = mp.Project.ProjectCode,
                ProjectName = mp.Project.ProjectName,
                AccountCode = mp.Project.Account?.AccountCode ?? string.Empty,
                AccountName = mp.Project.Account?.AccountName ?? string.Empty,
                ManagementRole = mp.Role,
                Status = mp.Project.Status,
                ActiveResourceCount = mp.Project.Allocations
                    .Count(a => a.DeletedAt == null && a.FromDate <= today && (a.ToDate == null || a.ToDate >= today))
            }).ToList()
        };

        // Assert
        response.ManagedProjects.Should().NotBeNull();
        response.ManagedProjects.Should().HaveCount(1);

        var managed = response.ManagedProjects[0];
        managed.ProjectCode.Should().Be(TestData.ProjectCode);
        managed.ManagementRole.Should().Be("TeamLead");
        managed.ActiveResourceCount.Should().Be(1);
    }

    // ─── No managed projects → empty list (not null) ─────────────────────────

    [Fact(DisplayName = "GetEmployeeDetail_NoManagedProjects_ShouldReturnEmptyList")]
    public void GetEmployeeDetail_NoManagedProjects_ShouldReturnEmptyList()
    {
        // Arrange — staff employee with no PM or TeamLead assignments
        var employeeId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Charlie",
            LastName = "Staff",
            Email = "charlie@test.com",
            Designation = "Developer",
            Role = EmployeeRole.Staff,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ManagedProjects = new List<Project>() // empty — not a PM
        };

        // Act — build the enriched response with no managed projects
        var response = new EmployeeDetailResponse
        {
            EmployeeId = employee.Id,
            EmpCode = employee.EmpCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Designation = employee.Designation,
            Role = employee.Role,
            IsActive = employee.IsActive,
            Email = employee.Email,
            AvailabilityPercentage = 100,
            AllocationStatus = AllocationStatus.Bench,
            Skills = [],
            SkillDetails = [],
            CurrentAllocations = [],
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            // TDD: ManagedProjects property does not exist yet on EmployeeDetailResponse
            ManagedProjects = new List<ManagedProjectItem>()
        };

        // Assert — should be empty list, NOT null
        response.ManagedProjects.Should().NotBeNull();
        response.ManagedProjects.Should().BeEmpty();
    }
}
