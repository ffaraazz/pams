using FluentAssertions;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies that ProjectSummaryResponse and ProjectDetailResponse
/// include a ResourceCount property (count of active allocations).
/// These tests will FAIL because the property doesn't exist yet on either DTO.
/// </summary>
public sealed class ProjectResourceCountTests
{
    [Fact(DisplayName = "ResourceCount | ProjectSummaryResponse_ShouldHaveResourceCountProperty")]
    public void ProjectSummaryResponse_ShouldHaveResourceCountProperty()
    {
        // Arrange
        var type = typeof(PAMS.Application.DTOs.Projects.ProjectSummaryResponse);

        // Act
        var property = type.GetProperty("ResourceCount");

        // Assert — will FAIL: ProjectSummaryResponse has no ResourceCount yet
        property.Should().NotBeNull(
            "ProjectSummaryResponse should have a 'ResourceCount' property " +
            "indicating the number of active allocations on the project");
        property!.PropertyType.Should().Be(typeof(int),
            "ResourceCount should be an int");
    }

    [Fact(DisplayName = "ResourceCount | ProjectDetailResponse_ShouldHaveResourceCountProperty")]
    public void ProjectDetailResponse_ShouldHaveResourceCountProperty()
    {
        // Arrange
        var type = typeof(PAMS.Application.DTOs.Projects.ProjectDetailResponse);

        // Act
        var property = type.GetProperty("ResourceCount");

        // Assert — will FAIL: ProjectDetailResponse has no ResourceCount yet
        property.Should().NotBeNull(
            "ProjectDetailResponse should have a 'ResourceCount' property " +
            "indicating the number of active allocations on the project");
        property!.PropertyType.Should().Be(typeof(int),
            "ResourceCount should be an int");
    }
}
