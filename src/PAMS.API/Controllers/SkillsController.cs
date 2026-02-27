using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Skills;
using PAMS.Application.DTOs.Skills;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Manage skills — list, create, and update skill definitions.
/// </summary>
[ApiController]
[Route("api/v1/skills")]
[Authorize]
[Tags("Skills")]
[Produces("application/json")]
public sealed class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISkillRepository _skillRepo;

    public SkillsController(IMediator mediator, ISkillRepository skillRepo)
    {
        _mediator = mediator;
        _skillRepo = skillRepo;
    }

    /// <summary>
    /// List all skills with optional active filter.
    /// </summary>
    /// <remarks>
    /// Returns all skills. Optionally filter by active status.
    /// Available to all authenticated users.
    /// </remarks>
    /// <param name="isActive">Filter by active status (true/false). Omit for all.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of skills.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SkillResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SkillResponse>>> List(
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
    {
        var skills = await _skillRepo.GetFilteredAsync(isActive, ct);

        var data = skills.Select(s => new SkillResponse
        {
            SkillId = s.Id,
            SkillName = s.SkillName,
            IsActive = s.IsActive
        }).ToList();

        return Ok(data);
    }

    /// <summary>
    /// Create a skill (HR only).
    /// </summary>
    /// <remarks>
    /// Creates a new skill definition. Skill name must be unique (case-insensitive). HR only.
    /// </remarks>
    /// <param name="command">Skill creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created skill.</returns>
    [HttpPost]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(SkillResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SkillResponse>> Create(
        [FromBody] CreateSkillCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update skill name or active status (HR only).
    /// </summary>
    /// <remarks>
    /// Updates the skill name and/or active status. Skill name uniqueness enforced. HR only.
    /// </remarks>
    /// <param name="skillId">Skill unique identifier (GUID).</param>
    /// <param name="request">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated skill.</returns>
    [HttpPut("{skillId:guid}")]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(SkillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SkillResponse>> Update(
        Guid skillId,
        [FromBody] UpdateSkillRequest request,
        CancellationToken ct)
    {
        var command = new UpdateSkillCommand
        {
            SkillId = skillId,
            SkillName = request.SkillName,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

public sealed record UpdateSkillRequest
{
    public string? SkillName { get; init; }
    public bool? IsActive { get; init; }
}
