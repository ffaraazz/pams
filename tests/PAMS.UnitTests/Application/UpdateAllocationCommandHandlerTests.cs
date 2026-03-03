using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Allocations;
using PAMS.Application.DTOs.Allocations;
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
/// Unit tests for UpdateAllocationCommandHandler (FR-012).
/// Handler orchestrates: find allocation → find project → PM scope → ended check →
/// capacity with exclusion → update → persist → audit.
/// </summary>
public sealed class UpdateAllocationCommandHandlerTests
{
    private readonly IAllocationRepository _allocationRepo = Substitute.For<IAllocationRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateAllocationCommandHandler CreateSut()
        => new(_allocationRepo, _projectRepo, _employeeRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Allocation CreateAllocation(
        Guid? id = null,
        Guid? projectId = null,
        Guid? employeeId = null,
        int pct = 50,
        DateOnly? from = null,
        DateOnly? to = null)
    {
        return new Allocation
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            EmployeeId = employeeId ?? Guid.NewGuid(),
            Percentage = pct,
            FromDate = from ?? TestData.Today,
            ToDate = to, // null = open-ended
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Project CreateProject(Guid id, Guid? pmId = null)
    {
        return new Project
        {
            Id = id,
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Test Project",
            ProjectManagerId = pmId,
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

    // ─── FR-012 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-012 | Handle_ValidUpdate_ShouldPersistAndReturn")]
    public async Task Handle_ValidUpdate_ShouldPersistAndReturn()
    {
        // Arrange
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = CreateAllocation(allocId, projectId, employeeId, 50);
        var project = CreateProject(projectId, hrId);
        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Bob",
            LastName = "Staff",
            IsActive = true
        };

        _currentUser.Role.Returns(EmployeeRole.HR);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _allocationRepo.GetOverlappingTotalPercentageAsync(
            employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(),
            allocId, Arg.Any<CancellationToken>()).Returns(0);
        _employeeRepo.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 60
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Percentage.Should().Be(60);
        _allocationRepo.Received(1).Update(Arg.Any<Allocation>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-012 | Allocation not found → NotFoundException ──────────────────

    [Fact(DisplayName = "FR-012 | Handle_AllocationNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_AllocationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _allocationRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Allocation?)null);

        var command = new UpdateAllocationCommand
        {
            AllocationId = Guid.NewGuid(),
            FromDate = TestData.Today,
            Percentage = 50
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-012 | PM not project owner → ForbiddenException ─────────────────

    [Fact(DisplayName = "FR-012 | Handle_PmNotOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();

        var allocation = CreateAllocation(allocId, projectId);
        var project = CreateProject(projectId, otherPmId);

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 50
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ─── FR-012 | Ended allocation → DomainException ────────────────────────

    [Fact(DisplayName = "FR-012 | Handle_AllocationEnded_ShouldThrowDomainException")]
    public async Task Handle_AllocationEnded_ShouldThrowDomainException()
    {
        // Arrange — allocation ended yesterday
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var allocation = CreateAllocation(allocId, projectId, to: TestData.Yesterday);
        var project = CreateProject(projectId);

        _currentUser.Role.Returns(EmployeeRole.HR);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 50
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already ended*");
    }

    // ─── FR-012 | Capacity exceeded → CapacityExceededException ─────────────

    [Fact(DisplayName = "FR-012 | Handle_CapacityExceeded_ShouldThrowCapacityExceededException")]
    public async Task Handle_CapacityExceeded_ShouldThrowCapacityExceededException()
    {
        // Arrange — other allocations total 80%, trying to set this to 30%
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var allocation = CreateAllocation(allocId, projectId, employeeId, 50);
        var project = CreateProject(projectId);

        _currentUser.Role.Returns(EmployeeRole.HR);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _allocationRepo.GetOverlappingTotalPercentageAsync(
            employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(),
            allocId, Arg.Any<CancellationToken>()).Returns(80);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 30 // 80 + 30 = 110 > 100
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CapacityExceededException>();
    }

    // ─── FR-012 | PM is project owner → allowed ─────────────────────────────

    [Fact(DisplayName = "FR-012 | Handle_PmIsOwner_ShouldSucceed")]
    public async Task Handle_PmIsOwner_ShouldSucceed()
    {
        // Arrange
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var pmId = Guid.NewGuid();

        var allocation = CreateAllocation(allocId, projectId, employeeId, 50);
        var project = CreateProject(projectId, pmId);
        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Bob",
            LastName = "Staff",
            IsActive = true
        };

        _currentUser.Role.Returns(EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(pmId);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _allocationRepo.GetOverlappingTotalPercentageAsync(
            employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(),
            allocId, Arg.Any<CancellationToken>()).Returns(0);
        _employeeRepo.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 50
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── FR-012 | ProjectRole updated ────────────────────────────────────────

    [Fact(DisplayName = "FR-012 | Handle_WhenAllocationUpdated_ProjectRoleShouldBeUpdated")]
    public async Task Handle_WhenAllocationUpdated_ProjectRoleShouldBeUpdated()
    {
        // Arrange — existing allocation with ProjectRole "Developer"
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = CreateAllocation(allocId, projectId, employeeId, 50);
        allocation.ProjectRole = "Developer";
        var project = CreateProject(projectId, hrId);
        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Bob",
            LastName = "Staff",
            IsActive = true
        };

        _currentUser.Role.Returns(EmployeeRole.HR);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _allocationRepo.GetOverlappingTotalPercentageAsync(
            employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(),
            allocId, Arg.Any<CancellationToken>()).Returns(0);
        _employeeRepo.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 60,
            ProjectRole = "Tech Lead"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — ProjectRole should be updated to the new value
        result.ProjectRole.Should().Be("Tech Lead");
    }

    // ─── FR-012 | Audit log on success ──────────────────────────────────────

    [Fact(DisplayName = "FR-012 | Handle_SuccessfulUpdate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulUpdate_ShouldWriteAuditLog()
    {
        // Arrange
        var allocId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var allocation = CreateAllocation(allocId, projectId, employeeId, 50);
        var project = CreateProject(projectId);
        var employee = new Employee
        {
            Id = employeeId,
            EmpCode = TestData.StaffEmpCode,
            FirstName = "Bob",
            LastName = "Staff",
            IsActive = true
        };

        _currentUser.Role.Returns(EmployeeRole.HR);
        _allocationRepo.GetByIdAsync(allocId, Arg.Any<CancellationToken>()).Returns(allocation);
        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _allocationRepo.GetOverlappingTotalPercentageAsync(
            employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(),
            allocId, Arg.Any<CancellationToken>()).Returns(0);
        _employeeRepo.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);

        var command = new UpdateAllocationCommand
        {
            AllocationId = allocId,
            FromDate = TestData.Today,
            Percentage = 50
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("allocation.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
