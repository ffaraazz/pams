using FluentAssertions;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Domain;

/// <summary>
/// Unit tests for AllocationCapacityService — the core capacity engine (FR-011).
/// Tests cover: happy path, boundary cases, edge cases, and failure paths.
/// All tests are designed to fail-first (TDD author mode) until implementation exists.
/// </summary>
public sealed class AllocationCapacityServiceTests
{
    // ─── FR-011 / AC-011-1: Sum all active allocations for overlapping days ────

    [Fact(DisplayName = "FR-011 | Validate_WhenNoExistingAllocations_ShouldNotThrow")]
    public void Validate_WhenNoExistingAllocations_ShouldNotThrow()
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 0;
        var requestedPercentage = 50;
        var fromDate = TestData.Today;
        DateOnly? toDate = TestData.NextMonth;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, fromDate, toDate);

        // Assert — employee with zero allocations should pass capacity check
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "FR-011 | Validate_WhenExactly100Percent_ShouldNotThrow")]
    public void Validate_WhenExactly100Percent_ShouldNotThrow()
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 75;
        var requestedPercentage = 25;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, TestData.NextMonth);

        // Assert — 75 + 25 = 100 → exactly at limit, should NOT throw
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "FR-011 | Validate_WhenCapacityExceeded_ShouldThrowCapacityExceededException")]
    public void Validate_WhenCapacityExceeded_ShouldThrowCapacityExceededException()
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 75;
        var requestedPercentage = 50;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, TestData.NextMonth);

        // Assert — 75 + 50 = 125 > 100 → must throw CapacityExceededException
        act.Should().Throw<PAMS.Domain.Exceptions.CapacityExceededException>();
    }

    // ─── FR-011 / AC-011-3: Available capacity = 100 - max daily sum ──────────

    [Theory(DisplayName = "FR-011 | Validate_VariousExistingTotals_ShouldEnforceCapacityCorrectly")]
    [InlineData(0, 100, false)]     // 0 + 100 = 100 → OK
    [InlineData(0, 101, true)]      // 0 + 101 > 100 → exceeds (percentage > 100 is invalid anyway)
    [InlineData(50, 50, false)]     // 50 + 50 = 100 → OK
    [InlineData(50, 51, true)]      // 50 + 51 = 101 → exceeds
    [InlineData(99, 1, false)]      // 99 + 1 = 100 → OK
    [InlineData(99, 2, true)]       // 99 + 2 = 101 → exceeds
    [InlineData(100, 1, true)]      // 100 + 1 = 101 → fully allocated, any new allocation fails
    [InlineData(25, 25, false)]     // 25 + 25 = 50 → OK
    public void Validate_VariousExistingTotals_ShouldEnforceCapacityCorrectly(
        int existingTotal, int requestedPercentage, bool shouldThrow)
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationCapacityService();

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, TestData.NextMonth);

        // Assert
        if (shouldThrow)
            act.Should().Throw<PAMS.Domain.Exceptions.CapacityExceededException>();
        else
            act.Should().NotThrow();
    }

    // ─── FR-011 / Edge: Single-day allocation ─────────────────────────────────

    [Fact(DisplayName = "FR-011 | Validate_SingleDayAllocation_ShouldCheckCapacityForThatDay")]
    public void Validate_SingleDayAllocation_ShouldCheckCapacityForThatDay()
    {
        // Arrange — single day (fromDate == toDate)
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 80;
        var requestedPercentage = 25;
        var singleDay = TestData.Today;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, singleDay, singleDay);

        // Assert — 80 + 25 = 105 > 100 → should throw even for a single day
        act.Should().Throw<PAMS.Domain.Exceptions.CapacityExceededException>();
    }

    // ─── FR-011 / Edge: Open-ended existing allocation ────────────────────────

    [Fact(DisplayName = "FR-011 | Validate_OpenEndedExisting_ShouldAssumeIndefiniteOverlap")]
    public void Validate_OpenEndedExisting_ShouldAssumeIndefiniteOverlap()
    {
        // Arrange — existing open-ended allocation uses full overlap
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 60;  // open-ended 60% overlaps indefinitely
        var requestedPercentage = 50;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, null);

        // Assert — 60 + 50 = 110 > 100 → must throw
        act.Should().Throw<PAMS.Domain.Exceptions.CapacityExceededException>();
    }

    // ─── FR-011 / Edge: Zero existing, full 100% new allocation ───────────────

    [Fact(DisplayName = "FR-011 | Validate_WhenBenchEmployee_100PercentAllocation_ShouldSucceed")]
    public void Validate_WhenBenchEmployee_100PercentAllocation_ShouldSucceed()
    {
        // Arrange — employee on bench (0% allocated), requesting full 100%
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 0;
        var requestedPercentage = 100;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, TestData.NextMonth);

        // Assert — 0 + 100 = 100 → exactly at limit, should succeed
        act.Should().NotThrow();
    }

    // ─── FR-011 / AC-011-5: Performance concern (tested indirectly) ───────────

    [Fact(DisplayName = "FR-011 | Validate_WithExistingTotal_ShouldBeInstantaneous")]
    public void Validate_WithExistingTotal_ShouldBeInstantaneous()
    {
        // Arrange — this is a pure function; should be sub-millisecond
        var sut = new PAMS.Domain.Services.AllocationCapacityService();

        // Act & Assert — run 10000 iterations to confirm no performance regression
        var act = () =>
        {
            for (var i = 0; i < 10_000; i++)
                sut.Validate(50, 25, TestData.Today, TestData.NextMonth);
        };

        act.ExecutionTime().Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    // ─── FR-011 / Negative: Capacity exception should contain useful details ──

    [Fact(DisplayName = "FR-011 | Validate_WhenExceeded_ExceptionShouldContainCapacityDetails")]
    public void Validate_WhenExceeded_ExceptionShouldContainCapacityDetails()
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationCapacityService();
        var existingTotal = 80;
        var requestedPercentage = 30;

        // Act
        var act = () => sut.Validate(existingTotal, requestedPercentage, TestData.Today, TestData.NextMonth);

        // Assert — exception must communicate what happened for ProblemDetails
        act.Should().Throw<PAMS.Domain.Exceptions.CapacityExceededException>()
            .Which.Message.Should().MatchRegex("80|30|20"); // available = 100 - 80 = 20
    }
}
