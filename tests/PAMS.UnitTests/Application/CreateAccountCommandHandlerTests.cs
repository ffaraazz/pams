using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Accounts;
using PAMS.Application.DTOs.Accounts;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateAccountCommandHandler (FR-001).
/// Handler orchestrates: code uniqueness check → create account → persist → audit.
/// </summary>
public sealed class CreateAccountCommandHandlerTests
{
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private CreateAccountCommandHandler CreateSut() => new(_accountRepo, _unitOfWork, _auditLog);

    // ─── FR-001 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-001 | Handle_ValidCommand_ShouldPersistAndReturnAccountDetail")]
    public async Task Handle_ValidCommand_ShouldPersistAndReturnAccountDetail()
    {
        // Arrange
        _accountRepo.CodeExistsAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = AccountType.Client
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccountCode.Should().Be(TestData.AccountCode);
        result.AccountName.Should().Be("Acme Corp");
        result.AccountType.Should().Be(AccountType.Client);
        result.IsActive.Should().BeTrue();
        result.AccountId.Should().NotBeEmpty();
        await _accountRepo.Received(1).AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-001 | Duplicate code → ConflictException ────────────────────────

    [Fact(DisplayName = "FR-001 | Handle_DuplicateCode_ShouldThrowConflictException")]
    public async Task Handle_DuplicateCode_ShouldThrowConflictException()
    {
        // Arrange
        _accountRepo.CodeExistsAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = AccountType.Client
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─── FR-001 | Audit log written on success ──────────────────────────────

    [Fact(DisplayName = "FR-001 | Handle_SuccessfulCreate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulCreate_ShouldWriteAuditLog()
    {
        // Arrange
        _accountRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = AccountType.Internal
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("account.created")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    // ─── FR-001 | Each account type is accepted ─────────────────────────────

    [Theory(DisplayName = "FR-001 | Handle_ValidAccountTypes_ShouldSetCorrectType")]
    [InlineData(AccountType.Client)]
    [InlineData(AccountType.Internal)]
    [InlineData(AccountType.Bench)]
    public async Task Handle_ValidAccountTypes_ShouldSetCorrectType(AccountType accountType)
    {
        // Arrange
        _accountRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateAccountCommand
        {
            AccountCode = "ACC-NEW",
            AccountName = "Test Account",
            AccountType = accountType
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.AccountType.Should().Be(accountType);
    }

    // ─── FR-001 | No persist when conflict ──────────────────────────────────

    [Fact(DisplayName = "FR-001 | Handle_DuplicateCode_ShouldNotPersist")]
    public async Task Handle_DuplicateCode_ShouldNotPersist()
    {
        // Arrange
        _accountRepo.CodeExistsAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateAccountCommand
        {
            AccountCode = TestData.AccountCode,
            AccountName = "X",
            AccountType = AccountType.Client
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        await _accountRepo.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
