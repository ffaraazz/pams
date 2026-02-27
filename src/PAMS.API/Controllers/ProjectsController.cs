using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Projects;
using PAMS.Application.DTOs.Common;
using PAMS.Application.DTOs.Projects;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Project management endpoints. Projects belong to accounts and have allocations.
/// </summary>
[ApiController]
[Route("api/v1/projects")]
[Authorize]
[Produces("application/json")]
[Tags("Projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProjectRepository _projectRepo;

    public ProjectsController(IMediator mediator, IProjectRepository projectRepo)
    {
        _mediator = mediator;
        _projectRepo = projectRepo;
    }

    /// <summary>
    /// List projects with optional search and filters.
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of projects. Supports search by project code or name,
    /// and filtering by account code, status, active status, billable flag, and PM employee code.
    /// Use projectManagerEmpCode with the current user's empCode to get "my projects".
    /// </remarks>
    /// <param name="search">Search by project code or name (partial match, case-insensitive).</param>
    /// <param name="accountCode">Filter by account code.</param>
    /// <param name="status">Filter by project status (Upcoming, Active, Completed).</param>
    /// <param name="isActive">Filter by active status.</param>
    /// <param name="billable">Filter by billable flag.</param>
    /// <param name="projectManagerEmpCode">Filter projects where specified employee is PM.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="limit">Items per page (default: 10, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of project summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProjectSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<ProjectSummaryResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? accountCode,
        [FromQuery] ProjectStatus? status,
        [FromQuery] bool? isActive,
        [FromQuery] bool? billable,
        [FromQuery] string? projectManagerEmpCode,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var projects = await _projectRepo.GetFilteredAsync(
            search, accountCode, status, isActive, billable, projectManagerEmpCode, page, limit, ct);
        var totalRecords = await _projectRepo.GetFilteredCountAsync(
            search, accountCode, status, isActive, billable, projectManagerEmpCode, ct);

        var data = projects.Select(p => new ProjectSummaryResponse
        {
            ProjectId = p.Id,
            ProjectCode = p.ProjectCode,
            ProjectName = p.ProjectName,
            AccountId = p.AccountId,
            AccountCode = p.Account?.AccountCode ?? string.Empty,
            AccountName = p.Account?.AccountName ?? string.Empty,
            ProjectManagerId = p.ProjectManagerId,
            ProjectManagerEmpCode = p.ProjectManager?.EmpCode,
            ProjectManagerName = p.ProjectManager is not null
                ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}" : null,
            Status = p.Status,
            Billable = p.Billable,
            IsActive = p.IsActive,
            StartDate = p.StartDate,
            EndDate = p.EndDate
        }).ToList();

        return Ok(new PagedResponse<ProjectSummaryResponse>
        {
            Data = data,
            Pagination = PaginationMeta.Create(page, limit, totalRecords)
        });
    }

    /// <summary>
    /// Get project by code.
    /// </summary>
    /// <remarks>
    /// Returns detailed project information including account and project manager details.
    /// </remarks>
    /// <param name="projectCode">Unique project business code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Project detail.</returns>
    [HttpGet("{projectCode}")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailResponse>> GetByCode(
        string projectCode, CancellationToken ct)
    {
        var p = await _projectRepo.GetByCodeAsync(projectCode, ct);
        if (p is null) return NotFound();

        return Ok(new ProjectDetailResponse
        {
            ProjectId = p.Id,
            ProjectCode = p.ProjectCode,
            ProjectName = p.ProjectName,
            AccountId = p.AccountId,
            AccountCode = p.Account?.AccountCode ?? string.Empty,
            AccountName = p.Account?.AccountName ?? string.Empty,
            ProjectManagerId = p.ProjectManagerId,
            ProjectManagerEmpCode = p.ProjectManager?.EmpCode,
            ProjectManagerName = p.ProjectManager is not null
                ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}" : null,
            Status = p.Status,
            Billable = p.Billable,
            IsActive = p.IsActive,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new project (HR only).
    /// </summary>
    /// <remarks>
    /// Creates a project under an account with an assigned PM. Project code must be unique.
    /// Billable defaults to true for Client accounts, false otherwise.
    /// </remarks>
    /// <param name="command">Project creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created project detail.</returns>
    [HttpPost]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDetailResponse>> Create(
        [FromBody] CreateProjectCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByCode),
            new { projectCode = result.ProjectCode }, result);
    }

    /// <summary>
    /// Update project (HR or PM — PM scoped to own projects).
    /// </summary>
    /// <remarks>
    /// Project code is immutable. Set isActive=false to deactivate.
    /// PM can only update projects they manage. Both HR and PM can modify status.
    /// </remarks>
    /// <param name="projectCode">Project code to update.</param>
    /// <param name="request">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated project detail.</returns>
    [HttpPut("{projectCode}")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailResponse>> Update(
        string projectCode,
        [FromBody] UpdateProjectRequest request,
        CancellationToken ct)
    {
        var command = new UpdateProjectCommand
        {
            ProjectCode = projectCode,
            ProjectName = request.ProjectName,
            ProjectManagerEmpCode = request.ProjectManagerEmpCode,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            Billable = request.Billable,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

public sealed record UpdateProjectRequest
{
    public required string ProjectName { get; init; }
    public string? ProjectManagerEmpCode { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required ProjectStatus Status { get; init; }
    public required bool Billable { get; init; }
    public bool? IsActive { get; init; }
}
