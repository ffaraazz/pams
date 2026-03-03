using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Allocations;
using PAMS.Application.DTOs.Allocations;
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

    public AllocationsController(
        IMediator mediator,
        IAllocationRepository allocationRepo,
        IEmployeeRepository employeeRepo,
        IProjectRepository projectRepo)
    {
        _mediator = mediator;
        _allocationRepo = allocationRepo;
        _employeeRepo = employeeRepo;
        _projectRepo = projectRepo;
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
            Percentage = request.Percentage
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
            return BadRequest("Only 'stop' action is supported.");

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
}

public sealed record StopAllocationRequest
{
    public required string Action { get; init; }
}
