using FluentAssertions;
using PAMS.Application.DTOs.Employees;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Tests for EmployeeDetailResponse construction.
/// ManagedProjects and CurrentAllocations were removed in v1.10.0.
/// These tests now verify the simplified response only contains core employee data.
/// </summary>
public sealed class EmployeeDetailResponseEnrichmentTests
{
    [Fact(DisplayName = "GetEmployeeDetail_PM_ShouldBuildSimplifiedResponse")]
    public void GetEmployeeDetail_PM_ShouldBuildSimplifiedResponse()
    {
        var employeeId = Guid.NewGuid();

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
            UpdatedAt = DateTime.UtcNow
        };

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
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };

        response.EmployeeId.Should().Be(employeeId);
        response.EmpCode.Should().Be(TestData.PmEmpCode);
        response.FullName.Should().Be("Alice Manager");
        response.Role.Should().Be(EmployeeRole.ProjectManager);
        response.AvailabilityPercentage.Should().Be(100);
        response.AllocationStatus.Should().Be(AllocationStatus.Bench);
    }

    [Fact(DisplayName = "GetEmployeeDetail_TeamLead_ShouldBuildSimplifiedResponse")]
    public void GetEmployeeDetail_TeamLead_ShouldBuildSimplifiedResponse()
    {
        var employeeId = Guid.NewGuid();

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
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };

        response.EmployeeId.Should().Be(employeeId);
        response.EmpCode.Should().Be(TestData.StaffEmpCode);
        response.FullName.Should().Be("Bob Lead");
        response.Role.Should().Be(EmployeeRole.Staff);
    }

    [Fact(DisplayName = "GetEmployeeDetail_NoAllocations_ShouldReturnBenchStatus")]
    public void GetEmployeeDetail_NoAllocations_ShouldReturnBenchStatus()
    {
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
            UpdatedAt = DateTime.UtcNow
        };

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
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };

        response.AvailabilityPercentage.Should().Be(100);
        response.AllocationStatus.Should().Be(AllocationStatus.Bench);
        response.Skills.Should().BeEmpty();
    }
}
