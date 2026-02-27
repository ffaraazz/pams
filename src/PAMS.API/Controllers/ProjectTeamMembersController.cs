using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.ProjectTeamMembers;
using PAMS.Application.DTOs.ProjectTeamMembers;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Manage team-lead / reportee assignments within a project.
/// </summary>
[ApiController]
[Route("api/v1/projects/{projectCode}/team-members")]
[Authorize(Policy = "CanAllocate")]
[Tags("Project Team Members")]
[Produces("application/json")]
public sealed class ProjectTeamMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IProjectRepository _projectRepo;
    private readonly IProjectTeamMemberRepository _teamMemberRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ICurrentUserService _currentUser;

    public ProjectTeamMembersController(
        IMediator mediator,
        IProjectRepository projectRepo,
        IProjectTeamMemberRepository teamMemberRepo,
        IEmployeeRepository employeeRepo,
        ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _projectRepo = projectRepo;
        _teamMemberRepo = teamMemberRepo;
        _employeeRepo = employeeRepo;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List team-lead / reportee assignments for a project (HR, PM).
    /// </summary>
    /// <remarks>
    /// Returns all team-lead / reportee assignments for the specified project.
    /// PMs can only view their own projects.
    /// </remarks>
    /// <param name="projectCode">Unique project business code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of team member assignments.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectTeamMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProjectTeamMemberResponse>>> List(
        string projectCode, CancellationToken ct)
    {
        var project = await _projectRepo.GetByCodeAsync(projectCode, ct);
        if (project is null) return NotFound();

        // PM scope check
        if (_currentUser.Role == EmployeeRole.ProjectManager
            && project.ProjectManagerId != _currentUser.EmployeeId)
        {
            return Forbid();
        }

        var members = await _teamMemberRepo.GetByProjectAsync(project.Id, ct);

        var data = new List<ProjectTeamMemberResponse>();
        foreach (var m in members)
        {
            var teamLead = await _employeeRepo.GetByIdAsync(m.TeamLeadId, ct);
            var reportee = await _employeeRepo.GetByIdAsync(m.ReporteeId, ct);

            data.Add(new ProjectTeamMemberResponse
            {
                Id = m.Id,
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                TeamLeadId = m.TeamLeadId,
                TeamLeadEmpCode = teamLead?.EmpCode ?? string.Empty,
                TeamLeadFullName = teamLead is not null
                    ? $"{teamLead.FirstName} {teamLead.LastName}" : string.Empty,
                ReporteeId = m.ReporteeId,
                ReporteeEmpCode = reportee?.EmpCode ?? string.Empty,
                ReporteeFullName = reportee is not null
                    ? $"{reportee.FirstName} {reportee.LastName}" : string.Empty,
                CreatedAt = m.CreatedAt
            });
        }

        return Ok(data);
    }

    /// <summary>
    /// Add a team-lead / reportee assignment (HR, PM).
    /// </summary>
    /// <remarks>
    /// Assigns a reportee employee to a team lead within the project.
    /// Both employees must be active and allocated to the project.
    /// Duplicate assignment returns 409. Self-assignment returns 422.
    /// </remarks>
    /// <param name="projectCode">Unique project business code.</param>
    /// <param name="request">Team member assignment request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created team member assignment.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectTeamMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectTeamMemberResponse>> Add(
        string projectCode,
        [FromBody] AddTeamMemberRequest request,
        CancellationToken ct)
    {
        var command = new AddProjectTeamMemberCommand
        {
            ProjectCode = projectCode,
            TeamLeadEmpCode = request.TeamLeadEmpCode,
            ReporteeEmpCode = request.ReporteeEmpCode
        };

        await _mediator.Send(command, ct);

        // Fetch the created member to return a response
        var project = await _projectRepo.GetByCodeAsync(projectCode, ct);
        if (project is null) return NotFound();

        var teamLead = await _employeeRepo.GetByEmpCodeAsync(request.TeamLeadEmpCode, ct);
        var reportee = await _employeeRepo.GetByEmpCodeAsync(request.ReporteeEmpCode, ct);

        if (teamLead is null || reportee is null) return NotFound();

        var members = await _teamMemberRepo.GetByProjectAsync(project.Id, ct);
        var created = members.FirstOrDefault(m =>
            m.TeamLeadId == teamLead.Id && m.ReporteeId == reportee.Id);

        if (created is null) return NotFound();

        var response = new ProjectTeamMemberResponse
        {
            Id = created.Id,
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode,
            TeamLeadId = teamLead.Id,
            TeamLeadEmpCode = teamLead.EmpCode,
            TeamLeadFullName = $"{teamLead.FirstName} {teamLead.LastName}",
            ReporteeId = reportee.Id,
            ReporteeEmpCode = reportee.EmpCode,
            ReporteeFullName = $"{reportee.FirstName} {reportee.LastName}",
            CreatedAt = created.CreatedAt
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Remove a team-lead / reportee assignment (HR, PM).
    /// </summary>
    /// <remarks>
    /// Removes the specified team assignment from the project.
    /// Both teamLeadEmpCode and reporteeEmpCode must match an existing assignment.
    /// </remarks>
    /// <param name="projectCode">Unique project business code.</param>
    /// <param name="teamLeadEmpCode">Team lead employee code.</param>
    /// <param name="reporteeEmpCode">Reportee employee code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{teamLeadEmpCode}/{reporteeEmpCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(
        string projectCode,
        string teamLeadEmpCode,
        string reporteeEmpCode,
        CancellationToken ct)
    {
        var command = new RemoveProjectTeamMemberCommand
        {
            ProjectCode = projectCode,
            TeamLeadEmpCode = teamLeadEmpCode,
            ReporteeEmpCode = reporteeEmpCode
        };

        await _mediator.Send(command, ct);
        return NoContent();
    }
}

public sealed record AddTeamMemberRequest
{
    public required string TeamLeadEmpCode { get; init; }
    public required string ReporteeEmpCode { get; init; }
}
