using PAMS.Domain.Entities;

namespace PAMS.Domain.Repositories;

public interface ISystemConfigRepository
{
    Task<SystemConfig> GetAsync(CancellationToken ct = default);
    Task<int> GetMinAllocationPercentageAsync(CancellationToken ct = default);
    Task<int> GetAllocationIncrementAsync(CancellationToken ct = default);
    void Update(SystemConfig config);
}
