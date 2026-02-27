using FluentAssertions;
using NSubstitute;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for StopAllocationCommandHandler (FR-013).
/// Handler flow: auth check → load allocation → PM scope check →
/// delegate stop-date to AllocationStopService → persist → audit.
/// </summary>
public sealed class StopAllocationCommandHandlerTests
{
    private readonly PAMS.Domain.Repositories.IAllocationRepository _allocationRepo = Substitute.For<PAMS.Domain.Repositories.IAllocationRepository>();
    private readonly PAMS.Domain.Repositories.IProjectRepository _projectRepo = Substitute.For<PAMS.Domain.Repositories.IProjectRepository>();
    private readonly PAMS.Application.Interfaces.ICurrentUserService _currentUser = Substitute.For<PAMS.Application.Interfaces.ICurrentUserService>();
    private readonly PAMS.Domain.Common.IUnitOfWork _unitOfWork = Substitute.For<PAMS.Domain.Common.IUnitOfWork>();
    private readonly PAMS.Application.Interfaces.IAuditLogService _auditLog = Substitute.For<PAMS.Application.Interfaces.IAuditLogService>();

    private PAMS.Application.Commands.Allocations.StopAllocationCommandHandler CreateSut()
        => new(_allocationRepo, _projectRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── FR-013 / AC-013-2: Happy path – active allocation stopped ──────────

    [Fact(DisplayName = "FR-013 | Handle_ActiveAllocation_ShouldSetToDateAndPersist")]
    public async Task Handle_ActiveAllocation_ShouldSetToDateAndPersist()
    {
        // Arrange — allocation started yesterday, toDate is null (open-ended)
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var pmId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.EmployeeId.Returns(employeeId);
        allocation.FromDate.Returns(TestData.Yesterday);
        allocation.ToDate.Returns((DateOnly?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(pmId);
        project.IsActive.Returns(true);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(pmId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.StopAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — toDate is set (AllocationStopService determines the value)
        allocation.Received(1).ToDate = Arg.Any<DateOnly>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-013 / AC-013-5: PM cannot stop allocations on other PM's project ─

    [Fact(DisplayName = "FR-013 | Handle_PmNotProjectOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotProjectOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(otherPmId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.StopAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.ForbiddenException>();
    }

    // ─── FR-013 / Edge: Allocation not found ─────────────────────────────────

    [Fact(DisplayName = "FR-013 | Handle_AllocationNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_AllocationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var allocationId = Guid.NewGuid();

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns((PAMS.Domain.Entities.Allocation?)null);

        var command = new PAMS.Application.Commands.Allocations.StopAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.NotFoundException>();
    }

    // ─── FR-013 / Edge: Already ended allocation ─────────────────────────────

    [Fact(DisplayName = "FR-013 | Handle_AlreadyEndedAllocation_ShouldThrowDomainException")]
    public async Task Handle_AlreadyEndedAllocation_ShouldThrowDomainException()
    {
        // Arrange — allocation with toDate in the past
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.FromDate.Returns(TestData.Yesterday.AddDays(-30));
        allocation.ToDate.Returns(TestData.Yesterday);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.StopAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert — already ended allocations cannot be stopped again
        await act.Should().ThrowAsync<PAMS.Domain.Exceptions.DomainException>();
    }

    // ─── FR-013 / AC-013-4: Audit log written after stop ─────────────────────

    [Fact(DisplayName = "FR-013 | Handle_SuccessfulStop_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulStop_ShouldWriteAuditLog()
    {
        // Arrange
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.FromDate.Returns(TestData.Yesterday);
        allocation.ToDate.Returns((DateOnly?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.StopAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("allocation.stopped")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
