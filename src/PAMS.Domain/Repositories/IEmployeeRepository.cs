using PAMS.Domain.Entities;

namespace PAMS.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByEmpCodeAsync(string empCode, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmpCodeAsync(string empCode, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> GetFilteredAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        int page, int limit, CancellationToken ct = default);
    Task<int> GetFilteredCountAsync(
        string? search, Guid? skillId, bool benchOnly, string? role,
        bool? isActive, DateOnly? windowFrom, DateOnly? windowTo,
        CancellationToken ct = default);
    Task<bool> WouldCreateCircularReportingAsync(Guid employeeId, Guid reportsToId, CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    void Update(Employee employee);
}
