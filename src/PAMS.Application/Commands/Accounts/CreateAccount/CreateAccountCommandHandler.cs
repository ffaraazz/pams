using MediatR;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Accounts;

public sealed class CreateAccountCommandHandler
    : IRequestHandler<CreateAccountCommand, AccountDetailResponse>
{
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<AccountDetailResponse> Handle(
        CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (await _accountRepo.CodeExistsAsync(request.AccountCode, cancellationToken))
            throw new ConflictException(
                $"Account code '{request.AccountCode}' is already in use.",
                "ERR_ACCOUNT_CODE_EXISTS");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = request.AccountCode,
            AccountName = request.AccountName,
            AccountType = request.AccountType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _accountRepo.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("account.created", new
        {
            account.Id,
            account.AccountCode,
            account.AccountType
        }, cancellationToken);

        return new AccountDetailResponse
        {
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            IsActive = account.IsActive,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
