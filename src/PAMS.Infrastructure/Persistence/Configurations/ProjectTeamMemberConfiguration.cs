using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class ProjectTeamMemberConfiguration : IEntityTypeConfiguration<ProjectTeamMember>
{
    public void Configure(EntityTypeBuilder<ProjectTeamMember> builder)
    {
        builder.ToTable("project_team_members", t =>
        {
            t.HasCheckConstraint("chk_ptm_lead_not_reportee", "team_lead_id != reportee_id");
        });

        builder.HasKey(ptm => ptm.Id);
        builder.Property(ptm => ptm.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(ptm => ptm.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(ptm => ptm.TeamLeadId)
            .HasColumnName("team_lead_id")
            .IsRequired();

        builder.Property(ptm => ptm.ReporteeId)
            .HasColumnName("reportee_id")
            .IsRequired();

        builder.Property(ptm => ptm.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        // Unique composite constraint
        builder.HasIndex(ptm => new { ptm.ProjectId, ptm.TeamLeadId, ptm.ReporteeId })
            .IsUnique()
            .HasDatabaseName("uq_ptm_project_lead_reportee");

        builder.HasIndex(ptm => ptm.ProjectId)
            .HasDatabaseName("idx_ptm_project");

        builder.HasIndex(ptm => ptm.TeamLeadId)
            .HasDatabaseName("idx_ptm_team_lead");

        builder.HasIndex(ptm => ptm.ReporteeId)
            .HasDatabaseName("idx_ptm_reportee");

        builder.HasOne(ptm => ptm.Project)
            .WithMany(p => p.TeamMembers)
            .HasForeignKey(ptm => ptm.ProjectId);

        builder.HasOne(ptm => ptm.TeamLead)
            .WithMany()
            .HasForeignKey(ptm => ptm.TeamLeadId);

        builder.HasOne(ptm => ptm.Reportee)
            .WithMany()
            .HasForeignKey(ptm => ptm.ReporteeId);
    }
}
