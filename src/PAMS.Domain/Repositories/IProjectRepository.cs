using PAMS.Domain.Entities;
using PAMS.Domain.Enums;

namespace PAMS.Domain.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByCodeAsync(string projectCode, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string projectCode, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> GetFilteredAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        int page, int limit, CancellationToken ct = default);
    Task<int> GetFilteredCountAsync(
        string? search, string? accountCode, ProjectStatus? status,
        bool? isActive, bool? billable, string? pmEmpCode,
        CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    void Update(Project project);
}
