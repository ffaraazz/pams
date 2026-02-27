using FluentAssertions;
using NSubstitute;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for RemoveAllocationCommandHandler (FR-014).
/// Handler: auth check → load allocation → PM scope → verify ended → soft-delete → audit.
/// Only ended allocations (toDate &lt; today) may be removed.
/// </summary>
public sealed class RemoveAllocationCommandHandlerTests
{
    private readonly PAMS.Domain.Repositories.IAllocationRepository _allocationRepo = Substitute.For<PAMS.Domain.Repositories.IAllocationRepository>();
    private readonly PAMS.Domain.Repositories.IProjectRepository _projectRepo = Substitute.For<PAMS.Domain.Repositories.IProjectRepository>();
    private readonly PAMS.Application.Interfaces.ICurrentUserService _currentUser = Substitute.For<PAMS.Application.Interfaces.ICurrentUserService>();
    private readonly PAMS.Domain.Common.IUnitOfWork _unitOfWork = Substitute.For<PAMS.Domain.Common.IUnitOfWork>();
    private readonly PAMS.Application.Interfaces.IAuditLogService _auditLog = Substitute.For<PAMS.Application.Interfaces.IAuditLogService>();

    private PAMS.Application.Commands.Allocations.RemoveAllocationCommandHandler CreateSut()
        => new(_allocationRepo, _projectRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── FR-014 / AC-014-1 + AC-014-2: Happy path – ended allocation removed ─

    [Fact(DisplayName = "FR-014 | Handle_EndedAllocation_ShouldSoftDeleteAndPersist")]
    public async Task Handle_EndedAllocation_ShouldSoftDeleteAndPersist()
    {
        // Arrange — allocation ended yesterday
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.ToDate.Returns(TestData.Yesterday);
        allocation.DeletedAt.Returns((DateTime?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — soft delete marker set
        allocation.Received(1).DeletedAt = Arg.Any<DateTime>();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-014 / AC-014-4: Active allocation cannot be removed ─────────────

    [Fact(DisplayName = "FR-014 | Handle_ActiveAllocation_ShouldThrowDomainException")]
    public async Task Handle_ActiveAllocation_ShouldThrowDomainException()
    {
        // Arrange — allocation still active (toDate is tomorrow or null)
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.FromDate.Returns(TestData.Yesterday);
        allocation.ToDate.Returns(TestData.Tomorrow); // still active
        allocation.DeletedAt.Returns((DateTime?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert — "Only ended allocations can be removed"
        await act.Should().ThrowAsync<PAMS.Domain.Exceptions.DomainException>();
    }

    // ─── FR-014 / AC-014-4: Future allocation cannot be removed ──────────────

    [Fact(DisplayName = "FR-014 | Handle_FutureAllocation_ShouldThrowDomainException")]
    public async Task Handle_FutureAllocation_ShouldThrowDomainException()
    {
        // Arrange — allocation hasn't started yet (fromDate in the future)
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.FromDate.Returns(TestData.Tomorrow);
        allocation.ToDate.Returns(TestData.Tomorrow.AddDays(30));
        allocation.DeletedAt.Returns((DateTime?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Domain.Exceptions.DomainException>();
    }

    // ─── FR-014 / Edge: Open-ended allocation cannot be removed ──────────────

    [Fact(DisplayName = "FR-014 | Handle_OpenEndedAllocation_ShouldThrowDomainException")]
    public async Task Handle_OpenEndedAllocation_ShouldThrowDomainException()
    {
        // Arrange — allocation with null toDate (ongoing)
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.FromDate.Returns(TestData.Yesterday);
        allocation.ToDate.Returns((DateOnly?)null); // open-ended
        allocation.DeletedAt.Returns((DateTime?)null);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Domain.Exceptions.DomainException>();
    }

    // ─── FR-014 / Edge: PM on someone else's project → forbidden ─────────────

    [Fact(DisplayName = "FR-014 | Handle_PmOnOtherProject_ShouldThrowForbiddenException")]
    public async Task Handle_PmOnOtherProject_ShouldThrowForbiddenException()
    {
        // Arrange
        var allocationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();

        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(allocationId);
        allocation.ProjectId.Returns(projectId);
        allocation.ToDate.Returns(TestData.Yesterday);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(projectId);
        project.ProjectManagerId.Returns(otherPmId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        _projectRepo.GetByIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.ForbiddenException>();
    }

    // ─── FR-014 / Edge: Already soft-deleted → not found ─────────────────────

    [Fact(DisplayName = "FR-014 | Handle_AlreadySoftDeleted_ShouldThrowNotFoundException")]
    public async Task Handle_AlreadySoftDeleted_ShouldThrowNotFoundException()
    {
        // Arrange — repo returns null for soft-deleted records (global query filter)
        var allocationId = Guid.NewGuid();

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);

        _allocationRepo.GetByIdAsync(allocationId, Arg.Any<CancellationToken>())
            .Returns((PAMS.Domain.Entities.Allocation?)null);

        var command = new PAMS.Application.Commands.Allocations.RemoveAllocationCommand
        {
            AllocationId = allocationId
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.NotFoundException>();
    }
}
