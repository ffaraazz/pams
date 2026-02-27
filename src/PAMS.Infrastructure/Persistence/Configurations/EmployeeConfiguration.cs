using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("employee_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.EmpCode)
            .HasColumnName("emp_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => e.EmpCode).IsUnique();

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.Designation)
            .HasColumnName("designation")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.ReportsToId)
            .HasColumnName("reports_to");

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.ReportsTo)
            .WithMany(e => e.DirectReports)
            .HasForeignKey(e => e.ReportsToId);

        builder.HasIndex(e => e.ReportsToId)
            .HasFilter("reports_to IS NOT NULL");
    }
}
