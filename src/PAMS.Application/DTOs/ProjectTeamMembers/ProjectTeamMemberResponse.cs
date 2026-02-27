namespace PAMS.Application.DTOs.ProjectTeamMembers;

public sealed record ProjectTeamMemberResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public Guid TeamLeadId { get; init; }
    public string TeamLeadEmpCode { get; init; } = string.Empty;
    public string TeamLeadFullName { get; init; } = string.Empty;
    public Guid ReporteeId { get; init; }
    public string ReporteeEmpCode { get; init; } = string.Empty;
    public string ReporteeFullName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
