using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");

        builder.HasKey(sc => sc.Id);
        builder.Property(sc => sc.Id)
            .HasColumnName("config_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(sc => sc.MinAllocationPercentage)
            .HasColumnName("min_allocation_pct")
            .HasDefaultValue(25)
            .IsRequired();

        builder.Property(sc => sc.AllocationIncrement)
            .HasColumnName("allocation_increment")
            .HasDefaultValue(5)
            .IsRequired();

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");
    }
}
