using Microsoft.EntityFrameworkCore;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence;

public class PamsDbContext : DbContext
{
    public PamsDbContext(DbContextOptions<PamsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<EmployeeSkill> EmployeeSkills => Set<EmployeeSkill>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProjectTeamMember> ProjectTeamMembers => Set<ProjectTeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pams");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PamsDbContext).Assembly);

        // Global query filter for soft-deleted allocations
        modelBuilder.Entity<Allocation>().HasQueryFilter(a => a.DeletedAt == null);
    }
}
