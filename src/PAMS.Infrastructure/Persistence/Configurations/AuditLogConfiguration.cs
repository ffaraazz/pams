using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("audit_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .IsRequired();

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id");

        builder.Property(a => a.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("idx_auditlog_entity");

        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("idx_auditlog_actor");

        builder.HasIndex(a => a.CreatedAt)
            .IsDescending()
            .HasDatabaseName("idx_auditlog_created");
    }
}
