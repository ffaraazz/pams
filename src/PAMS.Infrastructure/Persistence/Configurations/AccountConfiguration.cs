using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PAMS.Domain.Entities;

namespace PAMS.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("account_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AccountCode)
            .HasColumnName("account_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(a => a.AccountCode).IsUnique();

        builder.Property(a => a.AccountName)
            .HasColumnName("account_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.AccountType)
            .HasColumnName("account_type")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");
    }
}
