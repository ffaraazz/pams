using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PAMS.Domain.Entities;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Persistence.Seed;

public static class SystemConfigSeeder
{
    public static async Task SeedAsync(PamsDbContext context, ILogger logger)
    {
        if (await context.SystemConfigs.AnyAsync())
        {
            logger.LogDebug("SystemConfig already seeded, skipping");
            return;
        }

        var config = new SystemConfig
        {
            Id = Guid.NewGuid(),
            MinAllocationPercentage = 25,
            AllocationIncrement = 5,
            UpdatedAt = DateTime.UtcNow
        };

        context.SystemConfigs.Add(config);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded SystemConfig: MinAllocationPct={Min}, Increment={Inc}",
            config.MinAllocationPercentage, config.AllocationIncrement);
    }
}
