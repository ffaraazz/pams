using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly PamsDbContext _context;

    public AccountRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Account?> GetByCodeAsync(string accountCode, CancellationToken ct = default)
        => await _context.Accounts
            .Include(a => a.Projects)
            .FirstOrDefaultAsync(a => a.AccountCode.ToLower() == accountCode.ToLower(), ct);

    public async Task<bool> CodeExistsAsync(string accountCode, CancellationToken ct = default)
        => await _context.Accounts.AnyAsync(
            a => a.AccountCode.ToLower() == accountCode.ToLower(), ct);

    public async Task<IReadOnlyList<Account>> GetFilteredAsync(
        string? search, bool? isActive, AccountType? accountType,
        int page, int limit, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, isActive, accountType);
        return await query
            .OrderBy(a => a.AccountName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(a => a.Projects)
            .ToListAsync(ct);
    }

    public async Task<int> GetFilteredCountAsync(
        string? search, bool? isActive, AccountType? accountType,
        CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, isActive, accountType);
        return await query.CountAsync(ct);
    }

    public async Task<int> CountActiveProjectsAsync(Guid accountId, CancellationToken ct = default)
        => await _context.Projects.CountAsync(
            p => p.AccountId == accountId && p.IsActive, ct);

    public async Task AddAsync(Account account, CancellationToken ct = default)
        => await _context.Accounts.AddAsync(account, ct);

    public void Update(Account account)
        => _context.Accounts.Update(account);

    private IQueryable<Account> BuildFilteredQuery(
        string? search, bool? isActive, AccountType? accountType)
    {
        var query = _context.Accounts.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        if (accountType.HasValue)
            query = query.Where(a => a.AccountType == accountType.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(a =>
                a.AccountCode.ToLower().Contains(lowerSearch) ||
                a.AccountName.ToLower().Contains(lowerSearch));
        }

        return query;
    }
}
