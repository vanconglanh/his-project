using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class ApiPartnerConfiguration : IEntityTypeConfiguration<ApiPartner>
{
    public void Configure(EntityTypeBuilder<ApiPartner> builder)
    {
        builder.ToTable("diab_his_api_partners");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContactEmail).HasColumnName("contact_email").HasMaxLength(100);
        builder.Property(e => e.ApiKeyHash).HasColumnName("api_key_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ApiKeyPrefix).HasColumnName("api_key_prefix").HasMaxLength(20);
        builder.Property(e => e.ScopesJson).HasColumnName("scopes_json").HasColumnType("JSON");
        builder.Property(e => e.RateLimitPerMin).HasColumnName("rate_limit_per_min").HasDefaultValue(60);
        builder.Property(e => e.DailyQuota).HasColumnName("daily_quota").HasDefaultValue(10000);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ACTIVE");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.IpWhitelistJson).HasColumnName("ip_whitelist_json").HasColumnType("JSON");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => e.ApiKeyHash).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}
