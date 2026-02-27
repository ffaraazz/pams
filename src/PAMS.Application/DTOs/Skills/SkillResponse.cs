namespace PAMS.Application.DTOs.Skills;

public sealed record SkillResponse
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
