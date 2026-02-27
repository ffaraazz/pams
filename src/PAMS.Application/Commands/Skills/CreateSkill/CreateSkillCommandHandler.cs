using MediatR;
using PAMS.Application.DTOs.Skills;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Skills;

public sealed class CreateSkillCommandHandler : IRequestHandler<CreateSkillCommand, SkillResponse>
{
    private readonly ISkillRepository _skillRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public CreateSkillCommandHandler(
        ISkillRepository skillRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _skillRepo = skillRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<SkillResponse> Handle(CreateSkillCommand request, CancellationToken cancellationToken)
    {
        if (await _skillRepo.NameExistsAsync(request.SkillName, cancellationToken))
            throw new ConflictException(
                $"Skill name '{request.SkillName}' already exists.",
                "ERR_SKILL_NAME_EXISTS");

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            SkillName = request.SkillName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _skillRepo.AddAsync(skill, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("skill.created", new { skill.Id, skill.SkillName }, cancellationToken);

        return new SkillResponse
        {
            SkillId = skill.Id,
            SkillName = skill.SkillName,
            IsActive = skill.IsActive
        };
    }
}
