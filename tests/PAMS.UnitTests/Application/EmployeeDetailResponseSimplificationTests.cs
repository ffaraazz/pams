using FluentAssertions;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies that EmployeeDetailResponse has been simplified
/// by removing nested collection properties (CurrentAllocations, ManagedProjects).
/// These tests will FAIL because the properties still exist on the DTO.
/// </summary>
public sealed class EmployeeDetailResponseSimplificationTests
{
    [Fact(DisplayName = "Simplification | EmployeeDetailResponse_ShouldNotHaveCurrentAllocationsProperty")]
    public void EmployeeDetailResponse_ShouldNotHaveCurrentAllocationsProperty()
    {
        // Arrange
        var type = typeof(PAMS.Application.DTOs.Employees.EmployeeDetailResponse);

        // Act
        var property = type.GetProperty("CurrentAllocations");

        // Assert — will FAIL: CurrentAllocations still exists on the DTO
        property.Should().BeNull(
            "EmployeeDetailResponse should not embed CurrentAllocations; " +
            "allocations should be fetched via a separate endpoint");
    }

    [Fact(DisplayName = "Simplification | EmployeeDetailResponse_ShouldNotHaveManagedProjectsProperty")]
    public void EmployeeDetailResponse_ShouldNotHaveManagedProjectsProperty()
    {
        // Arrange
        var type = typeof(PAMS.Application.DTOs.Employees.EmployeeDetailResponse);

        // Act
        var property = type.GetProperty("ManagedProjects");

        // Assert — will FAIL: ManagedProjects still exists on the DTO
        property.Should().BeNull(
            "EmployeeDetailResponse should not embed ManagedProjects; " +
            "managed projects should be fetched via a separate endpoint");
    }
}
