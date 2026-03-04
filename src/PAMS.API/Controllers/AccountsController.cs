using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Accounts;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.DTOs.Common;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;

namespace PAMS.API.Controllers;

/// <summary>
/// Account management endpoints. Accounts group projects under a client or internal organization.
/// </summary>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
[Produces("application/json")]
[Tags("Accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _accountRepo;

    public AccountsController(IMediator mediator, IAccountRepository accountRepo)
    {
        _mediator = mediator;
        _accountRepo = accountRepo;
    }

    /// <summary>
    /// List accounts with optional search and filters.
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of accounts. Supports partial-match search on account code or name,
    /// and filtering by active status and account type. HR and PM roles only.
    /// </remarks>
    /// <param name="search">Search by account code or name (partial match, case-insensitive).</param>
    /// <param name="isActive">Filter by active status. Omit to return all.</param>
    /// <param name="accountType">Filter by account type (Client, Internal, Bench).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="limit">Items per page (default: 10, max: 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of account summaries.</returns>
    [HttpGet]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(PagedResponse<AccountSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AccountSummaryResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] AccountType? accountType,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var accounts = await _accountRepo.GetFilteredAsync(search, isActive, accountType, page, limit, ct);
        var totalRecords = await _accountRepo.GetFilteredCountAsync(search, isActive, accountType, ct);

        var data = accounts.Select(a => new AccountSummaryResponse
        {
            AccountId = a.Id,
            AccountCode = a.AccountCode,
            AccountName = a.AccountName,
            AccountType = a.AccountType,
            IsActive = a.IsActive,
            TotalActiveProjects = a.Projects.Count(p => p.IsActive),
            TotalInactiveProjects = a.Projects.Count(p => !p.IsActive)
        }).ToList();

        return Ok(new PagedResponse<AccountSummaryResponse>
        {
            Data = data,
            Pagination = PaginationMeta.Create(page, limit, totalRecords)
        });
    }

    /// <summary>
    /// Get account by code.
    /// </summary>
    /// <remarks>
    /// Returns detailed account information including active/inactive project counts.
    /// </remarks>
    /// <param name="accountCode">Unique account business code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Account detail.</returns>
    [HttpGet("{accountCode}")]
    [Authorize(Policy = "CanAllocate")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDetailResponse>> GetByCode(
        string accountCode, CancellationToken ct)
    {
        var account = await _accountRepo.GetByCodeAsync(accountCode, ct);
        if (account is null) return NotFound();

        return Ok(new AccountDetailResponse
        {
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            IsActive = account.IsActive,
            TotalActiveProjects = account.Projects.Count(p => p.IsActive),
            TotalInactiveProjects = account.Projects.Count(p => !p.IsActive),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        });
    }

    /// <summary>
    /// Create a new account (HR only).
    /// </summary>
    /// <remarks>
    /// Creates an account with the specified code, name, and type. Account code must be unique.
    /// </remarks>
    /// <param name="command">Account creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created account detail.</returns>
    [HttpPost]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountDetailResponse>> Create(
        [FromBody] CreateAccountCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByCode),
            new { accountCode = result.AccountCode }, result);
    }

    /// <summary>
    /// Update account (HR only). Account code is immutable.
    /// </summary>
    /// <remarks>
    /// Updates account name, type, or active status. Set isActive=false to deactivate.
    /// An account with active projects cannot be deactivated (returns 422).
    /// </remarks>
    /// <param name="accountCode">Account code to update.</param>
    /// <param name="request">Update request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated account detail.</returns>
    [HttpPut("{accountCode}")]
    [Authorize(Policy = "HROnly")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AccountDetailResponse>> Update(
        string accountCode,
        [FromBody] UpdateAccountRequest request,
        CancellationToken ct)
    {
        var command = new UpdateAccountCommand
        {
            AccountCode = accountCode,
            AccountName = request.AccountName,
            AccountType = request.AccountType,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

/// <summary>
/// Request body for updating an account (excludes immutable accountCode from path).
/// </summary>
public sealed record UpdateAccountRequest
{
    public required string AccountName { get; init; }
    public required AccountType AccountType { get; init; }
    /// <summary>Set to false to deactivate. Server returns 422 if account has active projects.</summary>
    public bool? IsActive { get; init; }
}
