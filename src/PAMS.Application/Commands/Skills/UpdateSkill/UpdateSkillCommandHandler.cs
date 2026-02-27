using MediatR;
using PAMS.Application.DTOs.Skills;
using PAMS.Application.Exceptions;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.Skills;

public sealed class UpdateSkillCommandHandler : IRequestHandler<UpdateSkillCommand, SkillResponse>
{
    private readonly ISkillRepository _skillRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateSkillCommandHandler(
        ISkillRepository skillRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _skillRepo = skillRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<SkillResponse> Handle(UpdateSkillCommand request, CancellationToken cancellationToken)
    {
        var skill = await _skillRepo.GetByIdAsync(request.SkillId, cancellationToken)
            ?? throw new NotFoundException("Skill", request.SkillId);

        if (!string.IsNullOrWhiteSpace(request.SkillName))
            skill.SkillName = request.SkillName;

        if (request.IsActive.HasValue)
            skill.IsActive = request.IsActive.Value;

        _skillRepo.Update(skill);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("skill.updated", new { skill.Id, skill.SkillName }, cancellationToken);

        return new SkillResponse
        {
            SkillId = skill.Id,
            SkillName = skill.SkillName,
            IsActive = skill.IsActive
        };
    }
}
