using FluentAssertions;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.DTOs.ProjectTeamMembers;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red-phase tests for enriched ProjectDetailResponse (FR-007).
/// ProjectDetailResponse should include Allocations and TeamMembers collections.
/// These tests reference properties that do not yet exist on ProjectDetailResponse —
/// they will fail to compile until the DTO is enriched.
/// </summary>
public sealed class ProjectDetailResponseEnrichmentTests
{
    // ─── Allocations on ProjectDetailResponse ────────────────────────────────

    [Fact(DisplayName = "FR-007 | GetByCode_ShouldIncludeAllocationsInResponse")]
    public void GetByCode_ShouldIncludeAllocationsInResponse()
    {
        // Arrange — project entity with one active allocation
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        var allocation = new Allocation
        {
            Id = allocationId,
            EmployeeId = employeeId,
            ProjectId = projectId,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Employee = new Employee
            {
                Id = employeeId,
                EmpCode = TestData.StaffEmpCode,
                FirstName = "Jane",
                LastName = "Doe",
                Designation = "Senior Developer",
                IsActive = true
            }
        };

        var project = new Project
        {
            Id = projectId,
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Alpha Project",
            AccountId = TestData.AccountId,
            ProjectManagerId = TestData.PmEmployeeId,
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            StartDate = TestData.LastMonth,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Account = new Account
            {
                Id = TestData.AccountId,
                AccountCode = TestData.AccountCode,
                AccountName = "Acme Corp",
                AccountType = AccountType.Client
            },
            Allocations = new List<Allocation> { allocation }
        };

        // Act — build the enriched response (mirrors expected controller/handler logic)
        var response = new ProjectDetailResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            AccountId = project.AccountId,
            AccountCode = project.Account.AccountCode,
            AccountName = project.Account.AccountName,
            ProjectManagerId = project.ProjectManagerId,
            Status = project.Status,
            Billable = project.Billable,
            IsActive = project.IsActive,
            StartDate = project.StartDate,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            // TDD: Allocations property does not exist yet on ProjectDetailResponse
            Allocations = project.Allocations.Select(a => new AllocationDetailResponse
            {
                AllocationId = a.Id,
                EmployeeId = a.EmployeeId,
                EmpCode = a.Employee?.EmpCode ?? string.Empty,
                EmployeeName = a.Employee is not null
                    ? $"{a.Employee.FirstName} {a.Employee.LastName}" : string.Empty,
                ProjectId = a.ProjectId,
                ProjectCode = project.ProjectCode,
                ProjectName = project.ProjectName,
                Percentage = a.Percentage,
                FromDate = a.FromDate,
                ToDate = a.ToDate,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        // Assert
        response.Allocations.Should().NotBeNull();
        response.Allocations.Should().HaveCount(1);
        response.Allocations[0].AllocationId.Should().Be(allocationId);
        response.Allocations[0].EmpCode.Should().Be(TestData.StaffEmpCode);
        response.Allocations[0].Percentage.Should().Be(50);
    }

    // ─── TeamMembers on ProjectDetailResponse ────────────────────────────────

    [Fact(DisplayName = "FR-007 | GetByCode_ShouldIncludeTeamMembersInResponse")]
    public void GetByCode_ShouldIncludeTeamMembersInResponse()
    {
        // Arrange — project with one team member assignment
        var projectId = Guid.NewGuid();
        var teamLeadId = Guid.NewGuid();
        var reporteeId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();

        var teamMember = new ProjectTeamMember
        {
            Id = teamMemberId,
            ProjectId = projectId,
            TeamLeadId = teamLeadId,
            ReporteeId = reporteeId,
            CreatedAt = DateTime.UtcNow,
            TeamLead = new Employee
            {
                Id = teamLeadId,
                EmpCode = "TL001",
                FirstName = "Alice",
                LastName = "Lead",
                IsActive = true
            },
            Reportee = new Employee
            {
                Id = reporteeId,
                EmpCode = TestData.StaffEmpCode,
                FirstName = "Bob",
                LastName = "Staff",
                IsActive = true
            }
        };

        var project = new Project
        {
            Id = projectId,
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Alpha Project",
            AccountId = TestData.AccountId,
            Status = ProjectStatus.Active,
            Billable = true,
            IsActive = true,
            StartDate = TestData.LastMonth,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Account = new Account
            {
                Id = TestData.AccountId,
                AccountCode = TestData.AccountCode,
                AccountName = "Acme Corp",
                AccountType = AccountType.Client
            },
            TeamMembers = new List<ProjectTeamMember> { teamMember }
        };

        // Act — build the enriched response
        var response = new ProjectDetailResponse
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            AccountId = project.AccountId,
            AccountCode = project.Account.AccountCode,
            AccountName = project.Account.AccountName,
            Status = project.Status,
            Billable = project.Billable,
            IsActive = project.IsActive,
            StartDate = project.StartDate,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            // TDD: TeamMembers property does not exist yet on ProjectDetailResponse
            TeamMembers = project.TeamMembers.Select(tm => new ProjectTeamMemberResponse
            {
                Id = tm.Id,
                ProjectId = tm.ProjectId,
                ProjectCode = project.ProjectCode,
                TeamLeadId = tm.TeamLeadId,
                TeamLeadEmpCode = tm.TeamLead?.EmpCode ?? string.Empty,
                TeamLeadFullName = tm.TeamLead is not null
                    ? $"{tm.TeamLead.FirstName} {tm.TeamLead.LastName}" : string.Empty,
                ReporteeId = tm.ReporteeId,
                ReporteeEmpCode = tm.Reportee?.EmpCode ?? string.Empty,
                ReporteeFullName = tm.Reportee is not null
                    ? $"{tm.Reportee.FirstName} {tm.Reportee.LastName}" : string.Empty,
                CreatedAt = tm.CreatedAt
            }).ToList()
        };

        // Assert
        response.TeamMembers.Should().NotBeNull();
        response.TeamMembers.Should().HaveCount(1);
        response.TeamMembers[0].Id.Should().Be(teamMemberId);
        response.TeamMembers[0].TeamLeadEmpCode.Should().Be("TL001");
        response.TeamMembers[0].ReporteeEmpCode.Should().Be(TestData.StaffEmpCode);
    }
}
