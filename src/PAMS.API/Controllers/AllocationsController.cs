using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Allocations;
using PAMS.Application.DTOs.Allocations;
using PAMS.Application.DTOs.Common;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Manage project allocations — create, update, stop, remove, and check capacity.
/// </summary>
[ApiController]
[Route("api/v1/allocations")]
[Authorize]
[Tags("Allocations")]
[Produces("application/json")]
public sealed class AllocationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAllocationRepository _allocationRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IExportService _exportService;

    public AllocationsController(
        IMediator mediator,
        IAllocationRepository allocationRepo,
        IEmployeeRepository employeeRepo,
        IProjectRepository projectRepo,
        ICurrentUserService currentUser,
        IExportService exportService)
    {
        _mediator = mediator;
        _allocationRepo = allocationRepo;
        _employeeRepo = employeeRepo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
        _exportService = exportService;
    }

    /// <summary>
    /// List allocations with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AllocationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<AllocationDetailResponse>>> List(
        [FromQuery] string? empCode,
        [FromQuery] string? projectCode,
        [FromQuery] string? projectManagerEmpCode,
        [FromQuery] string? status,
        [FromQuery] bool? billable,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        // Role scoping
        if (_currentUser.Role == EmployeeRole.Staff)
        {
            // Staff can only see own allocations
            if (empCode is not null && empCode != _currentUser.EmpCode)
                return Forbid();
            empCode = _currentUser.EmpCode;
        }
        else if (_currentUser.Role == EmployeeRole.ProjectManager)
        {
            // PM can only see allocations on their own projects
            if (projectManagerEmpCode is not null && projectManagerEmpCode != _currentUser.EmpCode)
                return Forbid();
            projectManagerEmpCode = _currentUser.EmpCode;
        }
        // HR sees all — no additional filter

        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var allocations = await _allocationRepo.GetFilteredAsync(
            empCode, projectCode, projectManagerEmpCode, status, billable, page, limit, sort, ct);
        var totalRecords = await _allocationRepo.GetFilteredCountAsync(
            empCode, projectCode, projectManagerEmpCode, status, billable, ct);

        var data = allocations
            .Select(a => AllocationDetailResponse.MapFrom(a))
            .ToList();

        return Ok(new PagedResponse<AllocationDetailResponse>
        {
            Data = data,
            Pagination = PaginationMeta.Create(page, limit, totalRecords)
        });
    }

    /// <summary>
    /// Export allocations as PDF or Excel.
    /// </summary>
    /// <remarks>
    /// Returns all matching allocations (no pagination) as a downloadable file.
    /// Accepts the same filter and sort parameters as the list endpoint.
    /// Role-based scoping applies: Staff sees own allocations, PM sees own projects, HR sees all.
    /// An optional JSON body may contain a column name map where keys are field names
    /// and values are display labels. Only mapped columns appear in the export.
    /// If no body is sent, all default columns are included.
    /// </remarks>
    /// <param name="empCode">Filter by allocated employee code.</param>
    /// <param name="projectCode">Filter by project code.</param>
    /// <param name="projectManagerEmpCode">Filter allocations on projects managed by this PM.</param>
    /// <param name="status">Filter by computed allocation status (Active, Ended, Upcoming).</param>
    /// <param name="billable">Filter by resource-level billable flag.</param>
    /// <param name="sort">Sort field. Prefix with - for descending (e.g. -fromDate).</param>
    /// <param name="ext">Export format: pdf or xls.</param>
    /// <param name="columns">Optional column name map. Keys = field names, values = display labels.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File download (PDF or Excel).</returns>
    /// <response code="200">File download.</response>
    /// <response code="400">Invalid export format or invalid sort field.</response>
    /// <response code="401">Missing or invalid authentication token.</response>
    /// <response code="403">Insufficient permissions (role-scoped).</response>
    [HttpPost("export")]
    [Produces("application/pdf", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] string? empCode,
        [FromQuery] string? projectCode,
        [FromQuery] string? projectManagerEmpCode,
        [FromQuery] string? status,
        [FromQuery] bool? billable,
        [FromQuery] string? sort,
        [FromQuery] string ext,
        [FromBody] Dictionary<string, string>? columns = null,
        CancellationToken ct = default)
    {
        var format = ext?.ToLowerInvariant();
        if (format != "pdf" && format != "xls")
            return Problem(
                detail: "ext must be 'pdf' or 'xls'.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://pams.internal/errors/ERR_VALIDATION");

        // Role scoping (same as List)
        if (_currentUser.Role == EmployeeRole.Staff)
        {
            if (empCode is not null && empCode != _currentUser.EmpCode)
                return Forbid();
            empCode = _currentUser.EmpCode;
        }
        else if (_currentUser.Role == EmployeeRole.ProjectManager)
        {
            if (projectManagerEmpCode is not null && projectManagerEmpCode != _currentUser.EmpCode)
                return Forbid();
            projectManagerEmpCode = _currentUser.EmpCode;
        }

        var allocations = await _allocationRepo.GetFilteredAllAsync(
            empCode, projectCode, projectManagerEmpCode, status, billable, sort, ct);

        var data = allocations
            .Select(a => AllocationDetailResponse.MapFrom(a))
            .ToList();

        var result = await _exportService.GenerateAllocationsAsync(
            data, format == "xls" ? "xlsx" : format, columns, ct);

        return File(result.FileBytes, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Create a new allocation (HR, PM).
    /// </summary>
    /// <remarks>
    /// Allocates an employee to a project for a percentage during a date range.
    /// Validates capacity (total allocations cannot exceed 100%).
    /// Employee must be active and project must be active.
    /// </remarks>
    /// <param name="command">Allocation creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created allocation detail.</returns>
    [HttpPost]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(AllocationDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AllocationDetailResponse>> Create(
        [FromBody] CreateAllocationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById),
            new { allocationId = result.AllocationId }, result);
    }

    /// <summary>
    /// Get allocation by ID.
    /// </summary>
    /// <remarks>
    /// Returns the full allocation detail including employee and project information.
    /// </remarks>
    /// <param name="allocationId">Allocation unique identifier (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Allocation detail.</returns>
    [HttpGet("{allocationId:guid}")]
    [ProducesResponseType(typeof(AllocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllocationDetailResponse>> GetById(
        Guid allocationId, CancellationToken ct)
    {
        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation is null) return NotFound();

        var employee = await _employeeRepo.GetByIdAsync(allocation.EmployeeId, ct);
        var project = await _projectRepo.GetByIdAsync(allocation.ProjectId, ct);

        return Ok(AllocationDetailResponse.MapFrom(allocation, employee, project));
    }

    /// <summary>
    /// Update allocation percentage or date range (HR, PM).
    /// </summary>
    /// <remarks>
    /// Updates the percentage, fromDate, or toDate of an allocation.
    /// Validates that total allocation for the employee does not exceed 100%.
    /// </remarks>
    /// <param name="allocationId">Allocation unique identifier (GUID).</param>
    /// <param name="request">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated allocation detail.</returns>
    [HttpPut("{allocationId:guid}")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(AllocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AllocationDetailResponse>> Update(
        Guid allocationId,
        [FromBody] UpdateAllocationRequest request,
        CancellationToken ct)
    {
        var command = new UpdateAllocationCommand
        {
            AllocationId = allocationId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Percentage = request.Percentage,
            Billable = request.Billable,
            ProjectRole = request.ProjectRole
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Stop an allocation (HR, PM). Request body: {"action": "stop"}.
    /// </summary>
    /// <remarks>
    /// Sets the allocation toDate to today, effectively ending it. Only "stop" action is supported.
    /// </remarks>
    /// <param name="allocationId">Allocation unique identifier (GUID).</param>
    /// <param name="request">Stop request body with action field.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated allocation detail with toDate set to today.</returns>
    [HttpPatch("{allocationId:guid}")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(AllocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Stop(
        Guid allocationId,
        [FromBody] StopAllocationRequest request,
        CancellationToken ct)
    {
        if (!string.Equals(request.Action, "stop", StringComparison.OrdinalIgnoreCase))
            return Problem(
                detail: "Only 'stop' action is supported.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://pams.internal/errors/ERR_VALIDATION");

        var command = new StopAllocationCommand { AllocationId = allocationId };
        await _mediator.Send(command, ct);

        // Return the updated allocation
        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation is null) return NotFound();

        var employee = await _employeeRepo.GetByIdAsync(allocation.EmployeeId, ct);
        var project = await _projectRepo.GetByIdAsync(allocation.ProjectId, ct);

        return Ok(AllocationDetailResponse.MapFrom(allocation, employee, project));
    }

    /// <summary>
    /// Soft-delete a past allocation (HR, PM).
    /// </summary>
    /// <remarks>
    /// Marks a past allocation as deleted. Only allocations that have already ended can be removed.
    /// Active or future allocations must be stopped first.
    /// </remarks>
    /// <param name="allocationId">Allocation unique identifier (GUID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The soft-deleted allocation detail.</returns>
    [HttpDelete("{allocationId:guid}")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(AllocationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult> Remove(
        Guid allocationId, CancellationToken ct)
    {
        var command = new RemoveAllocationCommand { AllocationId = allocationId };
        await _mediator.Send(command, ct);

        var allocation = await _allocationRepo.GetByIdAsync(allocationId, ct);
        if (allocation is null) return NotFound();

        var employee = await _employeeRepo.GetByIdAsync(allocation.EmployeeId, ct);
        var project = await _projectRepo.GetByIdAsync(allocation.ProjectId, ct);

        return Ok(AllocationDetailResponse.MapFrom(allocation, employee, project));
    }

    /// <summary>
    /// Check allocation capacity for an employee in a date window.
    /// </summary>
    /// <remarks>
    /// Returns how much allocation capacity (percentage) an employee has available
    /// in a given date window. Useful before creating or updating allocations.
    /// </remarks>
    /// <param name="empCode">Employee code to check capacity for.</param>
    /// <param name="fromDate">Start of the date window.</param>
    /// <param name="toDate">End of the date window (optional, defaults to open-ended).</param>
    /// <param name="excludeAllocationId">Allocation ID to exclude from the calculation (for updates).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Capacity check result with available percentage and allocation status.</returns>
    [HttpGet("capacity-check")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(CapacityCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapacityCheckResponse>> CapacityCheck(
        [FromQuery] string empCode,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] Guid? excludeAllocationId = null,
        CancellationToken ct = default)
    {
        var employee = await _employeeRepo.GetByEmpCodeAsync(empCode, ct);
        if (employee is null) return NotFound();

        var currentTotal = await _allocationRepo.GetOverlappingTotalPercentageAsync(
            employee.Id, fromDate, toDate, excludeAllocationId, ct);

        var available = Math.Max(0, 100 - currentTotal);
        var status = currentTotal == 0 ? AllocationStatus.Bench
            : currentTotal >= 100 ? AllocationStatus.Full
            : AllocationStatus.Partial;

        return Ok(new CapacityCheckResponse
        {
            EmpCode = employee.EmpCode,
            WindowFrom = fromDate,
            WindowTo = toDate,
            CurrentTotalPercentage = currentTotal,
            AvailablePercentage = available,
            AllocationStatus = status
        });
    }
}

public sealed record UpdateAllocationRequest
{
    public required DateOnly FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public required int Percentage { get; init; }
    public bool? Billable { get; init; }
    public string? ProjectRole { get; init; }
}

public sealed record StopAllocationRequest
{
    public required string Action { get; init; }
}
