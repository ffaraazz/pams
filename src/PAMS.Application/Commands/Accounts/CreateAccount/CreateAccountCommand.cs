using MediatR;
using PAMS.Application.DTOs.Accounts;
using PAMS.Domain.Enums;

namespace PAMS.Application.Commands.Accounts;

/// <summary>
/// Command to create a new account (FR-001).
/// </summary>
public sealed record CreateAccountCommand : IRequest<AccountDetailResponse>
{
    public required string AccountCode { get; init; }
    public required string AccountName { get; init; }
    public required AccountType AccountType { get; init; }
}
