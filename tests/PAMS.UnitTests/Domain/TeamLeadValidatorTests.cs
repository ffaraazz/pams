using FluentAssertions;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Domain;

/// <summary>
/// Unit tests for TeamLeadValidator — circular project-scoped reporting detection (FR-020).
/// Domain rule: if A leads B on project P, then B cannot lead A on the same project P.
/// Self-assignment (teamLead == reportee) is blocked at DB level (check constraint)
/// but also validated in domain.
/// Cross-project assignments are allowed: A leads B on Proj1, B leads A on Proj2.
/// Tests are designed to fail-first (TDD).
/// </summary>
public sealed class TeamLeadValidatorTests
{
    // ─── FR-020 / AC-020-6: Circular on same project blocked ─────────────────

    [Fact(DisplayName = "FR-020 | Validate_CircularOnSameProject_ShouldThrowCircularReportingException")]
    public void Validate_CircularOnSameProject_ShouldThrowCircularReportingException()
    {
        // Arrange — A leads B on ProjectX, now trying B leads A on ProjectX
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        // Existing mapping: A is team lead of B on this project
        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>
        {
            (empA, empB)
        };

        // Act — trying to add B as team lead of A on the same project
        var act = () => sut.Validate(projectId, teamLeadId: empB, reporteeId: empA, existingMappings);

        // Assert — circular detected
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-020 / AC-020-6: Self-assignment blocked ──────────────────────────

    [Fact(DisplayName = "FR-020 | Validate_SelfAssignment_ShouldThrowCircularReportingException")]
    public void Validate_SelfAssignment_ShouldThrowCircularReportingException()
    {
        // Arrange — trying to assign employee as their own team lead
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>();

        // Act — A leads A on the same project
        var act = () => sut.Validate(projectId, teamLeadId: empA, reporteeId: empA, existingMappings);

        // Assert
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-020 / AC-020-5: Cross-project assignments allowed ────────────────

    [Fact(DisplayName = "FR-020 | Validate_CrossProjectReverseAssignment_ShouldNotThrow")]
    public void Validate_CrossProjectReverseAssignment_ShouldNotThrow()
    {
        // Arrange — A leads B on Proj1 (existingMappings from Proj1)
        // Now assigning B leads A on Proj2 → different project → allowed
        // The validator receives ONLY mappings for the target project (Proj2)
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var proj2Id = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        // Proj2 has no existing mappings between A and B
        var existingMappingsForProj2 = new List<(Guid TeamLeadId, Guid ReporteeId)>();

        // Act — B leads A on Proj2 (A leads B on Proj1 is irrelevant)
        var act = () => sut.Validate(proj2Id, teamLeadId: empB, reporteeId: empA, existingMappingsForProj2);

        // Assert — allowed since it's a different project
        act.Should().NotThrow();
    }

    // ─── FR-020 / Happy: Valid assignment (no conflict) ──────────────────────

    [Fact(DisplayName = "FR-020 | Validate_ValidNewAssignment_ShouldNotThrow")]
    public void Validate_ValidNewAssignment_ShouldNotThrow()
    {
        // Arrange — A leads B already; now adding A leads C → no conflict
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>
        {
            (empA, empB)
        };

        // Act — A leads C on the same project, no circular
        var act = () => sut.Validate(projectId, teamLeadId: empA, reporteeId: empC, existingMappings);

        // Assert
        act.Should().NotThrow();
    }

    // ─── FR-020 / AC-020-4: Team lead on multiple projects ───────────────────

    [Fact(DisplayName = "FR-020 | Validate_TeamLeadMultipleReportees_ShouldNotThrow")]
    public void Validate_TeamLeadMultipleReportees_ShouldNotThrow()
    {
        // Arrange — A leads B, A leads C, now adding A leads D
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();
        var empD = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>
        {
            (empA, empB),
            (empA, empC)
        };

        // Act — A leads D on the same project
        var act = () => sut.Validate(projectId, teamLeadId: empA, reporteeId: empD, existingMappings);

        // Assert
        act.Should().NotThrow();
    }

    // ─── FR-020 / Edge: Duplicate assignment (already exists) ────────────────

    [Fact(DisplayName = "FR-020 | Validate_DuplicateAssignment_ShouldThrowDomainException")]
    public void Validate_DuplicateAssignment_ShouldThrowDomainException()
    {
        // Arrange — A leads B already exists, trying to add again
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>
        {
            (empA, empB)
        };

        // Act — trying to add the same mapping again
        var act = () => sut.Validate(projectId, teamLeadId: empA, reporteeId: empB, existingMappings);

        // Assert — duplicate blocked
        act.Should().Throw<PAMS.Domain.Exceptions.DomainException>();
    }

    // ─── FR-020 / Edge: Transitive circular (A→B, B→C, now C→A) ─────────────

    [Fact(DisplayName = "FR-020 | Validate_TransitiveCircularOnSameProject_ShouldThrowCircularReportingException")]
    public void Validate_TransitiveCircularOnSameProject_ShouldThrowCircularReportingException()
    {
        // Arrange — A leads B, B leads C, now trying C leads A on same project
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>
        {
            (empA, empB),
            (empB, empC)
        };

        // Act — C leads A → creates A → B → C → A cycle
        var act = () => sut.Validate(projectId, teamLeadId: empC, reporteeId: empA, existingMappings);

        // Assert — transitive circular detected
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-020 / Happy: Empty project (first assignment) ────────────────────

    [Fact(DisplayName = "FR-020 | Validate_FirstAssignmentOnProject_ShouldNotThrow")]
    public void Validate_FirstAssignmentOnProject_ShouldNotThrow()
    {
        // Arrange — no existing mappings for this project
        var sut = new PAMS.Domain.Services.TeamLeadValidator();
        var projectId = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();

        var existingMappings = new List<(Guid TeamLeadId, Guid ReporteeId)>();

        // Act — first ever assignment
        var act = () => sut.Validate(projectId, teamLeadId: empA, reporteeId: empB, existingMappings);

        // Assert
        act.Should().NotThrow();
    }
}
