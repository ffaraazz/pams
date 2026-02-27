using FluentAssertions;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Domain;

/// <summary>
/// Unit tests for AllocationStopService — stop date computation (FR-013).
/// Domain rule:
///   if allocation.fromDate > today → stopDate = today (cancel before start)
///   else                           → stopDate = today + 1 day (release from tomorrow)
/// Tests are designed to fail-first (TDD) until the domain service is implemented.
/// </summary>
public sealed class AllocationStopServiceTests
{
    // ─── FR-013 / AC-013-1: fromDate > today → toDate = today ─────────────────

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenAllocationNotYetStarted_ShouldReturnToday")]
    public void ComputeStopDate_WhenAllocationNotYetStarted_ShouldReturnToday()
    {
        // Arrange — allocation starts in the future
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;
        var futureFromDate = TestData.Tomorrow; // starts tomorrow, hasn't begun

        // Act
        var stopDate = sut.ComputeStopDate(futureFromDate, today);

        // Assert — cancel before it starts: stopDate = today
        stopDate.Should().Be(today);
    }

    // ─── FR-013 / AC-013-2: fromDate <= today and toDate NULL or > tomorrow → tomorrow ─

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenAllocationAlreadyStarted_ShouldReturnTomorrow")]
    public void ComputeStopDate_WhenAllocationAlreadyStarted_ShouldReturnTomorrow()
    {
        // Arrange — allocation already started (fromDate in the past or today)
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;
        var pastFromDate = TestData.LastMonth; // started last month

        // Act
        var stopDate = sut.ComputeStopDate(pastFromDate, today);

        // Assert — release from tomorrow
        stopDate.Should().Be(today.AddDays(1));
    }

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenStartedToday_ShouldReturnTomorrow")]
    public void ComputeStopDate_WhenStartedToday_ShouldReturnTomorrow()
    {
        // Arrange — allocation starts today (fromDate == today)
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;

        // Act
        var stopDate = sut.ComputeStopDate(today, today);

        // Assert — started today → release from tomorrow
        stopDate.Should().Be(today.AddDays(1));
    }

    // ─── FR-013 / Edge: fromDate is far in the future ─────────────────────────

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenAllocationStartsFarInFuture_ShouldReturnToday")]
    public void ComputeStopDate_WhenAllocationStartsFarInFuture_ShouldReturnToday()
    {
        // Arrange — allocation starts months from now
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;
        var farFuture = today.AddMonths(6);

        // Act
        var stopDate = sut.ComputeStopDate(farFuture, today);

        // Assert — hasn't started, cancel = today
        stopDate.Should().Be(today);
    }

    // ─── FR-013 / Edge: fromDate is yesterday ─────────────────────────────────

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenStartedYesterday_ShouldReturnTomorrow")]
    public void ComputeStopDate_WhenStartedYesterday_ShouldReturnTomorrow()
    {
        // Arrange
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;
        var yesterday = TestData.Yesterday;

        // Act
        var stopDate = sut.ComputeStopDate(yesterday, today);

        // Assert — already active, release from tomorrow
        stopDate.Should().Be(today.AddDays(1));
    }

    // ─── FR-013 / Boundary: fromDate is exactly tomorrow (edge of "not started") ─

    [Fact(DisplayName = "FR-013 | ComputeStopDate_WhenFromDateIsTomorrow_ShouldReturnToday")]
    public void ComputeStopDate_WhenFromDateIsTomorrow_ShouldReturnToday()
    {
        // Arrange — fromDate > today by exactly 1 day
        var sut = new PAMS.Domain.Services.AllocationStopService();
        var today = TestData.Today;
        var tomorrow = TestData.Tomorrow;

        // Act
        var stopDate = sut.ComputeStopDate(tomorrow, today);

        // Assert — tomorrow > today → hasn't started → stopDate = today
        stopDate.Should().Be(today);
    }
}
