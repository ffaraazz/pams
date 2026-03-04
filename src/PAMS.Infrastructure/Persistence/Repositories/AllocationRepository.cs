using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class AllocationRepository : IAllocationRepository
{
    private readonly PamsDbContext _context;

    public AllocationRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<Allocation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Allocations.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Allocation>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
        => await _context.Allocations
            .Include(a => a.Project)
                .ThenInclude(p => p.Account)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.FromDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Allocation>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _context.Allocations
            .Include(a => a.Employee)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.FromDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Allocation>> GetOverlappingAsync(
        Guid employeeId, DateOnly from, DateOnly? to, CancellationToken ct = default)
    {
        var query = _context.Allocations
            .Where(a => a.EmployeeId == employeeId);

        if (to.HasValue)
        {
            query = query.Where(a =>
                a.FromDate <= to.Value &&
                (a.ToDate == null || a.ToDate >= from));
        }
        else
        {
            // Open-ended new allocation — overlaps with anything that hasn't ended before from
            query = query.Where(a =>
                a.ToDate == null || a.ToDate >= from);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<int> GetOverlappingTotalPercentageAsync(
        Guid employeeId, DateOnly from, DateOnly? to,
        Guid? excludeAllocationId = null, CancellationToken ct = default)
    {
        var query = _context.Allocations
            .Where(a => a.EmployeeId == employeeId);

        if (to.HasValue)
        {
            query = query.Where(a =>
                a.FromDate <= to.Value &&
                (a.ToDate == null || a.ToDate >= from));
        }
        else
        {
            query = query.Where(a =>
                a.ToDate == null || a.ToDate >= from);
        }

        if (excludeAllocationId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAllocationId.Value);
        }

        return await query.SumAsync(a => a.Percentage, ct);
    }

    public async Task AddAsync(Allocation allocation, CancellationToken ct = default)
        => await _context.Allocations.AddAsync(allocation, ct);

    public void Update(Allocation allocation)
        => _context.Allocations.Update(allocation);

    public void SoftDelete(Allocation allocation)
    {
        allocation.DeletedAt = DateTime.UtcNow;
        _context.Allocations.Update(allocation);
    }

    public async Task<IReadOnlyList<Allocation>> GetFilteredAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        int page, int limit, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(empCode, projectCode, projectManagerEmpCode, status, billable);
        return await query
            .OrderByDescending(a => a.FromDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(a => a.Employee)
            .Include(a => a.Project)
                .ThenInclude(p => p.Account)
            .ToListAsync(ct);
    }

    public async Task<int> GetFilteredCountAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(empCode, projectCode, projectManagerEmpCode, status, billable);
        return await query.CountAsync(ct);
    }

    private IQueryable<Allocation> BuildFilteredQuery(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable)
    {
        var query = _context.Allocations
            .Where(a => a.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(empCode))
            query = query.Where(a => a.Employee.EmpCode.ToLower() == empCode.ToLower());

        if (!string.IsNullOrWhiteSpace(projectCode))
            query = query.Where(a => a.Project.ProjectCode.ToLower() == projectCode.ToLower());

        if (!string.IsNullOrWhiteSpace(projectManagerEmpCode))
            query = query.Where(a => a.Project.ProjectManager != null &&
                a.Project.ProjectManager.EmpCode.ToLower() == projectManagerEmpCode.ToLower());

        if (billable.HasValue)
            query = query.Where(a => a.Billable == billable.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = status.ToLower() switch
            {
                "active" => query.Where(a => a.FromDate <= today && (a.ToDate == null || a.ToDate >= today)),
                "ended" => query.Where(a => a.ToDate != null && a.ToDate < today),
                "upcoming" => query.Where(a => a.FromDate > today),
                _ => query
            };
        }

        return query;
    }
}
