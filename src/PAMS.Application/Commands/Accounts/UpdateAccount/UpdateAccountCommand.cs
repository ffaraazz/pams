using MediatR;
using PAMS.Application.DTOs.Accounts;
using PAMS.Domain.Enums;

namespace PAMS.Application.Commands.Accounts;

/// <summary>
/// Command to update an account (FR-002). Account code is immutable.
/// </summary>
public sealed record UpdateAccountCommand : IRequest<AccountDetailResponse>
{
    public required string AccountCode { get; init; }
    public required string AccountName { get; init; }
    public required AccountType AccountType { get; init; }
    public bool? IsActive { get; init; }
}
