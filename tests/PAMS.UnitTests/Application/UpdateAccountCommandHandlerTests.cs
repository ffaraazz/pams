using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Accounts;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for UpdateAccountCommandHandler (FR-002).
/// Handler orchestrates: find by code → deactivation constraint → update → persist → audit.
/// </summary>
public sealed class UpdateAccountCommandHandlerTests
{
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateAccountCommandHandler CreateSut() => new(_accountRepo, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Account CreateAccount(string code = "ACC001", bool isActive = true)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = code,
            AccountName = "Old Name",
            AccountType = AccountType.Client,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
    }

    // ─── FR-002 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-002 | Handle_ValidCommand_ShouldUpdateAndReturnAccountDetail")]
    public async Task Handle_ValidCommand_ShouldUpdateAndReturnAccountDetail()
    {
        // Arrange
        var existing = CreateAccount(TestData.AccountCode);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "New Name",
            AccountType = AccountType.Internal
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountName.Should().Be("New Name");
        result.AccountType.Should().Be(AccountType.Internal);
        result.IsActive.Should().BeTrue();
        _accountRepo.Received(1).Update(Arg.Any<Account>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-002 | Not found → NotFoundException ─────────────────────────────

    [Fact(DisplayName = "FR-002 | Handle_AccountNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_AccountNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "X",
            AccountType = AccountType.Client
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-002 | Deactivation with active projects → DomainException ───────

    [Fact(DisplayName = "FR-002 | Handle_DeactivateWithActiveProjects_ShouldThrowDomainException")]
    public async Task Handle_DeactivateWithActiveProjects_ShouldThrowDomainException()
    {
        // Arrange
        var existing = CreateAccount(TestData.AccountCode, isActive: true);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);
        _accountRepo.CountActiveProjectsAsync(existing.Id, Arg.Any<CancellationToken>())
            .Returns(3);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Updated",
            AccountType = AccountType.Client,
            IsActive = false
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*active project*");
    }

    // ─── FR-002 | Deactivation with no active projects → allowed ────────────

    [Fact(DisplayName = "FR-002 | Handle_DeactivateWithNoActiveProjects_ShouldSucceed")]
    public async Task Handle_DeactivateWithNoActiveProjects_ShouldSucceed()
    {
        // Arrange
        var existing = CreateAccount(TestData.AccountCode, isActive: true);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);
        _accountRepo.CountActiveProjectsAsync(existing.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Closed Account",
            AccountType = AccountType.Client,
            IsActive = false
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
    }

    // ─── FR-002 | Already inactive → no deactivation check needed ───────────

    [Fact(DisplayName = "FR-002 | Handle_AlreadyInactive_ShouldNotCheckProjects")]
    public async Task Handle_AlreadyInactive_ShouldNotCheckProjects()
    {
        // Arrange — account is already inactive, IsActive=false in command
        var existing = CreateAccount(TestData.AccountCode, isActive: false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Still Inactive",
            AccountType = AccountType.Internal,
            IsActive = false
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — CountActiveProjectsAsync should NOT be called for re-deactivation
        await _accountRepo.DidNotReceive().CountActiveProjectsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ─── FR-002 | No IsActive supplied → remains unchanged ──────────────────

    [Fact(DisplayName = "FR-002 | Handle_NoIsActive_ShouldNotChangeActiveFlag")]
    public async Task Handle_NoIsActive_ShouldNotChangeActiveFlag()
    {
        // Arrange
        var existing = CreateAccount(TestData.AccountCode, isActive: true);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Updated Name",
            AccountType = AccountType.Client
            // IsActive not set (null)
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeTrue();
    }

    // ─── FR-002 | Audit log written on success ──────────────────────────────

    [Fact(DisplayName = "FR-002 | Handle_SuccessfulUpdate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulUpdate_ShouldWriteAuditLog()
    {
        // Arrange
        var existing = CreateAccount(TestData.AccountCode);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new UpdateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Updated",
            AccountType = AccountType.Client
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("account.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
