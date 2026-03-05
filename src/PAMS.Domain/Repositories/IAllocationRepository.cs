using PAMS.Domain.Entities;

namespace PAMS.Domain.Repositories;

public interface IAllocationRepository
{
    Task<Allocation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetOverlappingAsync(
        Guid employeeId, DateOnly from, DateOnly? to, CancellationToken ct = default);
    Task<int> GetOverlappingTotalPercentageAsync(
        Guid employeeId, DateOnly from, DateOnly? to,
        Guid? excludeAllocationId = null, CancellationToken ct = default);
    Task AddAsync(Allocation allocation, CancellationToken ct = default);
    void Update(Allocation allocation);
    void SoftDelete(Allocation allocation);
    Task<IReadOnlyList<Allocation>> GetFilteredAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        int page, int limit, string? sort, CancellationToken ct = default);
    Task<IReadOnlyList<Allocation>> GetFilteredAllAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        string? sort, CancellationToken ct = default);
    Task<int> GetFilteredCountAsync(
        string? empCode, string? projectCode, string? projectManagerEmpCode,
        string? status, bool? billable,
        CancellationToken ct = default);
}
