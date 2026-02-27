using MediatR;
using PAMS.Application.DTOs.SystemConfig;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Exceptions;
using PAMS.Domain.Repositories;

namespace PAMS.Application.Commands.SystemConfig;

public sealed class UpdateSystemConfigCommandHandler
    : IRequestHandler<UpdateSystemConfigCommand, SystemConfigResponse>
{
    private readonly ISystemConfigRepository _configRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLog;

    public UpdateSystemConfigCommandHandler(
        ISystemConfigRepository configRepo,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLog)
    {
        _configRepo = configRepo;
        _unitOfWork = unitOfWork;
        _auditLog = auditLog;
    }

    public async Task<SystemConfigResponse> Handle(
        UpdateSystemConfigCommand request, CancellationToken cancellationToken)
    {
        // Validate minAllocationPct is a multiple of allocationIncrement
        if (request.MinAllocationPct % request.AllocationIncrement != 0)
            throw new DomainException(
                $"Minimum allocation percentage ({request.MinAllocationPct}) must be a multiple of the increment ({request.AllocationIncrement}).",
                "ERR_CONFIG_INVALID");

        var config = await _configRepo.GetAsync(cancellationToken);

        config.MinAllocationPercentage = request.MinAllocationPct;
        config.AllocationIncrement = request.AllocationIncrement;
        config.UpdatedAt = DateTime.UtcNow;

        _configRepo.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLog.LogAsync("systemconfig.updated", new
        {
            config.MinAllocationPercentage,
            config.AllocationIncrement
        }, cancellationToken);

        return new SystemConfigResponse
        {
            MinAllocationPct = config.MinAllocationPercentage,
            AllocationIncrement = config.AllocationIncrement,
            UpdatedAt = config.UpdatedAt
        };
    }
}
