using System.Reflection;
using FluentAssertions;

namespace PAMS.UnitTests.Application;

/// <summary>
/// TDD Red Phase — verifies that IAllocationRepository exposes methods
/// needed for the GET /allocations paginated list endpoint.
/// These tests will FAIL because the methods don't exist on the interface yet.
/// </summary>
public sealed class AllocationListEndpointTests
{
    [Fact(DisplayName = "Allocations List | IAllocationRepository_ShouldHaveGetFilteredAsyncMethod")]
    public void IAllocationRepository_ShouldHaveGetFilteredAsyncMethod()
    {
        // Arrange
        var repoType = typeof(PAMS.Domain.Repositories.IAllocationRepository);

        // Act
        var method = repoType.GetMethod("GetFilteredAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert — will FAIL: IAllocationRepository has no GetFilteredAsync method yet
        method.Should().NotBeNull(
            "IAllocationRepository should have a 'GetFilteredAsync' method " +
            "to support filtered/paginated allocation listing");
        method!.ReturnType.Should().BeAssignableTo(typeof(Task<>).MakeGenericType(
            typeof(IReadOnlyList<>).MakeGenericType(typeof(PAMS.Domain.Entities.Allocation))),
            "GetFilteredAsync should return Task<IReadOnlyList<Allocation>>");
    }

    [Fact(DisplayName = "Allocations List | IAllocationRepository_ShouldHaveGetFilteredCountAsyncMethod")]
    public void IAllocationRepository_ShouldHaveGetFilteredCountAsyncMethod()
    {
        // Arrange
        var repoType = typeof(PAMS.Domain.Repositories.IAllocationRepository);

        // Act
        var method = repoType.GetMethod("GetFilteredCountAsync",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert — will FAIL: IAllocationRepository has no GetFilteredCountAsync method yet
        method.Should().NotBeNull(
            "IAllocationRepository should have a 'GetFilteredCountAsync' method " +
            "to support paginated allocation count queries");
        method!.ReturnType.Should().Be(typeof(Task<int>),
            "GetFilteredCountAsync should return Task<int>");
    }
}
