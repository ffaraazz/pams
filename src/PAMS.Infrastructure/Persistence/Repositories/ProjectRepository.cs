using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PAMS.Application.Exceptions;
using PAMS.Application.Helpers;
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
            .Include(p => p.Allocations)
                .ThenInclude(a => a.Employee)
            .Include(p => p.TeamMembers)
                .ThenInclude(tm => tm.TeamLead)
            .Include(p => p.TeamMembers)
                .ThenInclude(tm => tm.Reportee)
            .FirstOrDefaultAsync(p => p.ProjectCode.ToLower() == projectCode.ToLower(), ct);

    public async Task<bool> CodeExistsAsync(string projectCode, CancellationToken ct = default)
        => await _context.Projects.AnyAsync(
            p => p.ProjectCode.ToLower() == projectCode.ToLower(), ct);

    public async Task<IReadOnlyList<Project>> GetFilteredAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        int page, int limit, string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, accountCode, status, isActive, billable, pmEmpCode);
        query = ApplySort(query, sort);
        return await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(p => p.Account)
            .Include(p => p.ProjectManager)
            .Include(p => p.Allocations)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Project>> GetFilteredAllAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, accountCode, status, isActive, billable, pmEmpCode);
        query = ApplySort(query, sort);
        return await query
            .Include(p => p.Account)
            .Include(p => p.ProjectManager)
            .Include(p => p.Allocations)
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

    private static readonly Dictionary<string, Expression<Func<Project, object>>> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["projectName"] = p => p.ProjectName,
        ["projectCode"] = p => p.ProjectCode,
        ["startDate"] = p => p.StartDate,
        ["endDate"] = p => p.EndDate!,
        ["status"] = p => p.Status,
        ["billable"] = p => p.Billable,
        ["isActive"] = p => p.IsActive,
        ["createdAt"] = p => p.CreatedAt,
        ["updatedAt"] = p => p.UpdatedAt,
        ["accountCode"] = p => p.Account.AccountCode,
        ["accountName"] = p => p.Account.AccountName,
        ["projectManagerName"] = p => p.ProjectManager!.FirstName,
        ["projectManagerEmpCode"] = p => p.ProjectManager!.EmpCode,
    };

    private static IQueryable<Project> ApplySort(IQueryable<Project> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderBy(p => p.ProjectName);

        var isDescending = sort.StartsWith('-');
        var field = isDescending ? sort[1..] : sort;

        if (SortFields.TryGetValue(field, out var keySelector))
            return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

        throw new InvalidSortException(field, SortFields.Keys);
    }
}
