using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", t =>
        {
            t.HasCheckConstraint("chk_project_dates", "end_date IS NULL OR end_date >= start_date");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("project_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.ProjectCode)
            .HasColumnName("project_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.ProjectCode).IsUnique();

        builder.Property(p => p.ProjectName)
            .HasColumnName("project_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(p => p.ProjectManagerId)
            .HasColumnName("project_manager_id");

        builder.Property(p => p.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(p => p.EndDate)
            .HasColumnName("end_date");

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(Domain.Enums.ProjectStatus.Upcoming);

        builder.Property(p => p.Billable)
            .HasColumnName("billable")
            .HasDefaultValue(true);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(p => p.Account)
            .WithMany(a => a.Projects)
            .HasForeignKey(p => p.AccountId);

        builder.HasOne(p => p.ProjectManager)
            .WithMany(e => e.ManagedProjects)
            .HasForeignKey(p => p.ProjectManagerId);

        builder.HasIndex(p => p.AccountId);
    }
}
