using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("allocations", t =>
        {
            t.HasCheckConstraint("chk_allocation_dates", "to_date IS NULL OR to_date >= from_date");
            t.HasCheckConstraint("chk_allocation_percentage", "percentage BETWEEN 1 AND 100");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("allocation_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(a => a.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(a => a.FromDate)
            .HasColumnName("from_date")
            .IsRequired();

        builder.Property(a => a.ToDate)
            .HasColumnName("to_date");

        builder.Property(a => a.Percentage)
            .HasColumnName("percentage")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(a => a.ProjectRole)
            .HasColumnName("project_role")
            .HasMaxLength(150);

        builder.Property(a => a.AllocatedById)
            .HasColumnName("allocated_by_id")
            .IsRequired();

        builder.Property(a => a.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Allocations)
            .HasForeignKey(a => a.EmployeeId);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Allocations)
            .HasForeignKey(a => a.ProjectId);

        builder.HasOne(a => a.AllocatedBy)
            .WithMany()
            .HasForeignKey(a => a.AllocatedById);

        // Primary capacity query index (partial: only non-deleted)
        builder.HasIndex(a => new { a.EmployeeId, a.FromDate, a.ToDate })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_alloc_employee_dates");

        // Project view query index
        builder.HasIndex(a => a.ProjectId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_alloc_project");

        // Employee view index
        builder.HasIndex(a => a.EmployeeId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_alloc_employee_active");
    }
}
