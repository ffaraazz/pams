using FluentAssertions;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Domain;

/// <summary>
/// Unit tests for ReportingChainValidator — circular reporting detection (FR-007).
/// Domain rule: Employee A cannot report to B if B (directly or transitively) reports to A.
/// Self-reference (reportsTo = self) is also blocked.
/// Tests are designed to fail-first (TDD) until the domain service is implemented.
/// </summary>
public sealed class ReportingChainValidatorTests
{
    // ─── FR-007 / AC-007-8: Self-reference blocked ────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_SelfReference_ShouldThrowCircularReportingException")]
    public void Validate_SelfReference_ShouldThrowCircularReportingException()
    {
        // Arrange — employee tries to set reportsTo = themselves
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var employeeId = Guid.NewGuid();

        // Simulate a lookup function that returns the reporting chain
        // In this case, employeeId reports to itself → immediate circular
        Func<Guid, Guid?> getReportsTo = id => id == employeeId ? employeeId : null;

        // Act
        var act = () => sut.Validate(employeeId, employeeId, getReportsTo);

        // Assert
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-007 / AC-007-8: Direct circular chain (A → B → A) ────────────────

    [Fact(DisplayName = "FR-007 | Validate_DirectCircularChain_ShouldThrowCircularReportingException")]
    public void Validate_DirectCircularChain_ShouldThrowCircularReportingException()
    {
        // Arrange — A reports to B, now setting B reports to A
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var employeeA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();

        // B currently reports to nobody, A currently reports to B
        // Now trying: B.reportsTo = A → would create A → B → A cycle
        Func<Guid, Guid?> getReportsTo = id =>
        {
            if (id == employeeA) return employeeB;
            return null;
        };

        // Act — set B to report to A (when A already reports to B)
        var act = () => sut.Validate(employeeB, employeeA, getReportsTo);

        // Assert — A → B → A is circular
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-007 / AC-007-8: Transitive circular chain (A → B → C → A) ────────

    [Fact(DisplayName = "FR-007 | Validate_TransitiveCircularChain_ShouldThrowCircularReportingException")]
    public void Validate_TransitiveCircularChain_ShouldThrowCircularReportingException()
    {
        // Arrange — A → B → C, now setting C.reportsTo = A → creates A → B → C → A cycle
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();

        // Current chain: A reports to B, B reports to C
        Func<Guid, Guid?> getReportsTo = id =>
        {
            if (id == empA) return empB;
            if (id == empB) return empC;
            return null;
        };

        // Act — set C.reportsTo = A → A → B → C → A
        var act = () => sut.Validate(empC, empA, getReportsTo);

        // Assert
        act.Should().Throw<PAMS.Domain.Exceptions.CircularReportingException>();
    }

    // ─── FR-007 / Happy: Valid reporting chain (no circular) ──────────────────

    [Fact(DisplayName = "FR-007 | Validate_ValidReportingChain_ShouldNotThrow")]
    public void Validate_ValidReportingChain_ShouldNotThrow()
    {
        // Arrange — A → B → C, now setting D.reportsTo = C → valid (D → C → null)
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();
        var empD = Guid.NewGuid();

        Func<Guid, Guid?> getReportsTo = id =>
        {
            if (id == empA) return empB;
            if (id == empB) return empC;
            return null;
        };

        // Act — D reports to C, no circular chain exists
        var act = () => sut.Validate(empD, empC, getReportsTo);

        // Assert
        act.Should().NotThrow();
    }

    // ─── FR-007 / Happy: Null reportsTo is always valid ──────────────────────

    [Fact(DisplayName = "FR-007 | Validate_NullReportsTo_ShouldNotThrow")]
    public void Validate_NullReportsTo_ShouldNotThrow()
    {
        // Arrange — setting reportsTo = null means no manager
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var employeeId = Guid.NewGuid();

        Func<Guid, Guid?> getReportsTo = _ => null;

        // Act — null reportsTo is valid (top of chain)
        var act = () => sut.ValidateNullable(employeeId, null, getReportsTo);

        // Assert
        act.Should().NotThrow();
    }

    // ─── FR-007 / Edge: Deep chain (no cycle) ────────────────────────────────

    [Fact(DisplayName = "FR-007 | Validate_DeepChainNoCycle_ShouldNotThrow")]
    public void Validate_DeepChainNoCycle_ShouldNotThrow()
    {
        // Arrange — chain of 10 employees, no cycle
        var sut = new PAMS.Domain.Services.ReportingChainValidator();
        var employees = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();

        Func<Guid, Guid?> getReportsTo = id =>
        {
            for (int i = 0; i < employees.Length - 1; i++)
            {
                if (id == employees[i]) return employees[i + 1];
            }
            return null;
        };

        var newEmployee = Guid.NewGuid();

        // Act — new employee reports to the end of chain
        var act = () => sut.Validate(newEmployee, employees[0], getReportsTo);

        // Assert — valid chain, no cycle
        act.Should().NotThrow();
    }
}
