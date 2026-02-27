using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateEmployeeCommandValidator (FR-007).
/// Validates: empCode uniqueness, email format/uniqueness, name length,
/// role enum, designation required, reportsTo validity.
/// </summary>
public sealed class CreateEmployeeCommandValidatorTests
{
    private readonly PAMS.Domain.Repositories.IEmployeeRepository _employeeRepo = Substitute.For<PAMS.Domain.Repositories.IEmployeeRepository>();

    private PAMS.Application.Validators.CreateEmployeeCommandValidator CreateSut()
        => new(_employeeRepo);

    // ─── FR-007 / AC-007-1: empCode uniqueness ──────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_DuplicateEmpCode_ShouldHaveValidationError")]
    public async Task Validate_DuplicateEmpCode_ShouldHaveValidationError()
    {
        // Arrange — empCode already exists
        _employeeRepo.ExistsByEmpCodeAsync("EMP001", Arg.Any<CancellationToken>())
            .Returns(true);

        var validator = CreateSut();
        var command = CreateValidCommand() with { EmpCode = "EMP001" };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.EmpCode)
            .WithErrorCode("ERR_EMPCODE_EXISTS");
    }

    // ─── FR-007 / AC-007-1: empCode required ────────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_EmptyEmpCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyEmpCode_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { EmpCode = "" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.EmpCode);
    }

    // ─── FR-007 / AC-007-2: First name required ─────────────────────────────

    [Theory(DisplayName = "FR-007 | Validate_MissingFirstName_ShouldHaveValidationError")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_MissingFirstName_ShouldHaveValidationError(string? firstName)
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { FirstName = firstName! };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    // ─── FR-007 / AC-007-2: First name max 100 characters ───────────────────

    [Fact(DisplayName = "FR-007 | Validate_FirstNameTooLong_ShouldHaveValidationError")]
    public async Task Validate_FirstNameTooLong_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { FirstName = new string('A', 101) };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.FirstName);
    }

    // ─── FR-007 / AC-007-2: Last name required ──────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_MissingLastName_ShouldHaveValidationError")]
    public async Task Validate_MissingLastName_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { LastName = "" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    // ─── FR-007 / AC-007-2: Last name max 100 characters ────────────────────

    [Fact(DisplayName = "FR-007 | Validate_LastNameTooLong_ShouldHaveValidationError")]
    public async Task Validate_LastNameTooLong_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { LastName = new string('Z', 101) };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    // ─── FR-007 / AC-007-3: Email required ──────────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_MissingEmail_ShouldHaveValidationError")]
    public async Task Validate_MissingEmail_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Email = "" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    // ─── FR-007 / AC-007-3: Email invalid format ────────────────────────────

    [Theory(DisplayName = "FR-007 | Validate_InvalidEmailFormat_ShouldHaveValidationError")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@missing.com")]
    [InlineData("spaces in@mail.com")]
    public async Task Validate_InvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Email = email };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    // ─── FR-007 / AC-007-3: Email must be unique ────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_DuplicateEmail_ShouldHaveValidationError")]
    public async Task Validate_DuplicateEmail_ShouldHaveValidationError()
    {
        _employeeRepo.ExistsByEmailAsync("existing@company.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var validator = CreateSut();
        var command = CreateValidCommand() with { Email = "existing@company.com" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    // ─── FR-007 / AC-007-4: Invalid role ────────────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_InvalidRole_ShouldHaveValidationError")]
    public async Task Validate_InvalidRole_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Role = (PAMS.Domain.Enums.EmployeeRole)999 };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Role);
    }

    // ─── FR-007 / AC-007-4: Valid roles ─────────────────────────────────────

    [Theory(DisplayName = "FR-007 | Validate_ValidRole_ShouldNotHaveValidationError")]
    [InlineData(PAMS.Domain.Enums.EmployeeRole.HR)]
    [InlineData(PAMS.Domain.Enums.EmployeeRole.ProjectManager)]
    [InlineData(PAMS.Domain.Enums.EmployeeRole.Staff)]
    public async Task Validate_ValidRole_ShouldNotHaveValidationError(PAMS.Domain.Enums.EmployeeRole role)
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Role = role };

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Role);
    }

    // ─── FR-007 / AC-007-5: Designation required ────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_MissingDesignation_ShouldHaveValidationError")]
    public async Task Validate_MissingDesignation_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Designation = "" };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Designation);
    }

    // ─── FR-007 / AC-007-5: Designation max 150 chars ───────────────────────

    [Fact(DisplayName = "FR-007 | Validate_DesignationTooLong_ShouldHaveValidationError")]
    public async Task Validate_DesignationTooLong_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = CreateValidCommand() with { Designation = new string('X', 151) };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Designation);
    }

    // ─── FR-007 / Happy: Fully valid command ────────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_ValidCommand_ShouldNotHaveAnyErrors")]
    public async Task Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var validator = CreateSut();
        var command = CreateValidCommand();

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static PAMS.Application.Commands.Employees.CreateEmployeeCommand CreateValidCommand()
    {
        return new PAMS.Application.Commands.Employees.CreateEmployeeCommand
        {
            EmpCode = "EMP999",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@company.com",
            Role = PAMS.Domain.Enums.EmployeeRole.Staff,
            Designation = "Software Engineer",
            ReportsToEmpCode = null,
            SkillIds = new List<Guid>()
        };
    }
}
