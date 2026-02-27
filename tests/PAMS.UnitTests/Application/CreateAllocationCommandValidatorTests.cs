using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateAllocationCommandValidator (FR-010).
/// Validates: percentage min/max/increment, date constraints.
/// Uses FluentValidation's TestValidate extension.
/// </summary>
public sealed class CreateAllocationCommandValidatorTests
{
    // Default system config: min=25, increment=5, max=100
    private readonly PAMS.Domain.Repositories.ISystemConfigRepository _configRepo = Substitute.For<PAMS.Domain.Repositories.ISystemConfigRepository>();

    private PAMS.Application.Validators.CreateAllocationCommandValidator CreateSut()
    {
        var config = Substitute.For<PAMS.Domain.Entities.SystemConfig>();
        config.MinAllocationPercentage.Returns(25);
        config.AllocationIncrement.Returns(5);

        _configRepo.GetAsync(Arg.Any<CancellationToken>())
            .Returns(config);

        return new PAMS.Application.Validators.CreateAllocationCommandValidator(_configRepo);
    }

    // ─── FR-010 / AC-010-1: fromDate required ───────────────────────────────

    [Fact(DisplayName = "FR-010 | Validate_MissingFromDate_ShouldHaveValidationError")]
    public async Task Validate_MissingFromDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = default, // missing
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.FromDate);
    }

    // ─── FR-010 / AC-010-2: toDate < fromDate → error ──────────────────────

    [Fact(DisplayName = "FR-010 | Validate_ToDateBeforeFromDate_ShouldHaveValidationError")]
    public async Task Validate_ToDateBeforeFromDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Tomorrow,
            ToDate = TestData.Today // before fromDate
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ToDate);
    }

    // ─── FR-010 / AC-010-2: toDate == fromDate → allowed (single day) ──────

    [Fact(DisplayName = "FR-010 | Validate_SingleDayAllocation_ShouldNotHaveValidationError")]
    public async Task Validate_SingleDayAllocation_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 25,
            FromDate = TestData.Today,
            ToDate = TestData.Today // same day — single day allocation
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.ToDate);
    }

    // ─── FR-010 / AC-010-3: percentage below minimum ────────────────────────

    [Theory(DisplayName = "FR-010 | Validate_PercentageBelowMinimum_ShouldHaveValidationError")]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(24)]
    public async Task Validate_PercentageBelowMinimum_ShouldHaveValidationError(int pct)
    {
        // Arrange — min is 25%
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = pct,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Percentage);
    }

    // ─── FR-010 / AC-010-3: percentage above 100 ────────────────────────────

    [Fact(DisplayName = "FR-010 | Validate_PercentageAbove100_ShouldHaveValidationError")]
    public async Task Validate_PercentageAbove100_ShouldHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 105,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Percentage);
    }

    // ─── FR-010 / AC-010-3: percentage not multiple of increment ─────────────

    [Theory(DisplayName = "FR-010 | Validate_PercentageNotMultipleOfIncrement_ShouldHaveValidationError")]
    [InlineData(27)] // not multiple of 5
    [InlineData(33)]
    [InlineData(51)]
    [InlineData(99)]
    public async Task Validate_PercentageNotMultipleOfIncrement_ShouldHaveValidationError(int pct)
    {
        // Arrange — increment is 5
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = pct,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Percentage);
    }

    // ─── FR-010 / Happy: Valid percentage values ─────────────────────────────

    [Theory(DisplayName = "FR-010 | Validate_ValidPercentage_ShouldNotHaveValidationError")]
    [InlineData(25)]
    [InlineData(30)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public async Task Validate_ValidPercentage_ShouldNotHaveValidationError(int pct)
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = pct,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.Percentage);
    }

    // ─── FR-010 / Edge: Empty projectCode ───────────────────────────────────

    [Fact(DisplayName = "FR-010 | Validate_EmptyProjectCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyProjectCode_ShouldHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = "",
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ProjectCode);
    }

    // ─── FR-010 / Edge: Empty empCode ───────────────────────────────────────

    [Fact(DisplayName = "FR-010 | Validate_EmptyEmpCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyEmpCode_ShouldHaveValidationError()
    {
        // Arrange
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = "",
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.EmpCode);
    }
}
