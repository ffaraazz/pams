using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Employees;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Common;
using PAMS.Application.DTOs.Employees;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Employee management, search, and filter (single-endpoint strategy).
/// </summary>
[ApiController]
[Route("api/v1/employees")]
[Authorize]
[Produces("application/json")]
[Tags("Employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IAllocationRepository _allocationRepo;
    private readonly ICurrentUserService _currentUser;

    public EmployeesController(
        IMediator mediator,
        IEmployeeRepository employeeRepo,
        IAllocationRepository allocationRepo,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _employeeRepo = employeeRepo;
        _allocationRepo = allocationRepo;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List, search, and filter employees (HR, PM).
    /// </summary>
    /// <remarks>
    /// Unified endpoint for listing, searching, and filtering employees.
    /// Supports text search (name, empCode, skill), role filter, active status filter,
    /// bench-only filter, and date window for availability computation.
    /// </remarks>
    /// <param name="search">Search query. Matches against full name (partial), empCode (prefix), or skill name (exact). Minimum 2 characters.</param>
    /// <param name="role">Filter by employee role (HR, ProjectManager, Staff).</param>
    /// <param name="isActive">Filter by active status.</param>
    /// <param name="benchOnly">Return only employees with 0% allocation for the given date window.</param>
    /// <param name="windowFrom">Start of availability window (default: today).</param>
    /// <param name="windowTo">End of availability window (default: today).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="limit">Items per page (default: 10, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of employee summaries with availability.</returns>
    [HttpGet]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<EmployeeSummaryResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] EmployeeRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] bool benchOnly = false,
        [FromQuery] DateOnly? windowFrom = null,
        [FromQuery] DateOnly? windowTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var roleStr = role?.ToString();
        var employees = await _employeeRepo.GetFilteredAsync(
            search, null, benchOnly, roleStr, isActive, windowFrom, windowTo, page, limit, ct);
        var totalRecords = await _employeeRepo.GetFilteredCountAsync(
            search, null, benchOnly, roleStr, isActive, windowFrom, windowTo, ct);

        var data = employees.Select(e =>
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var activeAllocations = e.Allocations
                .Where(a => a.DeletedAt == null && a.FromDate <= today && (a.ToDate == null || a.ToDate >= today))
                .ToList();
            var totalPct = activeAllocations.Sum(a => a.Percentage);
            var availability = Math.Max(0, 100 - totalPct);

            return new EmployeeSummaryResponse
            {
                EmployeeId = e.Id,
                EmpCode = e.EmpCode,
                FullName = $"{e.FirstName} {e.LastName}",
                Designation = e.Designation,
                Role = e.Role,
                IsActive = e.IsActive,
                AvailabilityPercentage = availability,
                AllocationStatus = totalPct == 0 ? AllocationStatus.Bench
                    : totalPct >= 100 ? AllocationStatus.Full
                    : AllocationStatus.Partial,
                Skills = e.EmployeeSkills.Select(es => es.Skill?.SkillName ?? string.Empty).ToList()
            };
        }).ToList();

        return Ok(new PagedResponse<EmployeeSummaryResponse>
        {
            Data = data,
            Pagination = PaginationMeta.Create(page, limit, totalRecords)
        });
    }

    /// <summary>
    /// Get employee by code with allocation details.
    /// </summary>
    /// <remarks>
    /// Returns employee detail with current allocation status.
    /// By default, only active and upcoming allocations are included.
    /// Use includeEnded=true to include past allocations. includeRemoved=true (HR only) includes soft-deleted allocations.
    /// </remarks>
    /// <param name="empCode">Unique employee business code.</param>
    /// <param name="windowFrom">Start of availability window (default: today).</param>
    /// <param name="windowTo">End of availability window (default: today).</param>
    /// <param name="includeEnded">Include ended (past) allocations.</param>
    /// <param name="includeRemoved">HR only. Include soft-deleted allocations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Employee detail with allocations.</returns>
    [HttpGet("{empCode}")]
    [ProducesResponseType(typeof(EmployeeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailResponse>> GetByCode(
        string empCode,
        [FromQuery] DateOnly? windowFrom = null,
        [FromQuery] DateOnly? windowTo = null,
        [FromQuery] bool includeEnded = false,
        [FromQuery] bool includeRemoved = false,
        CancellationToken ct = default)
    {
        var employee = await _employeeRepo.GetByEmpCodeAsync(empCode, ct);
        if (employee is null) return NotFound();

        return Ok(await BuildEmployeeDetailResponse(employee, includeEnded, includeRemoved, ct));
    }

    /// <summary>
    /// Get authenticated user's own profile.
    /// </summary>
    /// <remarks>
    /// Returns the full employee detail for the currently authenticated user.
    /// Uses the employee ID from the Keycloak token sub claim. Available to all roles.
    /// </remarks>
    /// <param name="windowFrom">Start of availability window (default: today).</param>
    /// <param name="windowTo">End of availability window (default: today).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Authenticated user's employee profile.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(EmployeeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmployeeDetailResponse>> GetMe(
        [FromQuery] DateOnly? windowFrom = null,
        [FromQuery] DateOnly? windowTo = null,
        CancellationToken ct = default)
    {
        var employee = await _employeeRepo.GetByIdAsync(_currentUser.EmployeeId, ct);
        if (employee is null) return NotFound();

        return Ok(await BuildEmployeeDetailResponse(employee, false, false, ct));
    }

    /// <summary>
    /// Create a new employee (HR only).
    /// </summary>
    /// <remarks>
    /// Creates an employee with the specified details. empCode and email must be unique.
    /// Optional reportsToEmpCode sets the reporting chain. Circular reporting is blocked (422).
    /// </remarks>
    /// <param name="command">Employee creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created employee detail.</returns>
    [HttpPost]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(EmployeeDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeDetailResponse>> Create(
        [FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var employeeId = await _mediator.Send(command, ct);
        var employee = await _employeeRepo.GetByIdAsync(employeeId, ct);

        var detail = employee is not null
            ? await BuildEmployeeDetailResponse(employee, false, false, ct)
            : null;

        return CreatedAtAction(nameof(GetByCode),
            new { empCode = command.EmpCode }, detail);
    }

    /// <summary>
    /// Update employee (HR only). EmpCode is immutable.
    /// </summary>
    /// <remarks>
    /// Updates employee details. Set isActive=false to deactivate.
    /// Email uniqueness is enforced. Circular reporting chain is blocked (422).
    /// Skills are synced — existing skills not in the list are removed.
    /// </remarks>
    /// <param name="empCode">Employee code to update.</param>
    /// <param name="request">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated employee detail.</returns>
    [HttpPut("{empCode}")]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(EmployeeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmployeeDetailResponse>> Update(
        string empCode,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken ct)
    {
        var command = new UpdateEmployeeCommand
        {
            EmpCode = empCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Designation = request.Designation,
            Role = request.Role,
            ReportsToEmpCode = request.ReportsToEmpCode,
            SkillIds = request.SkillIds ?? [],
            IsActive = request.IsActive
        };

        await _mediator.Send(command, ct);

        var employee = await _employeeRepo.GetByEmpCodeAsync(empCode, ct);
        var detail = employee is not null
            ? await BuildEmployeeDetailResponse(employee, false, false, ct)
            : null;

        return Ok(detail);
    }

    private async Task<EmployeeDetailResponse> BuildEmployeeDetailResponse(
        Domain.Entities.Employee employee, bool includeEnded, bool includeRemoved,
        CancellationToken ct)
    {
        var allocations = await _allocationRepo.GetByEmployeeAsync(employee.Id, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var filtered = allocations
            .Where(a =>
            {
                if (a.DeletedAt != null && !includeRemoved) return false;
                if (!includeEnded && a.ToDate.HasValue && a.ToDate.Value < today) return false;
                return true;
            })
            .ToList();

        var activeAllocations = allocations
            .Where(a => a.DeletedAt == null && a.FromDate <= today && (a.ToDate == null || a.ToDate >= today))
            .ToList();
        var totalPct = activeAllocations.Sum(a => a.Percentage);
        var availability = Math.Max(0, 100 - totalPct);

        return new EmployeeDetailResponse
        {
            EmployeeId = employee.Id,
            EmpCode = employee.EmpCode,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Designation = employee.Designation,
            Role = employee.Role,
            IsActive = employee.IsActive,
            Email = employee.Email,
            AvailabilityPercentage = availability,
            AllocationStatus = totalPct == 0 ? AllocationStatus.Bench
                : totalPct >= 100 ? AllocationStatus.Full
                : AllocationStatus.Partial,
            ReportsToId = employee.ReportsToId,
            ReportsToEmpCode = employee.ReportsTo?.EmpCode,
            ReportsToName = employee.ReportsTo is not null
                ? $"{employee.ReportsTo.FirstName} {employee.ReportsTo.LastName}" : null,
            Skills = employee.EmployeeSkills.Select(es => es.Skill?.SkillName ?? string.Empty).ToList(),
            SkillDetails = employee.EmployeeSkills.Select(es => new SkillDetailItem
            {
                SkillId = es.SkillId,
                SkillName = es.Skill?.SkillName ?? string.Empty,
                IsActive = es.Skill?.IsActive ?? false
            }).ToList(),
            CurrentAllocations = filtered.Select(a => new AllocationDetailResponse
            {
                AllocationId = a.Id,
                EmployeeId = a.EmployeeId,
                EmpCode = employee.EmpCode,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                ProjectId = a.ProjectId,
                ProjectCode = a.Project?.ProjectCode ?? string.Empty,
                ProjectName = a.Project?.ProjectName ?? string.Empty,
                Percentage = a.Percentage,
                FromDate = a.FromDate,
                ToDate = a.ToDate,
                CreatedAt = a.CreatedAt
            }).ToList(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }
}

public sealed record UpdateEmployeeRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Designation { get; init; }
    public required EmployeeRole Role { get; init; }
    public string? ReportsToEmpCode { get; init; }
    public List<Guid>? SkillIds { get; init; }
    public bool? IsActive { get; init; }
}
