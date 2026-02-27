using MediatR;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Accounts;

public sealed class UpdateAccountCommandHandler
    : IRequestHandler<UpdateAccountCommand, AccountDetailResponse>
{
    private readonly IAccountRepository _accountRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateAccountCommandHandler(
        IAccountRepository accountRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _accountRepo = accountRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<AccountDetailResponse> Handle(
        UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepo.GetByCodeAsync(request.AccountCode, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountCode);

        // Check deactivation constraint
        if (request.IsActive == false && account.IsActive)
        {
            var activeProjects = await _accountRepo.CountActiveProjectsAsync(
                account.Id, cancellationToken);
            if (activeProjects > 0)
                throw new DomainException(
                    $"Account has {activeProjects} active project(s). Deactivate or complete them before deactivating the account.",
                    "ERR_ACCOUNT_HAS_ACTIVE_PROJECTS");
        }

        account.AccountName = request.AccountName;
        account.AccountType = request.AccountType;
        if (request.IsActive.HasValue)
            account.IsActive = request.IsActive.Value;
        account.UpdatedAt = DateTime.UtcNow;

        _accountRepo.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("account.updated", new
        {
            account.Id,
            account.AccountCode
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
