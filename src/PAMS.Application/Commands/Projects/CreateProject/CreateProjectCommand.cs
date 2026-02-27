using MediatR;
using PAMS.Application.DTOs.Projects;

namespace PAMS.Application.Commands.Projects;

/// <summary>
/// Command to create a new project (FR-003).
/// </summary>
public sealed record CreateProjectCommand : IRequest<ProjectDetailResponse>
{
    public required string ProjectCode { get; init; }
    public required string ProjectName { get; init; }
    public required string AccountCode { get; init; }
    public string? ProjectManagerEmpCode { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool? Billable { get; init; }
}
