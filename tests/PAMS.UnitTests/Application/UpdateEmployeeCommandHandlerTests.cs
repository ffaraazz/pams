using FluentAssertions;
using MediatR;
using NSubstitute;
using PAMS.Application.Commands.Employees;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for UpdateEmployeeCommandHandler (FR-008).
/// Handler orchestrates: find → email uniqueness → reporting chain → circular check → update skills → persist → audit.
/// </summary>
public sealed class UpdateEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly ISkillRepository _skillRepo = Substitute.For<ISkillRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateEmployeeCommandHandler CreateSut()
        => new(_employeeRepo, _skillRepo, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Employee CreateEmployee(string empCode = "EMP001", string email = "john@acme.com")
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = empCode,
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            Designation = "Developer",
            Role = EmployeeRole.Staff,
            IsActive = true,
            EmployeeSkills = new List<EmployeeSkill>()
        };
    }

    // ─── FR-008 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_ValidCommand_ShouldUpdateAndReturnUnit")]
    public async Task Handle_ValidCommand_ShouldUpdateAndReturnUnit()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Updated",
            LastName = "Name",
            Email = "john@acme.com", // same email
            Designation = "Senior Dev",
            Role = EmployeeRole.Staff
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _employeeRepo.Received(1).Update(Arg.Any<Employee>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-008 | Not found → NotFoundException ─────────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_EmployeeNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_EmployeeNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _employeeRepo.GetByEmpCodeAsync("UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = "UNKNOWN",
            FirstName = "X",
            LastName = "Y",
            Email = "x@acme.com",
            Designation = "Dev",
            Role = EmployeeRole.Staff
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-008 | Changed email taken → ConflictException ───────────────────

    [Fact(DisplayName = "FR-008 | Handle_EmailChangedAndTaken_ShouldThrowConflictException")]
    public async Task Handle_EmailChangedAndTaken_ShouldThrowConflictException()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode, "old@acme.com");
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _employeeRepo.ExistsByEmailAsync("taken@acme.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "taken@acme.com", // different email, already exists
            Designation = "Dev",
            Role = EmployeeRole.Staff
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─── FR-008 | Same email → no uniqueness check ──────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_SameEmail_ShouldNotCheckUniqueness")]
    public async Task Handle_SameEmail_ShouldNotCheckUniqueness()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode, "same@acme.com");
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "same@acme.com", // same email
            Designation = "Dev",
            Role = EmployeeRole.Staff
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — ExistsByEmailAsync should NOT be called
        await _employeeRepo.DidNotReceive().ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ─── FR-008 | Circular reporting → CircularReportingException ───────────

    [Fact(DisplayName = "FR-008 | Handle_CircularReporting_ShouldThrowCircularReportingException")]
    public async Task Handle_CircularReporting_ShouldThrowCircularReportingException()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode);
        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            EmpCode = TestData.PmEmpCode,
            FirstName = "Alice",
            LastName = "Mgr",
            Email = "alice@acme.com",
            Role = EmployeeRole.ProjectManager,
            IsActive = true
        };

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _employeeRepo.GetByEmpCodeAsync(TestData.PmEmpCode, Arg.Any<CancellationToken>())
            .Returns(manager);
        _employeeRepo.WouldCreateCircularReportingAsync(employee.Id, managerId, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Designation = "Dev",
            Role = EmployeeRole.Staff,
            ReportsToEmpCode = TestData.PmEmpCode
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CircularReportingException>();
    }

    // ─── FR-008 | Manager not found → NotFoundException ─────────────────────

    [Fact(DisplayName = "FR-008 | Handle_ManagerNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ManagerNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _employeeRepo.GetByEmpCodeAsync("MGR-GHOST", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Designation = "Dev",
            Role = EmployeeRole.Staff,
            ReportsToEmpCode = "MGR-GHOST"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-008 | Skills cleared and re-added ───────────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_WithNewSkills_ShouldClearAndReAdd")]
    public async Task Handle_WithNewSkills_ShouldClearAndReAdd()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var existingSkillId = Guid.NewGuid();
        var employee = CreateEmployee(TestData.StaffEmpCode);
        employee.EmployeeSkills.Add(new EmployeeSkill { EmployeeId = employee.Id, SkillId = existingSkillId });

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Designation = "Dev",
            Role = EmployeeRole.Staff,
            SkillIds = [skillId]
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — old skill cleared, new one added
        employee.EmployeeSkills.Should().HaveCount(1);
        employee.EmployeeSkills.First().SkillId.Should().Be(skillId);
    }

    // ─── FR-008 | IsActive flag properly set ────────────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_DeactivateEmployee_ShouldSetInactive")]
    public async Task Handle_DeactivateEmployee_ShouldSetInactive()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Designation = "Dev",
            Role = EmployeeRole.Staff,
            IsActive = false
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        employee.IsActive.Should().BeFalse();
    }

    // ─── FR-008 | Audit log written ─────────────────────────────────────────

    [Fact(DisplayName = "FR-008 | Handle_SuccessfulUpdate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulUpdate_ShouldWriteAuditLog()
    {
        // Arrange
        var employee = CreateEmployee(TestData.StaffEmpCode);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        var command = new UpdateEmployeeCommand
        {
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Updated",
            LastName = "Employee",
            Email = "john@acme.com",
            Designation = "Senior Dev",
            Role = EmployeeRole.Staff
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("employee.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
