namespace PAMS.Application.DTOs.Skills;

public sealed record SkillResponse
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    /// <summary>Whether the skill is active in the master skill list. Inactive skills are hidden from selection but retained on existing employee profiles.</summary>
    public bool IsActive { get; init; }
}
