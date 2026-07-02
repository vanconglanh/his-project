using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("diab_his_sys_tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(t => t.CompanyName).HasColumnName("company_name").HasMaxLength(255);
        builder.Property(t => t.CskcbCode).HasColumnName("cskcb_code").HasMaxLength(20);
        builder.Property(t => t.TaxCode).HasColumnName("tax_code").HasMaxLength(20);
        builder.Property(t => t.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(t => t.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(t => t.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(t => t.EmailSupport).HasColumnName("email_support").HasMaxLength(255);
        builder.Property(t => t.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(t => t.Subdomain).HasColumnName("subdomain").HasMaxLength(63).IsRequired();
        builder.Property(t => t.StorageQuotaGb).HasColumnName("storage_quota_gb").HasDefaultValue(20);
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ACTIVE");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.BhytTokenEncrypted).HasColumnName("bhyt_token_encrypted");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.Subdomain).IsUnique();

    }
}
