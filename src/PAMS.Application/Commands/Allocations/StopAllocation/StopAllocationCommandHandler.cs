using MediatR;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.Domain.Services;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Handles StopAllocationCommand (FR-013).
/// Flow: load allocation → PM scope → verify not already ended → compute stop date → persist → audit.
/// </summary>
public sealed class StopAllocationCommandHandler : IRequestHandler<StopAllocationCommand, Unit>
{
    private readonly IAllocationRepository _allocationRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public StopAllocationCommandHandler(
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

    public async Task<Unit> Handle(StopAllocationCommand request, CancellationToken cancellationToken)
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
                "Project Managers can only stop allocations on their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        // 4. Check if already ended
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (allocation.ToDate.HasValue && allocation.ToDate.Value <= today)
        {
            throw new DomainException(
                "Allocation has already ended.",
                "ERR_ALLOCATION_ALREADY_ENDED");
        }

        // 5. Compute stop date
        var stopService = new AllocationStopService();
        var stopDate = stopService.ComputeStopDate(allocation.FromDate, today);

        // 6. Apply stop date
        // When cancelling a future allocation (stopDate < fromDate), use fromDate
        // to satisfy DB constraint chk_allocation_dates (to_date >= from_date).
        allocation.ToDate = stopDate < allocation.FromDate ? allocation.FromDate : stopDate;
        allocation.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Audit log
        await _auditLog.LogAsync("allocation.stopped", new
        {
            AllocationId = allocation.Id,
            StopDate = stopDate,
            allocation.EmployeeId,
            allocation.ProjectId
        }, cancellationToken);

        return Unit.Value;
    }
}
