using PAMS.Domain.Entities;

namespace PAMS.Domain.Repositories;

public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Skill>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Skill>> GetFilteredAsync(bool? isActive, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string skillName, CancellationToken ct = default);
    Task AddAsync(Skill skill, CancellationToken ct = default);
    void Update(Skill skill);
}
