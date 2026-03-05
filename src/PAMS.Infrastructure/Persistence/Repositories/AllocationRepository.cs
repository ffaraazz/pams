using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PAMS.Application.Exceptions;
using PAMS.Application.Helpers;
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
        int page, int limit, string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(empCode, projectCode, projectManagerEmpCode, status, billable);
        query = ApplySort(query, sort);
        return await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(a => a.Employee)
            .Include(a => a.Project)
                .ThenInclude(p => p.Account)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Allocation>> GetFilteredAllAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(empCode, projectCode, projectManagerEmpCode, status, billable);
        query = ApplySort(query, sort);
        return await query
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

    private static readonly Dictionary<string, Expression<Func<Allocation, object>>> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fromDate"] = a => a.FromDate,
        ["toDate"] = a => a.ToDate!,
        ["percentage"] = a => a.Percentage,
        ["billable"] = a => a.Billable,
        ["projectRole"] = a => a.ProjectRole!,
        ["createdAt"] = a => a.CreatedAt,
        ["updatedAt"] = a => a.UpdatedAt,
        ["empCode"] = a => a.Employee.EmpCode,
        ["employeeName"] = a => a.Employee.FirstName,
        ["projectCode"] = a => a.Project.ProjectCode,
        ["projectName"] = a => a.Project.ProjectName,
        ["accountCode"] = a => a.Project.Account.AccountCode,
        ["accountName"] = a => a.Project.Account.AccountName,
        ["projectBillable"] = a => a.Project.Billable,
        ["status"] = a => a.FromDate,
    };

    private static IQueryable<Allocation> ApplySort(IQueryable<Allocation> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderByDescending(a => a.FromDate);

        var isDescending = sort.StartsWith('-');
        var field = isDescending ? sort[1..] : sort;

        if (SortFields.TryGetValue(field, out var keySelector))
            return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

        throw new InvalidSortException(field, SortFields.Keys);
    }
}
