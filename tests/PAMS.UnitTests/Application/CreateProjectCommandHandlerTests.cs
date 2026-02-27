using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Projects;
using PAMS.Application.DTOs.Projects;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Enums;
using PAMS.Domain.Repositories;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateProjectCommandHandler (FR-003).
/// Handler orchestrates: code uniqueness → account lookup → PM lookup → billable default → persist → audit.
/// </summary>
public sealed class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IAccountRepository _accountRepo = Substitute.For<IAccountRepository>();
    private readonly IEmployeeRepository _employeeRepo = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private CreateProjectCommandHandler CreateSut()
        => new(_projectRepo, _accountRepo, _employeeRepo, _unitOfWork, _auditLog);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static Account CreateAccount(AccountType type = AccountType.Client)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            AccountCode = TestData.AccountCode,
            AccountName = "Acme Corp",
            AccountType = type,
            IsActive = true
        };
    }

    private static Employee CreatePm()
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            EmpCode = TestData.PmEmpCode,
            FirstName = "Alice",
            LastName = "Manager",
            Email = "alice@acme.com",
            Role = EmployeeRole.ProjectManager,
            IsActive = true
        };
    }

    // ─── FR-003 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-003 | Handle_ValidCommand_ShouldCreateProjectAndReturn")]
    public async Task Handle_ValidCommand_ShouldCreateProjectAndReturn()
    {
        // Arrange
        var account = CreateAccount();
        var pm = CreatePm();

        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);
        _employeeRepo.GetByEmpCodeAsync(TestData.PmEmpCode, Arg.Any<CancellationToken>()).Returns(pm);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "New Project",
            AccountCode = TestData.AccountCode,
            ProjectManagerEmpCode = TestData.PmEmpCode,
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ProjectCode.Should().Be(TestData.ProjectCode);
        result.ProjectName.Should().Be("New Project");
        result.Status.Should().Be(ProjectStatus.Upcoming);
        result.IsActive.Should().BeTrue();
        result.AccountCode.Should().Be(TestData.AccountCode);
        result.ProjectManagerEmpCode.Should().Be(TestData.PmEmpCode);
        await _projectRepo.Received(1).AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-003 | Duplicate code → ConflictException ────────────────────────

    [Fact(DisplayName = "FR-003 | Handle_DuplicateCode_ShouldThrowConflictException")]
    public async Task Handle_DuplicateCode_ShouldThrowConflictException()
    {
        // Arrange
        _projectRepo.CodeExistsAsync(TestData.ProjectCode, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    // ─── FR-003 | Account not found → NotFoundException ─────────────────────

    [Fact(DisplayName = "FR-003 | Handle_AccountNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_AccountNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>())
            .Returns((Account?)null);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-003 | PM not found → NotFoundException ──────────────────────────

    [Fact(DisplayName = "FR-003 | Handle_PmNotFound_ShouldThrowNotFoundException")]
    public async Task Handle_PmNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var account = CreateAccount();
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);
        _employeeRepo.GetByEmpCodeAsync("UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "X",
            AccountCode = TestData.AccountCode,
            ProjectManagerEmpCode = "UNKNOWN",
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── FR-004 | Billable defaults to true for Client accounts ─────────────

    [Fact(DisplayName = "FR-004 | Handle_ClientAccountNoBillable_ShouldDefaultToTrue")]
    public async Task Handle_ClientAccountNoBillable_ShouldDefaultToTrue()
    {
        // Arrange
        var account = CreateAccount(AccountType.Client);
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Client Project",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
            // Billable not set
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Billable.Should().BeTrue();
    }

    // ─── FR-004 | Billable defaults to false for Internal accounts ──────────

    [Fact(DisplayName = "FR-004 | Handle_InternalAccountNoBillable_ShouldDefaultToFalse")]
    public async Task Handle_InternalAccountNoBillable_ShouldDefaultToFalse()
    {
        // Arrange
        var account = CreateAccount(AccountType.Internal);
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Internal Project",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Billable.Should().BeFalse();
    }

    // ─── FR-004 | Explicit billable overrides default ───────────────────────

    [Fact(DisplayName = "FR-004 | Handle_ExplicitBillable_ShouldOverrideDefault")]
    public async Task Handle_ExplicitBillable_ShouldOverrideDefault()
    {
        // Arrange — Internal account but explicitly set as billable
        var account = CreateAccount(AccountType.Internal);
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Special Internal",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today,
            Billable = true
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Billable.Should().BeTrue();
    }

    // ─── FR-003 | No PM specified → project created without PM ──────────────

    [Fact(DisplayName = "FR-003 | Handle_NoPm_ShouldCreateProjectWithoutPm")]
    public async Task Handle_NoPm_ShouldCreateProjectWithoutPm()
    {
        // Arrange
        var account = CreateAccount();
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "No PM Project",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
            // No ProjectManagerEmpCode
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.ProjectManagerId.Should().BeNull();
        result.ProjectManagerEmpCode.Should().BeNull();
    }

    // ─── FR-003 | Audit log written on success ──────────────────────────────

    [Fact(DisplayName = "FR-003 | Handle_SuccessfulCreate_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulCreate_ShouldWriteAuditLog()
    {
        // Arrange
        var account = CreateAccount();
        _projectRepo.CodeExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _accountRepo.GetByCodeAsync(TestData.AccountCode, Arg.Any<CancellationToken>()).Returns(account);

        var command = new CreateProjectCommand
        {
            ProjectCode = TestData.ProjectCode,
            ProjectName = "Audit Project",
            AccountCode = TestData.AccountCode,
            StartDate = TestData.Today
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("project.created")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
