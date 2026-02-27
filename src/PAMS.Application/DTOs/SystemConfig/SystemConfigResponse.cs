namespace PAMS.Application.DTOs.SystemConfig;

public sealed record SystemConfigResponse
{
    public int MinAllocationPct { get; init; }
    public int AllocationIncrement { get; init; }
    public DateTime UpdatedAt { get; init; }
}
