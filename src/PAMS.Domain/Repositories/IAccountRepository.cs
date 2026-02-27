using PAMS.Domain.Entities;
using PAMS.Domain.Enums;

namespace PAMS.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByCodeAsync(string accountCode, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string accountCode, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetFilteredAsync(
        string? search, bool? isActive, AccountType? accountType,
        int page, int limit, CancellationToken ct = default);
    Task<int> GetFilteredCountAsync(
        string? search, bool? isActive, AccountType? accountType,
        CancellationToken ct = default);
    Task<int> CountActiveProjectsAsync(Guid accountId, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    void Update(Account account);
}
