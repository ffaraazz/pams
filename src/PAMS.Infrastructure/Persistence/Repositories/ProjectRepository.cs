using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly PamsDbContext _context;

    public ProjectRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Account)
            .Include(p => p.ProjectManager)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetByCodeAsync(string projectCode, CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Account)
            .Include(p => p.ProjectManager)
            .FirstOrDefaultAsync(p => p.ProjectCode.ToLower() == projectCode.ToLower(), ct);

    public async Task<bool> CodeExistsAsync(string projectCode, CancellationToken ct = default)
        => await _context.Projects.AnyAsync(
            p => p.ProjectCode.ToLower() == projectCode.ToLower(), ct);

    public async Task<IReadOnlyList<Project>> GetFilteredAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        int page, int limit, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, accountCode, status, isActive, billable, pmEmpCode);
        return await query
            .OrderBy(p => p.ProjectName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(p => p.Account)
            .Include(p => p.ProjectManager)
            .ToListAsync(ct);
    }

    public async Task<int> GetFilteredCountAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, accountCode, status, isActive, billable, pmEmpCode);
        return await query.CountAsync(ct);
    }

    public async Task AddAsync(Project project, CancellationToken ct = default)
        => await _context.Projects.AddAsync(project, ct);

    public void Update(Project project)
        => _context.Projects.Update(project);

    private IQueryable<Project> BuildFilteredQuery(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode)
    {
        var query = _context.Projects.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (billable.HasValue)
            query = query.Where(p => p.Billable == billable.Value);

        if (!string.IsNullOrWhiteSpace(accountCode))
            query = query.Where(p => p.Account.AccountCode.ToLower() == accountCode.ToLower());

        if (!string.IsNullOrWhiteSpace(pmEmpCode))
            query = query.Where(p => p.ProjectManager != null &&
                p.ProjectManager.EmpCode.ToLower() == pmEmpCode.ToLower());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(p =>
                p.ProjectCode.ToLower().Contains(lowerSearch) ||
                p.ProjectName.ToLower().Contains(lowerSearch));
        }

        return query;
    }
}
