using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.SystemConfig;
using PAMS.Application.DTOs.SystemConfig;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// System configuration — manage allocation rules (HR only).
/// </summary>
[ApiController]
[Route("api/v1/system-config")]
[Authorize(Policy = "HROnly")]
[Tags("System Config")]
[Produces("application/json")]
public sealed class SystemConfigController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISystemConfigRepository _configRepo;

    public SystemConfigController(IMediator mediator, ISystemConfigRepository configRepo)
    {
        _mediator = mediator;
        _configRepo = configRepo;
    }

    /// <summary>
    /// Get current system configuration (HR only).
    /// </summary>
    /// <remarks>
    /// Returns the current system-wide allocation configuration values including
    /// minimum allocation percentage and allocation increment.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current system configuration.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SystemConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemConfigResponse>> Get(CancellationToken ct)
    {
        var config = await _configRepo.GetAsync(ct);

        return Ok(new SystemConfigResponse
        {
            MinAllocationPct = config.MinAllocationPercentage,
            AllocationIncrement = config.AllocationIncrement,
            UpdatedAt = config.UpdatedAt
        });
    }

    /// <summary>
    /// Update system configuration (HR only).
    /// </summary>
    /// <remarks>
    /// Updates the system-wide allocation rules. MinAllocationPct must be between 1 and 100.
    /// AllocationIncrement must be between 1 and 100 and MinAllocationPct must be a multiple of AllocationIncrement.
    /// </remarks>
    /// <param name="command">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated system configuration.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(SystemConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SystemConfigResponse>> Update(
        [FromBody] UpdateSystemConfigCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
