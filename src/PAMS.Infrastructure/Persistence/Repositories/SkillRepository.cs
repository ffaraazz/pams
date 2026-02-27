using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class SkillRepository : ISkillRepository
{
    private readonly PamsDbContext _context;

    public SkillRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Skill>> GetAllAsync(CancellationToken ct = default)
        => await _context.Skills.OrderBy(s => s.SkillName).ToListAsync(ct);

    public async Task<IReadOnlyList<Skill>> GetFilteredAsync(bool? isActive, CancellationToken ct = default)
    {
        var query = _context.Skills.AsQueryable();
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);
        return await query.OrderBy(s => s.SkillName).ToListAsync(ct);
    }

    public async Task<bool> NameExistsAsync(string skillName, CancellationToken ct = default)
        => await _context.Skills.AnyAsync(
            s => s.SkillName.ToLower() == skillName.ToLower(), ct);

    public async Task AddAsync(Skill skill, CancellationToken ct = default)
        => await _context.Skills.AddAsync(skill, ct);

    public void Update(Skill skill)
        => _context.Skills.Update(skill);
}
