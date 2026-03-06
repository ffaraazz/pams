using MediatR;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Handles RemoveAllocationCommand (FR-014).
/// Flow: load allocation → PM scope → verify ended → soft-delete → persist → audit.
/// Only allocations with toDate &lt; today may be removed.
/// </summary>
public sealed class RemoveAllocationCommandHandler : IRequestHandler<RemoveAllocationCommand, Unit>
{
    private readonly IAllocationRepository _allocationRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public RemoveAllocationCommandHandler(
        IAllocationRepository allocationRepo,
        IProjectRepository projectRepo,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _allocationRepo = allocationRepo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<Unit> Handle(RemoveAllocationCommand request, CancellationToken cancellationToken)
    {
        // 1. Load allocation
        var allocation = await _allocationRepo.GetByIdAsync(request.AllocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation", request.AllocationId);

        // 2. Load project for PM scope check
        var project = await _projectRepo.GetByIdAsync(allocation.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", allocation.ProjectId);

        // 3. PM scope enforcement
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            throw new ForbiddenException(
                "Project Managers can only remove allocations on their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        // 4. Verify allocation has ended (toDate <= today)
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!allocation.ToDate.HasValue || allocation.ToDate.Value > today)
        {
            throw new DomainException(
                "Only ended allocations can be removed. Stop the allocation first.",
                "ERR_ALLOCATION_NOT_ENDED");
        }

        // 5. Soft delete
        allocation.DeletedAt = DateTime.UtcNow;
        allocation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Audit log
        await _auditLog.LogAsync("allocation.removed", new
        {
            AllocationId = allocation.Id,
            allocation.EmployeeId,
            allocation.ProjectId
        }, cancellationToken);

        return Unit.Value;
    }
}
