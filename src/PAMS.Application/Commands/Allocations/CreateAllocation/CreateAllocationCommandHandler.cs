using MediatR;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;
using PAMS.Domain.Services;

namespace PAMS.Application.Commands.Allocations;

/// <summary>
/// Handles CreateAllocationCommand (FR-010).
/// Flow: auth → project lookup → PM scope → employee lookup → capacity check → persist → audit.
/// </summary>
public sealed class CreateAllocationCommandHandler
    : IRequestHandler<CreateAllocationCommand, AllocationDetailResponse>
{
    private readonly IProjectRepository _projectRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IAllocationRepository _allocationRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public CreateAllocationCommandHandler(
        IProjectRepository projectRepo,
        IEmployeeRepository employeeRepo,
        IAllocationRepository allocationRepo,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _projectRepo = projectRepo;
        _employeeRepo = employeeRepo;
        _allocationRepo = allocationRepo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<AllocationDetailResponse> Handle(
        CreateAllocationCommand request, CancellationToken cancellationToken)
    {
        // 1. Load project
        var project = await _projectRepo.GetByCodeAsync(request.ProjectCode, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectCode);

        // 2. PM scope enforcement
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            throw new ForbiddenException(
                "Project Managers can only allocate to their own projects.",
                "ERR_NOT_PROJECT_OWNER");
        }

        // 3. Load employee
        var employee = await _employeeRepo.GetByEmpCodeAsync(request.EmpCode, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmpCode);

        // 4. Capacity check — load overlapping allocations and sum percentages
        var overlapping = await _allocationRepo.GetOverlappingAsync(
            employee.Id, request.FromDate, request.ToDate, cancellationToken);
        var existingTotal = overlapping.Sum(a => a.Percentage);

        var capacityService = new AllocationCapacityService();
        capacityService.Validate(existingTotal, request.Percentage, request.FromDate, request.ToDate);

        // 5. Build and persist allocation
        var allocation = new Allocation
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Percentage = request.Percentage,
            ProjectRole = request.ProjectRole,
            AllocatedById = _currentUser.EmployeeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _allocationRepo.AddAsync(allocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Audit log
        await _auditLog.LogAsync("allocation.created", new
        {
            AllocationId = allocation.Id,
            employee.EmpCode,
            project.ProjectCode,
            allocation.Percentage,
            allocation.FromDate,
            allocation.ToDate
        }, cancellationToken);

        // 7. Map response
        return AllocationDetailResponse.MapFrom(allocation, employee, project);
    }
}
