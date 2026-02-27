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
}
