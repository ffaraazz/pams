using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for AddProjectTeamMemberCommandValidator (FR-020).
/// Validates: self-assignment block, required fields, valid references.
/// Circular detection is handled by TeamLeadValidator in the handler, not the validator.
/// </summary>
public sealed class AddProjectTeamMemberCommandValidatorTests
{
    private PAMS.Application.Validators.AddProjectTeamMemberCommandValidator CreateSut()
        => new();

    // ─── FR-020 / Self-assignment: teamLead == reportee ─────────────────────

    [Fact(DisplayName = "FR-020 | Validate_SelfAssignment_ShouldHaveValidationError")]
    public async Task Validate_SelfAssignment_ShouldHaveValidationError()
    {
        // Arrange — same empCode for team lead and reportee
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "EMP010",
            ReporteeEmpCode = "EMP010" // self-assignment
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ReporteeEmpCode);
    }

    // ─── FR-020 / Required: projectCode ─────────────────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_EmptyProjectCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyProjectCode_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = "",
            TeamLeadEmpCode = "EMP010",
            ReporteeEmpCode = "EMP020"
        };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ProjectCode);
    }

    // ─── FR-020 / Required: teamLeadEmpCode ─────────────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_EmptyTeamLeadEmpCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyTeamLeadEmpCode_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "",
            ReporteeEmpCode = "EMP020"
        };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.TeamLeadEmpCode);
    }

    // ─── FR-020 / Required: reporteeEmpCode ─────────────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_EmptyReporteeEmpCode_ShouldHaveValidationError")]
    public async Task Validate_EmptyReporteeEmpCode_ShouldHaveValidationError()
    {
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "EMP010",
            ReporteeEmpCode = ""
        };

        var result = await validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ReporteeEmpCode);
    }

    // ─── FR-020 / Happy: Valid command ──────────────────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_ValidCommand_ShouldNotHaveAnyErrors")]
    public async Task Validate_ValidCommand_ShouldNotHaveAnyErrors()
    {
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "EMP010",
            ReporteeEmpCode = "EMP020"
        };

        var result = await validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── FR-020 / Edge: Case sensitivity on empCodes ────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_CaseInsensitiveSelfAssignment_ShouldHaveValidationError")]
    public async Task Validate_CaseInsensitiveSelfAssignment_ShouldHaveValidationError()
    {
        // Arrange — same empCode in different case
        var validator = CreateSut();
        var command = new PAMS.Application.Commands.ProjectTeamMembers.AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "emp010",
            ReporteeEmpCode = "EMP010" // same code, different case
        };

        // Act
        var result = await validator.TestValidateAsync(command);

        // Assert — should catch case-insensitive self-assignment
        result.ShouldHaveValidationErrorFor(c => c.ReporteeEmpCode);
    }
}
