using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAMS.Application.Commands.Accounts;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.DTOs.Common;
using PAMS.Application.Interfaces;
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
    private readonly IExportService _exportService;

    public AccountsController(IMediator mediator, IAccountRepository accountRepo, IExportService exportService)
    {
        _mediator = mediator;
        _accountRepo = accountRepo;
        _exportService = exportService;
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
    /// <param name="sort">Sort field. Prefix with - for descending (e.g. -accountName).</param>
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
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(page, 1);

        var accounts = await _accountRepo.GetFilteredAsync(search, isActive, accountType, page, limit, sort, ct);
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
    /// Export accounts as PDF or Excel.
    /// </summary>
    /// <remarks>
    /// Returns all matching accounts (no pagination) as a downloadable file.
    /// Accepts the same filter and sort parameters as the list endpoint.
    /// An optional JSON body may contain a column name map where keys are field names
    /// and values are display labels. Only mapped columns appear in the export.
    /// If no body is sent, all default columns are included.
    /// </remarks>
    /// <param name="search">Search by account code or name (partial match, case-insensitive).</param>
    /// <param name="isActive">Filter by active status. Omit to return all.</param>
    /// <param name="accountType">Filter by account type (Client, Internal, Bench).</param>
    /// <param name="sort">Sort field. Prefix with - for descending (e.g. -accountName).</param>
    /// <param name="ext">Export format: pdf or xls.</param>
    /// <param name="columns">Optional column name map. Keys = field names, values = display labels.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File download (PDF or Excel).</returns>
    /// <response code="200">File download.</response>
    /// <response code="400">Invalid export format or invalid sort field.</response>
    /// <response code="401">Missing or invalid authentication token.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPost("export")]
    [Authorize(Policy = "CanAllocate")]
    [Produces("application/pdf", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] AccountType? accountType,
        [FromQuery] string? sort,
        [FromQuery] string ext,
        [FromBody] Dictionary<string, string>? columns = null,
        CancellationToken ct = default)
    {
        var format = ext?.ToLowerInvariant();
        if (format != "pdf" && format != "xls")
            return Problem(
                detail: "ext must be 'pdf' or 'xls'.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://pams.internal/errors/ERR_VALIDATION");

        var accounts = await _accountRepo.GetFilteredAllAsync(search, isActive, accountType, sort, ct);

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

        var result = await _exportService.GenerateAccountsAsync(
            data, format == "xls" ? "xlsx" : format, columns, ct);

        return File(result.FileBytes, result.ContentType, result.FileName);
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
