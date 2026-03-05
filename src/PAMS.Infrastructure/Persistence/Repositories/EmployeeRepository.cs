using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PAMS.Application.Exceptions;
using PAMS.Application.Helpers;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly PamsDbContext _context;

    public EmployeeRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Employees
            .Include(e => e.EmployeeSkills).ThenInclude(es => es.Skill)
            .Include(e => e.Allocations).ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Employee?> GetByEmpCodeAsync(string empCode, CancellationToken ct = default)
        => await _context.Employees
            .Include(e => e.EmployeeSkills).ThenInclude(es => es.Skill)
            .Include(e => e.Allocations).ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(
            e => e.EmpCode.ToLower() == empCode.ToLower(), ct);

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Employees.FirstOrDefaultAsync(
            e => e.Email.ToLower() == email.ToLower(), ct);

    public async Task<bool> ExistsByEmpCodeAsync(string empCode, CancellationToken ct = default)
        => await _context.Employees.AnyAsync(
            e => e.EmpCode.ToLower() == empCode.ToLower(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Employees.AnyAsync(
            e => e.Email.ToLower() == email.ToLower(), ct);

    public async Task<IReadOnlyList<Employee>> GetFilteredAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        int page, int limit, string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, skillId, benchOnly, role, isActive, windowFrom, windowTo);
        query = ApplySort(query, sort);
        return await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetFilteredAllAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, skillId, benchOnly, role, isActive, windowFrom, windowTo);
        query = ApplySort(query, sort);
        return await query.ToListAsync(ct);
    }

    public async Task<int> GetFilteredCountAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, skillId, benchOnly, role, isActive, windowFrom, windowTo);
        return await query.CountAsync(ct);
    }

    public async Task<bool> WouldCreateCircularReportingAsync(Guid employeeId, Guid reportsToId, CancellationToken ct = default)
    {
        // Walk up the chain from reportsToId looking for employeeId
        var current = reportsToId;
        var visited = new HashSet<Guid> { employeeId };

        while (true)
        {
            if (current == employeeId)
                return true;

            if (!visited.Add(current))
                break;

            var next = await _context.Employees
                .Where(e => e.Id == current)
                .Select(e => e.ReportsToId)
                .FirstOrDefaultAsync(ct);

            if (next is null)
                break;

            current = next.Value;
        }

        return false;
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
        => await _context.Employees.AddAsync(employee, ct);

    public void Update(Employee employee)
        => _context.Employees.Update(employee);

    private IQueryable<Employee> BuildFilteredQuery(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo)
    {
        var query = _context.Employees
            .Include(e => e.EmployeeSkills).ThenInclude(es => es.Skill)
            .Include(e => e.Allocations)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(lowerSearch) ||
                e.LastName.ToLower().Contains(lowerSearch) ||
                e.EmpCode.ToLower().Contains(lowerSearch) ||
                e.Email.ToLower().Contains(lowerSearch));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(e => e.Role.ToString() == role);

        if (skillId.HasValue)
            query = query.Where(e => e.EmployeeSkills.Any(es => es.SkillId == skillId.Value));

        if (benchOnly)
        {
            // Bench employees = those with no active overlapping allocations today
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => !e.Allocations.Any(a =>
                a.DeletedAt == null &&
                a.FromDate <= today &&
                (a.ToDate == null || a.ToDate >= today)));
        }

        return query;
    }

    private static readonly Dictionary<string, Expression<Func<Employee, object>>> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["firstName"] = e => e.FirstName,
        ["lastName"] = e => e.LastName,
        ["fullName"] = e => e.FirstName,
        ["empCode"] = e => e.EmpCode,
        ["email"] = e => e.Email,
        ["designation"] = e => e.Designation,
        ["role"] = e => e.Role,
        ["isActive"] = e => e.IsActive,
        ["createdAt"] = e => e.CreatedAt,
        ["updatedAt"] = e => e.UpdatedAt,
    };

    private static IQueryable<Employee> ApplySort(IQueryable<Employee> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);

        var isDescending = sort.StartsWith('-');
        var field = isDescending ? sort[1..] : sort;

        if (SortFields.TryGetValue(field, out var keySelector))
            return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

        throw new InvalidSortException(field, SortFields.Keys);
    }
}
