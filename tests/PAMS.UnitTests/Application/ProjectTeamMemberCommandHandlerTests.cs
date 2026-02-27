using FluentAssertions;
using MediatR;
using NSubstitute;
using PAMS.Application.Commands.ProjectTeamMembers;
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
/// Unit tests for AddProjectTeamMemberCommandHandler (FR-020).
/// Handler orchestrates: project lookup → PM scope → employee lookups → duplicate check →
/// circular check → persist → audit.
/// </summary>
public sealed class AddProjectTeamMemberCommandHandlerTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly IProjectTeamMemberRepository _teamMemberRepo = Substitute.For<IProjectTeamMemberRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private AddProjectTeamMemberCommandHandler CreateSut()
        => new(_projectRepo, _employeeRepo, _teamMemberRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Project CreateProject(Guid? pmId = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Test Project",
            ProjectManagerId = pmId,
            IsActive = true
        };
    }

    private static Employee CreateEmployee(string empCode, Guid? id = null)
    {
        return new Employee
        {
            Id = id ?? Guid.NewGuid(),
            EmpCode = empCode,
            FirstName = empCode,
            LastName = "Test",
            Email = $"{empCode.ToLower()}@acme.com",
            Role = EmployeeRole.Staff,
            IsActive = true
        };
    }

    // ─── FR-020 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_ValidCommand_ShouldPersistTeamMember")]
    public async Task Handle_ValidCommand_ShouldPersistTeamMember()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);
        _teamMemberRepo.WouldCreateCircularLeadershipAsync(
            project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        await _teamMemberRepo.Received(1).AddAsync(Arg.Any<ProjectTeamMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-020 | Project not found ─────────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_ProjectNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ProjectNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-020 | PM scope — not owner ──────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_PmNotOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();
        var project = CreateProject(otherPmId);

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─── FR-020 | Team lead not found ───────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_LeadNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_LeadNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var project = CreateProject();
        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("GHOST", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "GHOST",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-020 | Reportee not found ────────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_ReporteeNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ReporteeNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("GHOST", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "GHOST"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-020 | Duplicate assignment → ConflictException ──────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_DuplicateAssignment_ShouldThrowConflictException")]
    public async Task Handle_DuplicateAssignment_ShouldThrowConflictException()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─── FR-020 | Circular leadership → CircularReportingException ──────────

    [Fact(DisplayName = "FR-020 | AddHandle_CircularLeadership_ShouldThrowCircularReportingException")]
    public async Task Handle_CircularLeadership_ShouldThrowCircularReportingException()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);
        _teamMemberRepo.WouldCreateCircularLeadershipAsync(
            project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CircularReportingException>();
    }

    // ─── FR-020 | Audit log on success ──────────────────────────────────────

    [Fact(DisplayName = "FR-020 | AddHandle_Success_ShouldWriteAuditLog")]
    public async Task Handle_Success_ShouldWriteAuditLog()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);
        _teamMemberRepo.WouldCreateCircularLeadershipAsync(
            project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);

        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("teamMember.added")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Unit tests for RemoveProjectTeamMemberCommandHandler (FR-020).
/// </summary>
public sealed class RemoveProjectTeamMemberCommandHandlerTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly IProjectTeamMemberRepository _teamMemberRepo = Substitute.For<IProjectTeamMemberRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private RemoveProjectTeamMemberCommandHandler CreateSut()
        => new(_projectRepo, _employeeRepo, _teamMemberRepo, _currentUser, _unitOfWork, _auditLog);

    private static Project CreateProject(Guid? pmId = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Test Project",
            ProjectManagerId = pmId,
            IsActive = true
        };
    }

    private static Employee CreateEmployee(string empCode, Guid? id = null)
    {
        return new Employee
        {
            Id = id ?? Guid.NewGuid(),
            EmpCode = empCode,
            FirstName = empCode,
            LastName = "Test",
            Email = $"{empCode.ToLower()}@acme.com",
            Role = EmployeeRole.Staff,
            IsActive = true
        };
    }

    // ─── FR-020 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-020 | RemoveHandle_ValidCommand_ShouldRemoveAndReturn")]
    public async Task Handle_ValidCommand_ShouldRemoveAndReturn()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        await _teamMemberRepo.Received(1).RemoveAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-020 | Project not found ─────────────────────────────────────────

    [Fact(DisplayName = "FR-020 | RemoveHandle_ProjectNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_ProjectNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-020 | PM not owner → ForbiddenException ─────────────────────────

    [Fact(DisplayName = "FR-020 | RemoveHandle_PmNotOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();
        var project = CreateProject(otherPmId);

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);

        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─── FR-020 | Assignment doesn't exist → NotFoundException ──────────────

    [Fact(DisplayName = "FR-020 | RemoveHandle_AssignmentNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_AssignmentNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(false);

        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-020 | Audit log on success ──────────────────────────────────────

    [Fact(DisplayName = "FR-020 | RemoveHandle_Success_ShouldWriteAuditLog")]
    public async Task Handle_Success_ShouldWriteAuditLog()
    {
        // Arrange
        var project = CreateProject();
        var lead = CreateEmployee("LEAD001");
        var reportee = CreateEmployee("REP001");

        _currentUser.Role.Returns(EmployeeRole.HR);
        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(project);
        _employeeRepo.GetByEmpCodeAsync("LEAD001", Arg.Any<CancellationToken>()).Returns(lead);
        _employeeRepo.GetByEmpCodeAsync("REP001", Arg.Any<CancellationToken>()).Returns(reportee);
        _teamMemberRepo.ExistsAsync(project.Id, lead.Id, reportee.Id, Arg.Any<CancellationToken>()).Returns(true);

        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = TestData.ProjectCode,
            TeamLeadEmpCode = "LEAD001",
            ReporteeEmpCode = "REP001"
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("teamMember.removed")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
