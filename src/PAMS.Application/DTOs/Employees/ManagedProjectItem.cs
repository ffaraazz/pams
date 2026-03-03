using PAMS.Domain.Enums;

namespace PAMS.Application.DTOs.Employees;

public sealed record ManagedProjectItem
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string AccountCode { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string ManagementRole { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public int ActiveResourceCount { get; init; }
}
