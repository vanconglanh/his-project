using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("diab_his_bil_vouchers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(255);
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasColumnType("DECIMAL(5,2)");
        builder.Property(x => x.AppliesTo).HasColumnName("applies_to").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to");
        builder.Property(x => x.MaxUses).HasColumnName("max_uses");
        builder.Property(x => x.UsedCount).HasColumnName("used_count");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(x => x.DeletedBy); // bang khong co cot deleted_by
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

public class VoucherUsageConfiguration : IEntityTypeConfiguration<VoucherUsage>
{
    public void Configure(EntityTypeBuilder<VoucherUsage> builder)
    {
        builder.ToTable("diab_his_bil_voucher_usages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.VoucherId).HasColumnName("voucher_id").HasMaxLength(36);
        builder.Property(x => x.BillingId).HasColumnName("billing_id").HasMaxLength(36);
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.UsedAt).HasColumnName("used_at");
        builder.Property(x => x.UsedBy).HasColumnName("used_by").HasMaxLength(36);
        builder.HasIndex(x => new { x.TenantId, x.VoucherId });
        builder.HasIndex(x => new { x.TenantId, x.BillingId });
    }
}
