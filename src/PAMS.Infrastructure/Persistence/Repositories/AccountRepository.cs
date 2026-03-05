using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PAMS.Application.Exceptions;
using PAMS.Application.Helpers;
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
        int page, int limit, string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, isActive, accountType);
        query = ApplySort(query, sort);
        return await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(a => a.Projects)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Account>> GetFilteredAllAsync(
        string? search, bool? isActive, AccountType? accountType,
        string? sort, CancellationToken ct = default)
    {
        var query = BuildFilteredQuery(search, isActive, accountType);
        query = ApplySort(query, sort);
        return await query
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

    private static readonly Dictionary<string, Expression<Func<Account, object>>> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accountName"] = a => a.AccountName,
        ["accountCode"] = a => a.AccountCode,
        ["accountType"] = a => a.AccountType,
        ["isActive"] = a => a.IsActive,
        ["createdAt"] = a => a.CreatedAt,
        ["updatedAt"] = a => a.UpdatedAt,
        ["totalActiveProjects"] = a => a.Projects.Count(p => p.IsActive),
        ["totalInactiveProjects"] = a => a.Projects.Count(p => !p.IsActive),
    };

    private static IQueryable<Account> ApplySort(IQueryable<Account> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return query.OrderBy(a => a.AccountName);

        var isDescending = sort.StartsWith('-');
        var field = isDescending ? sort[1..] : sort;

        if (SortFields.TryGetValue(field, out var keySelector))
            return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

        throw new InvalidSortException(field, SortFields.Keys);
    }
}
