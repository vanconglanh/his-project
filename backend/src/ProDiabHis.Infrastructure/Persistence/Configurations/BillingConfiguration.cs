using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;
using BillingEntity = ProDiabHis.Domain.Entities.Billing;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class BillingServiceConfiguration : IEntityTypeConfiguration<BillingService>
{
    public void Configure(EntityTypeBuilder<BillingService> builder)
    {
        builder.ToTable("diab_his_bil_services");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Price).HasColumnName("price").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatRate).HasColumnName("vat_rate");
        builder.Property(x => x.BhytCode).HasColumnName("bhyt_code").HasMaxLength(50);
        builder.Property(x => x.BhytMaxAmount).HasColumnName("bhyt_max_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(x => x.DeletedBy);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.ToTable("diab_his_bil_service_packages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnType("DECIMAL(5,2)");
        builder.HasMany(x => x.Items).WithOne(x => x.Package).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class ServicePackageItemConfiguration : IEntityTypeConfiguration<ServicePackageItem>
{
    public void Configure(EntityTypeBuilder<ServicePackageItem> builder)
    {
        builder.ToTable("diab_his_bil_service_package_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.PackageId).HasMaxLength(36);
        builder.Property(x => x.ServiceId).HasMaxLength(36);
    }
}

public class BillingConfiguration : IEntityTypeConfiguration<BillingEntity>
{
    public void Configure(EntityTypeBuilder<BillingEntity> builder)
    {
        builder.ToTable("diab_his_bil_billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.PatientId).HasMaxLength(36);
        builder.Property(x => x.EncounterId).HasMaxLength(36);
        builder.Property(x => x.BillNo).HasMaxLength(30);
        builder.Property(x => x.Subtotal).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatTotal).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.BhytAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.PatientPayable).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Balance).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.RightRoute).HasMaxLength(20);
        builder.HasMany(x => x.Items).WithOne(x => x.Billing).HasForeignKey(x => x.BillingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BillingItemConfiguration : IEntityTypeConfiguration<BillingItem>
{
    public void Configure(EntityTypeBuilder<BillingItem> builder)
    {
        builder.ToTable("diab_his_bil_billing_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.BillingId).HasMaxLength(36);
        builder.Property(x => x.RefId).HasMaxLength(36);
        builder.Property(x => x.ItemType).HasMaxLength(20).HasColumnName("item_type");
        builder.Property(x => x.Quantity).HasColumnType("DECIMAL(10,3)");
        builder.Property(x => x.UnitPrice).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.DiscountPercent).HasColumnType("DECIMAL(5,2)");
        builder.Property(x => x.LineTotal).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.BhytAmount).HasColumnType("DECIMAL(15,2)");
        builder.Ignore(x => x.Billing);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("diab_his_bil_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.BillingId).HasMaxLength(36);
        builder.Property(x => x.CashierShiftId).HasMaxLength(36);
        builder.Property(x => x.PaidBy).HasMaxLength(36);
        builder.Property(x => x.CreatedBy).HasMaxLength(36);
        builder.Property(x => x.Amount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.RefundedAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Method).HasMaxLength(20);
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.Provider).HasMaxLength(50);
        builder.Property(x => x.ProviderTxnId).HasMaxLength(100);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.ProviderTxnId).HasMaxLength(100);
    }
}

public class QrCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.ToTable("diab_his_bil_qr_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.BillingId).HasMaxLength(36);
        builder.Property(x => x.Provider).HasMaxLength(20);
        builder.Property(x => x.QrUrl).HasMaxLength(500);
        builder.Property(x => x.TransactionRef).HasMaxLength(50);
        builder.Property(x => x.Amount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Status).HasMaxLength(20);
    }
}

public class EInvoiceConfiguration : IEntityTypeConfiguration<EInvoice>
{
    public void Configure(EntityTypeBuilder<EInvoice> builder)
    {
        builder.ToTable("diab_his_bil_einvoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36);
        builder.Property(x => x.BillingId).HasMaxLength(36);
        builder.Property(x => x.CreatedBy).HasMaxLength(36);
        builder.Property(x => x.Provider).HasMaxLength(10);
        builder.Property(x => x.InvoiceNo).HasMaxLength(50);
        builder.Property(x => x.InvoiceSeries).HasMaxLength(20);
        builder.Property(x => x.CqtCode).HasMaxLength(13);
        builder.Property(x => x.TotalAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatAmount).HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Status).HasMaxLength(20);
        builder.Property(x => x.PdfUrl).HasMaxLength(500);
        builder.Property(x => x.XmlUrl).HasMaxLength(500);
    }
}

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.ToTable("diab_his_bil_cashier_shifts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(36).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.CashierUserId).HasMaxLength(36).HasColumnName("cashier_user_id");
        builder.Property(x => x.ShiftDate).HasColumnType("DATE").HasColumnName("shift_date");
        builder.Property(x => x.ShiftStart).HasColumnName("shift_start");
        builder.Property(x => x.ShiftEnd).HasColumnName("shift_end");
        builder.Property(x => x.OpeningBalance).HasColumnType("DECIMAL(15,2)").HasColumnName("opening_balance");
        builder.Property(x => x.ClosingBalance).HasColumnType("DECIMAL(15,2)").HasColumnName("closing_balance");
        builder.Property(x => x.ExpectedCash).HasColumnType("DECIMAL(15,2)").HasColumnName("expected_cash");
        builder.Property(x => x.ActualCash).HasColumnType("DECIMAL(15,2)").HasColumnName("actual_cash");
        builder.Property(x => x.Difference).HasColumnType("DECIMAL(15,2)").HasColumnName("difference");
        builder.Property(x => x.TotalCash).HasColumnType("DECIMAL(15,2)").HasColumnName("total_cash");
        builder.Property(x => x.TotalCard).HasColumnType("DECIMAL(15,2)").HasColumnName("total_card");
        builder.Property(x => x.TotalTransfer).HasColumnType("DECIMAL(15,2)").HasColumnName("total_transfer");
        builder.Property(x => x.TotalQr).HasColumnType("DECIMAL(15,2)").HasColumnName("total_qr");
        builder.Property(x => x.TotalOther).HasColumnType("DECIMAL(15,2)").HasColumnName("total_other");
        builder.Property(x => x.TotalRefund).HasColumnType("DECIMAL(15,2)").HasColumnName("total_refund");
        builder.Property(x => x.TotalVoid).HasColumnType("DECIMAL(15,2)").HasColumnName("total_void");
        builder.Property(x => x.CountTransactions).HasColumnName("count_transactions");
        builder.Property(x => x.BreakdownJson).HasColumnType("JSON").HasColumnName("breakdown_json");
        builder.Property(x => x.Status).HasMaxLength(10).HasColumnName("status");
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.ClosedBy).HasMaxLength(36).HasColumnName("closed_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
