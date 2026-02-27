using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.SystemConfig;
using PAMS.Application.DTOs.SystemConfig;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for UpdateSystemConfigCommandHandler (FR-021).
/// Handler orchestrates: validate multiple → get config → update → persist → audit.
/// </summary>
public sealed class UpdateSystemConfigCommandHandlerTests
{
    private readonly ISystemConfigRepository _configRepo = Substitute.For<ISystemConfigRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateSystemConfigCommandHandler CreateSut()
        => new(_configRepo, _unitOfWork, _auditLog);

    private static SystemConfig CreateConfig(int minPct = 25, int increment = 5)
    {
        return new SystemConfig
        {
            Id = Guid.NewGuid(),
            MinAllocationPercentage = minPct,
            AllocationIncrement = increment,
            UpdatedAt = DateTime.UtcNow.AddDays(-7)
        };
    }

    // ─── FR-021 | Happy path ────────────────────────────────────────────────

    [Fact(DisplayName = "FR-021 | Handle_ValidConfig_ShouldUpdateAndReturn")]
    public async Task Handle_ValidConfig_ShouldUpdateAndReturn()
    {
        // Arrange
        var config = CreateConfig();
        _configRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(config);

        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = 10,
            AllocationIncrement = 5
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MinAllocationPct.Should().Be(10);
        result.AllocationIncrement.Should().Be(5);
        _configRepo.Received(1).Update(Arg.Any<SystemConfig>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-021 | MinPct not multiple of increment → DomainException ────────

    [Fact(DisplayName = "FR-021 | Handle_MinPctNotMultipleOfIncrement_ShouldThrowDomainException")]
    public async Task Handle_MinPctNotMultipleOfIncrement_ShouldThrowDomainException()
    {
        // Arrange — 10 % 3 = 1, not a valid multiple
        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = 10,
            AllocationIncrement = 3
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*multiple*");
    }

    // ─── FR-021 | Valid multiples pass validation ───────────────────────────

    [Theory(DisplayName = "FR-021 | Handle_ValidMultiples_ShouldSucceed")]
    [InlineData(25, 5)]
    [InlineData(10, 10)]
    [InlineData(50, 25)]
    [InlineData(20, 5)]
    public async Task Handle_ValidMultiples_ShouldSucceed(int minPct, int increment)
    {
        // Arrange
        var config = CreateConfig();
        _configRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(config);

        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = minPct,
            AllocationIncrement = increment
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── FR-021 | Invalid multiples fail validation ─────────────────────────

    [Theory(DisplayName = "FR-021 | Handle_InvalidMultiples_ShouldThrowDomainException")]
    [InlineData(7, 5)]
    [InlineData(13, 10)]
    [InlineData(11, 3)]
    public async Task Handle_InvalidMultiples_ShouldThrowDomainException(int minPct, int increment)
    {
        // Arrange
        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = minPct,
            AllocationIncrement = increment
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }

    // ─── FR-021 | Config repo not called when validation fails ──────────────

    [Fact(DisplayName = "FR-021 | Handle_ValidationFails_ShouldNotCallRepo")]
    public async Task Handle_ValidationFails_ShouldNotCallRepo()
    {
        // Arrange
        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = 7,
            AllocationIncrement = 3
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        await _configRepo.DidNotReceive().GetAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-021 | Audit log on success ──────────────────────────────────────

    [Fact(DisplayName = "FR-021 | Handle_Success_ShouldWriteAuditLog")]
    public async Task Handle_Success_ShouldWriteAuditLog()
    {
        // Arrange
        var config = CreateConfig();
        _configRepo.GetAsync(Arg.Any<CancellationToken>()).Returns(config);

        var command = new UpdateSystemConfigCommand
        {
            MinAllocationPct = 25,
            AllocationIncrement = 5
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("systemconfig.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
