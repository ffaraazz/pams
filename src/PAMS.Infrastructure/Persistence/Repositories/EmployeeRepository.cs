using Microsoft.EntityFrameworkCore;
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
        => await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Employee?> GetByEmpCodeAsync(string empCode, CancellationToken ct = default)
        => await _context.Employees.FirstOrDefaultAsync(
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
        int page, int limit, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, skillId, benchOnly, role, isActive, windowFrom, windowTo);
        return await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);
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
        var query = _context.Employees.AsQueryable();

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
}
