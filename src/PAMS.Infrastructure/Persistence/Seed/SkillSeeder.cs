using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PAMS.Domain.Entities;
using PAMS.Infrastructure.Persistence;

namespace PAMS.Infrastructure.Persistence.Seed;

public static class SkillSeeder
{
    private static readonly string[] DefaultSkills =
    [
        "C#", ".NET", "JavaScript", "TypeScript", "React", "Angular",
        "Python", "Java", "SQL", "PostgreSQL", "Azure", "AWS",
        "Docker", "Kubernetes", "CI/CD", "Agile", "Scrum"
    ];

    public static async Task SeedAsync(PamsDbContext context, ILogger logger)
    {
        if (await context.Skills.AnyAsync())
        {
            logger.LogDebug("Skills already seeded, skipping");
            return;
        }

        foreach (var skillName in DefaultSkills)
        {
            context.Skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                SkillName = skillName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default skills", DefaultSkills.Length);
    }
}
