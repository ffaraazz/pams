using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Employees;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateEmployeeCommandHandler (FR-007).
/// Handler orchestrates: empCode uniqueness → email uniqueness → reporting chain → persist skills → audit.
/// </summary>
public sealed class CreateEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly ISkillRepository _skillRepo = Substitute.For<ISkillRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private CreateEmployeeCommandHandler CreateSut()
        => new(_employeeRepo, _skillRepo, _unitOfWork, _auditLog);

    // ─── FR-007 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-007 | Handle_ValidCommand_ShouldCreateAndReturnId")]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-NEW",
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "Developer"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        await _employeeRepo.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-007 | Duplicate empCode → ConflictException ─────────────────────

    [Fact(DisplayName = "FR-007 | Handle_DuplicateEmpCode_ShouldThrowConflictException")]
    public async Task Handle_DuplicateEmpCode_ShouldThrowConflictException()
    {
        // Arrange
        _employeeRepo.ExistsByEmpCodeAsync("EMP-DUP", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-DUP",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "QA"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        await _employeeRepo.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    // ─── FR-007 | Duplicate email → ConflictException ───────────────────────

    [Fact(DisplayName = "FR-007 | Handle_DuplicateEmail_ShouldThrowConflictException")]
    public async Task Handle_DuplicateEmail_ShouldThrowConflictException()
    {
        // Arrange
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync("taken@acme.com", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-UNQ",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "taken@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "QA"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─── FR-007 | ReportsTo manager not found → NotFoundException ───────────

    [Fact(DisplayName = "FR-007 | Handle_ManagerNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ManagerNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.GetByEmpCodeAsync("MGR-UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-NEW",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "Dev",
            ReportsToEmpCode = "MGR-UNKNOWN"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-007 | ReportsTo resolved successfully ───────────────────────────

    [Fact(DisplayName = "FR-007 | Handle_ValidManager_ShouldResolveReportsTo")]
    public async Task Handle_ValidManager_ShouldResolveReportsTo()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var manager = new Employee
        {
            Id = managerId,
            EmpCode = TestData.PmEmpCode,
            FirstName = "Alice",
            LastName = "Manager",
            Email = "alice@acme.com",
            Role = EmployeeRole.ProjectManager,
            IsActive = true
        };

        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.GetByEmpCodeAsync(TestData.PmEmpCode, Arg.Any<CancellationToken>()).Returns(manager);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-NEW",
            FirstName = "Bob",
            LastName = "Staff",
            Email = "bob@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "Dev",
            ReportsToEmpCode = TestData.PmEmpCode
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — employee was added with the manager's ID
        await _employeeRepo.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.ReportsToId == managerId),
            Arg.Any<CancellationToken>());
    }

    // ─── FR-007 | Skills added successfully ─────────────────────────────────

    [Fact(DisplayName = "FR-007 | Handle_WithSkills_ShouldAddEmployeeSkills")]
    public async Task Handle_WithSkills_ShouldAddEmployeeSkills()
    {
        // Arrange
        var skillId1 = Guid.NewGuid();
        var skillId2 = Guid.NewGuid();
        var skill1 = new Skill { Id = skillId1, SkillName = "C#", IsActive = true };
        var skill2 = new Skill { Id = skillId2, SkillName = "SQL", IsActive = true };

        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _skillRepo.GetByIdAsync(skillId1, Arg.Any<CancellationToken>()).Returns(skill1);
        _skillRepo.GetByIdAsync(skillId2, Arg.Any<CancellationToken>()).Returns(skill2);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-SKILL",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "Dev",
            SkillIds = [skillId1, skillId2]
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _employeeRepo.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.EmployeeSkills.Count == 2),
            Arg.Any<CancellationToken>());
    }

    // ─── FR-007 | Unknown skill ID is silently skipped ──────────────────────

    [Fact(DisplayName = "FR-007 | Handle_UnknownSkillId_ShouldSkipSilently")]
    public async Task Handle_UnknownSkillId_ShouldSkipSilently()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _skillRepo.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).Returns((Skill?)null);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-SKIP",
            FirstName = "Skip",
            LastName = "Skill",
            Email = "skip@acme.com",
            Role = EmployeeRole.Staff,
            Designation = "Dev",
            SkillIds = [unknownId]
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — employee created with 0 skills (unknown ID skipped)
        await _employeeRepo.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.EmployeeSkills.Count == 0),
            Arg.Any<CancellationToken>());
    }

    // ─── FR-007 | Audit log written ─────────────────────────────────────────

    [Fact(DisplayName = "FR-007 | Handle_SuccessfulCreate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulCreate_ShouldWriteAuditLog()
    {
        // Arrange
        _employeeRepo.ExistsByEmpCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _employeeRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateEmployeeCommand
        {
            EmpCode = "EMP-AUDIT",
            FirstName = "Audit",
            LastName = "Test",
            Email = "audit@acme.com",
            Role = EmployeeRole.HR,
            Designation = "HR Lead"
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("employee.created")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
