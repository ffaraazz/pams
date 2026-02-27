using FluentAssertions;
using NSubstitute;
using PAMS.Application.Commands.Skills;
using PAMS.Application.DTOs.Skills;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateSkillCommandHandler and UpdateSkillCommandHandler.
/// </summary>
public sealed class CreateSkillCommandHandlerTests
{
    private readonly ISkillRepository _skillRepo = Substitute.For<ISkillRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private CreateSkillCommandHandler CreateSut() => new(_skillRepo, _unitOfWork, _auditLog);

    // ─── Happy path ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "Skill | CreateHandle_ValidCommand_ShouldPersistAndReturn")]
    public async Task Handle_ValidCommand_ShouldPersistAndReturn()
    {
        // Arrange
        _skillRepo.NameExistsAsync("C#", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateSkillCommand { SkillName = "C#" };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SkillName.Should().Be("C#");
        result.IsActive.Should().BeTrue();
        result.SkillId.Should().NotBeEmpty();
        await _skillRepo.Received(1).AddAsync(Arg.Any<Skill>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Duplicate name → ConflictException ─────────────────────────────────

    [Fact(DisplayName = "Skill | CreateHandle_DuplicateName_ShouldThrowConflictException")]
    public async Task Handle_DuplicateName_ShouldThrowConflictException()
    {
        // Arrange
        _skillRepo.NameExistsAsync("C#", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateSkillCommand { SkillName = "C#" };
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        await _skillRepo.DidNotReceive().AddAsync(Arg.Any<Skill>(), Arg.Any<CancellationToken>());
    }

    // ─── Audit log ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Skill | CreateHandle_Success_ShouldWriteAuditLog")]
    public async Task Handle_Success_ShouldWriteAuditLog()
    {
        // Arrange
        _skillRepo.NameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateSkillCommand { SkillName = "SQL" };
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("skill.created")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Unit tests for UpdateSkillCommandHandler.
/// </summary>
public sealed class UpdateSkillCommandHandlerTests
{
    private readonly ISkillRepository _skillRepo = Substitute.For<ISkillRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditLogService _auditLog = Substitute.For<IAuditLogService>();

    private UpdateSkillCommandHandler CreateSut() => new(_skillRepo, _unitOfWork, _auditLog);

    // ─── Happy path — update name ───────────────────────────────────────────

    [Fact(DisplayName = "Skill | UpdateHandle_ValidName_ShouldUpdateAndReturn")]
    public async Task Handle_ValidName_ShouldUpdateAndReturn()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, SkillName = "Old", IsActive = true };
        _skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);

        var command = new UpdateSkillCommand { SkillId = skillId, SkillName = "New" };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.SkillName.Should().Be("New");
        _skillRepo.Received(1).Update(skill);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Skill not found → NotFoundException ────────────────────────────────

    [Fact(DisplayName = "Skill | UpdateHandle_NotFound_ShouldThrowNotFoundException")]
    public async Task Handle_NotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _skillRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Skill?)null);

        var command = new UpdateSkillCommand { SkillId = Guid.NewGuid() };
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ─── Deactivate skill ───────────────────────────────────────────────────

    [Fact(DisplayName = "Skill | UpdateHandle_SetInactive_ShouldDeactivate")]
    public async Task Handle_SetInactive_ShouldDeactivate()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, SkillName = "C#", IsActive = true };
        _skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);

        var command = new UpdateSkillCommand { SkillId = skillId, IsActive = false };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
    }

    // ─── No changes provided — name stays same ──────────────────────────────

    [Fact(DisplayName = "Skill | UpdateHandle_NoNameProvided_ShouldKeepExisting")]
    public async Task Handle_NoNameProvided_ShouldKeepExisting()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, SkillName = "Original", IsActive = true };
        _skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);

        var command = new UpdateSkillCommand { SkillId = skillId }; // no SkillName
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.SkillName.Should().Be("Original");
    }

    // ─── Audit log ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Skill | UpdateHandle_Success_ShouldWriteAuditLog")]
    public async Task Handle_Success_ShouldWriteAuditLog()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var skill = new Skill { Id = skillId, SkillName = "C#", IsActive = true };
        _skillRepo.GetByIdAsync(skillId, Arg.Any<CancellationToken>()).Returns(skill);

        var command = new UpdateSkillCommand { SkillId = skillId, SkillName = "C# Latest" };
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("skill.updated")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
