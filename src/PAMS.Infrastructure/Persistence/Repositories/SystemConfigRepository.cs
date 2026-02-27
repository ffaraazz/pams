using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;
using PAMS.Domain.Repositories;

namespace PAMS.Infrastructure.Persistence.Repositories;

public sealed class SystemConfigRepository : ISystemConfigRepository
{
    private readonly PamsDbContext _context;

    public SystemConfigRepository(PamsDbContext context)
    {
        _context = context;
    }

    public async Task<SystemConfig> GetAsync(CancellationToken ct = default)
    {
        var config = await _context.SystemConfigs.FirstOrDefaultAsync(ct);
        if (config is null)
        {
            // Return defaults if no config row exists
            config = new SystemConfig
            {
                Id = Guid.NewGuid(),
                MinAllocationPercentage = 25,
                AllocationIncrement = 5,
                UpdatedAt = DateTime.UtcNow
            };
        }
        return config;
    }

    public async Task<int> GetMinAllocationPercentageAsync(CancellationToken ct = default)
    {
        var config = await GetAsync(ct);
        return config.MinAllocationPercentage;
    }

    public async Task<int> GetAllocationIncrementAsync(CancellationToken ct = default)
    {
        var config = await GetAsync(ct);
        return config.AllocationIncrement;
    }

    public void Update(SystemConfig config)
        => _context.SystemConfigs.Update(config);
}
