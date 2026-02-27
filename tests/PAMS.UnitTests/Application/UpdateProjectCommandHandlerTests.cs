using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Projects;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for UpdateProjectCommandHandler (FR-005).
/// Handler orchestrates: find by code → PM scope enforcement → resolve PM → update → persist → audit.
/// </summary>
public sealed class UpdateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateProjectCommandHandler CreateSut()
        => new(_projectRepo, _employeeRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Project CreateProject(Guid? pmId = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Old Name",
            AccountId = Guid.NewGuid(),
            ProjectManagerId = pmId,
            StartDate = TestData.Today,
            Status = ProjectStatus.Upcoming,
            Billable = true,
            IsActive = true,
            Account = new Account
            {
                Id = Guid.NewGuid(),
                AccountCode = TestData.AccountCode,
                AccountName = "Acme",
                AccountType = AccountType.Client
            }
        };
    }

    private static Employee CreateEmployee(Guid id, string empCode = "PM001")
    {
        return new Employee
        {
            Id = id,
            EmpCode = empCode,
            FirstName = "Alice",
            LastName = "Manager",
            Email = "alice@acme.com",
            Role = EmployeeRole.ProjectManager,
            IsActive = true
        };
    }

    // ─── FR-005 | Happy path — HR updates any project ───────────────────────

    [Fact(DisplayName = "FR-005 | Handle_HrUpdatesProject_ShouldSucceed")]
    public async Task Handle_HrUpdatesProject_ShouldSucceed()
    {
        // Arrange
        var pmId = Guid.NewGuid();
        var project = CreateProject(pmId);
        var pm = CreateEmployee(pmId, TestData.PmEmpCode);

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.PmEmpCode, Arg.Any<CancellationToken>()).Returns(pm);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Updated Name",
            ProjectManagerEmpCode = TestData.PmEmpCode,
            StartDate = TestData.Today,
            Status = ProjectStatus.Active,
            Billable = false
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ProjectName.Should().Be("Updated Name");
        result.Status.Should().Be(ProjectStatus.Active);
        result.Billable.Should().BeFalse();
        _projectRepo.Received(1).Update(Arg.Any<Project>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-005 | Project not found → NotFoundException ─────────────────────

    [Fact(DisplayName = "FR-005 | Handle_ProjectNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ProjectNotFound_ShouldThrowNotFoundException()
    {
        //Arrange
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            StartDate = TestData.Today,
            Status = ProjectStatus.Upcoming,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-005 | PM scope — PM not project owner → ForbiddenException ──────

    [Fact(DisplayName = "FR-005 | Handle_PmNotOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange — PM tries to update a project they don't own
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();
        var project = CreateProject(otherPmId);

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            StartDate = TestData.Today,
            Status = ProjectStatus.Active,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─── FR-005 | PM scope — PM is project owner → allowed ──────────────────

    [Fact(DisplayName = "FR-005 | Handle_PmIsOwner_ShouldSucceed")]
    public async Task Handle_PmIsOwner_ShouldSucceed()
    {
        // Arrange
        var pmId = Guid.NewGuid();
        var project = CreateProject(pmId);

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(pmId);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "My Updated Project",
            StartDate = TestData.Today,
            Status = ProjectStatus.Active,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── FR-005 | New PM not found → NotFoundException ──────────────────────

    [Fact(DisplayName = "FR-005 | Handle_NewPmNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_NewPmNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var pmId = Guid.NewGuid();
        var project = CreateProject(pmId);

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            ProjectManagerEmpCode = "UNKNOWN",
            StartDate = TestData.Today,
            Status = ProjectStatus.Active,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-005 | Deactivation via IsActive flag ────────────────────────────

    [Fact(DisplayName = "FR-005 | Handle_SetIsActiveFalse_ShouldDeactivateProject")]
    public async Task Handle_SetIsActiveFalse_ShouldDeactivateProject()
    {
        // Arrange
        var project = CreateProject();
        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Deactivated",
            StartDate = TestData.Today,
            Status = ProjectStatus.Completed,
            Billable = false,
            IsActive = false
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
    }

    // ─── FR-005 | Audit log on success ──────────────────────────────────────

    [Fact(DisplayName = "FR-005 | Handle_SuccessfulUpdate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulUpdate_ShouldWriteAuditLog()
    {
        // Arrange
        var project = CreateProject();
        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Updated",
            StartDate = TestData.Today,
            Status = ProjectStatus.Active,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("project.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
