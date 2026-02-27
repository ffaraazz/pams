using MediatR;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.Domain.Services;

namespace PAMS.Application.Commands.Allocations;

public sealed class UpdateAllocationCommandHandler
    : IRequestHandler<UpdateAllocationCommand, AllocationDetailResponse>
{
    private readonly IAllocationRepository _allocationRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateAllocationCommandHandler(
        IAllocationRepository allocationRepo,
        IProjectRepository projectRepo,
        IEmployeeRepository employeeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _allocationRepo = allocationRepo;
        _projectRepo = projectRepo;
        _employeeRepo = employeeRepo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<AllocationDetailResponse> Handle(
        UpdateAllocationCommand request, CancellationToken cancellationToken)
    {
        var allocation = await _allocationRepo.GetByIdAsync(request.AllocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation", request.AllocationId);

        var project = await _projectRepo.GetByIdAsync(allocation.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", allocation.ProjectId);

        // PM scope check
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            throw new ForbiddenException(
                "Project Managers can only update allocations on their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        // Cannot edit ended allocations
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (allocation.ToDate.HasValue && allocation.ToDate.Value < today)
            throw new DomainException(
                "Cannot edit an allocation that has already ended.",
                "ERR_ALLOCATION_ALREADY_ENDED");

        // Capacity check — exclude this allocation from totals
        var existingTotal = await _allocationRepo.GetOverlappingTotalPercentageAsync(
            allocation.EmployeeId, request.FromDate, request.ToDate,
            excludeAllocationId: allocation.Id, ct: cancellationToken);

        var capacityService = new AllocationCapacityService();
        capacityService.Validate(existingTotal, request.Percentage, request.FromDate, request.ToDate);

        allocation.FromDate = request.FromDate;
        allocation.ToDate = request.ToDate;
        allocation.Percentage = request.Percentage;
        allocation.UpdatedAt = DateTime.UtcNow;

        _allocationRepo.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var employee = await _employeeRepo.GetByIdAsync(allocation.EmployeeId, cancellationToken);

        await _auditLog.LogAsync("allocation.updated", new
        {
            allocation.Id,
            allocation.Percentage,
            allocation.FromDate,
            allocation.ToDate
        }, cancellationToken);

        return new AllocationDetailResponse
        {
            AllocationId = allocation.Id,
            EmployeeId = allocation.EmployeeId,
            EmpCode = employee?.EmpCode ?? string.Empty,
            EmployeeName = employee is not null ? $"{employee.FirstName} {employee.LastName}" : string.Empty,
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            Percentage = allocation.Percentage,
            FromDate = allocation.FromDate,
            ToDate = allocation.ToDate,
            CreatedAt = allocation.CreatedAt
        };
    }
}
