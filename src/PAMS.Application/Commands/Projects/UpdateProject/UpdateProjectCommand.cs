using MediatR;
using PAMS.Application.DTOs.Projects;
using PAMS.Domain.Enums;

namespace PAMS.Application.Commands.Projects;

/// <summary>
/// Command to update a project (FR-004). Project code is immutable.
/// </summary>
public sealed record UpdateProjectCommand : IRequest<ProjectDetailResponse>
{
    public required string ProjectCode { get; init; }
    public required string ProjectName { get; init; }
    public string? ProjectManagerEmpCode { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required ProjectStatus Status { get; init; }
    public required bool Billable { get; init; }
    public bool? IsActive { get; init; }
}
