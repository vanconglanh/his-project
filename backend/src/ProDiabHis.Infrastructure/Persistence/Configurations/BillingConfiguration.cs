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
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasColumnType("DECIMAL(5,2)");
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);
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
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.PackageId).HasColumnName("package_id").HasMaxLength(36);
        builder.Property(x => x.ServiceId).HasColumnName("service_id").HasMaxLength(36);
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Ignore(x => x.Service);
    }
}

public class BillingConfiguration : IEntityTypeConfiguration<BillingEntity>
{
    public void Configure(EntityTypeBuilder<BillingEntity> builder)
    {
        builder.ToTable("diab_his_bil_billing");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.PatientId).HasColumnName("patient_id").HasMaxLength(36);
        builder.Property(x => x.EncounterId).HasColumnName("encounter_id").HasMaxLength(36);
        builder.Property(x => x.BillNo).HasColumnName("bill_no").HasMaxLength(30);
        builder.Property(x => x.Payer).HasColumnName("payer").HasMaxLength(20);
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatTotal).HasColumnName("vat_total").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.BhytAmount).HasColumnName("bhyt_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.PatientPayable).HasColumnName("patient_payable").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Balance).HasColumnName("balance").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(x => x.RightRoute).HasColumnName("right_route").HasMaxLength(20);
        builder.Property(x => x.PaymentDueDate).HasColumnName("payment_due_date");
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.VoidReason).HasColumnName("void_reason");
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(x => x.DeletedBy); // bang khong co cot deleted_by
        builder.HasMany(x => x.Items).WithOne(x => x.Billing).HasForeignKey(x => x.BillingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BillingItemConfiguration : IEntityTypeConfiguration<BillingItem>
{
    public void Configure(EntityTypeBuilder<BillingItem> builder)
    {
        builder.ToTable("diab_his_bil_billing_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.BillingId).HasColumnName("billing_id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.RefId).HasColumnName("ref_id").HasMaxLength(36);
        builder.Property(x => x.ItemType).HasMaxLength(20).HasColumnName("item_type");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("DECIMAL(10,3)");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatRate).HasColumnName("vat_rate");
        builder.Property(x => x.DiscountPercent).HasColumnName("discount_percent").HasColumnType("DECIMAL(5,2)");
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.BhytApplicable).HasColumnName("bhyt_applicable");
        builder.Property(x => x.BhytAmount).HasColumnName("bhyt_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Ignore(x => x.Billing);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("diab_his_bil_payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.BillingId).HasColumnName("billing_id").HasMaxLength(36);
        builder.Property(x => x.CashierShiftId).HasColumnName("cashier_shift_id").HasMaxLength(36);
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Method).HasColumnName("method").HasMaxLength(20);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(100);
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(50);
        builder.Property(x => x.ProviderTxnId).HasColumnName("provider_txn_id").HasMaxLength(100);
        builder.Property(x => x.PaidAt).HasColumnName("paid_at");
        builder.Property(x => x.PaidBy).HasColumnName("paid_by").HasMaxLength(36);
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.RefundedAmount).HasColumnName("refunded_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}

public class QrCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.ToTable("diab_his_bil_qr_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.BillingId).HasColumnName("billing_id").HasMaxLength(36);
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(20);
        builder.Property(x => x.QrPayload).HasColumnName("qr_payload");
        builder.Property(x => x.QrUrl).HasColumnName("qr_image_path").HasMaxLength(500);
        builder.Property(x => x.Amount).HasColumnName("amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.PaidAt).HasColumnName("paid_at");
        builder.Property(x => x.TransactionRef).HasColumnName("transaction_ref").HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20); // cot bo sung boi mig 9061
    }
}

public class EInvoiceConfiguration : IEntityTypeConfiguration<EInvoice>
{
    public void Configure(EntityTypeBuilder<EInvoice> builder)
    {
        builder.ToTable("diab_his_bil_einvoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.BillingId).HasColumnName("billing_id").HasMaxLength(36);
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(10);
        builder.Property(x => x.InvoiceNo).HasColumnName("invoice_no").HasMaxLength(50);
        builder.Property(x => x.InvoiceSeries).HasColumnName("invoice_series").HasMaxLength(20);
        builder.Property(x => x.CqtCode).HasColumnName("cqt_code").HasMaxLength(13);
        builder.Property(x => x.IssueDate).HasColumnName("issue_date");
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.VatAmount).HasColumnName("vat_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(x => x.PdfUrl).HasColumnName("pdf_url").HasMaxLength(500);
        builder.Property(x => x.XmlUrl).HasColumnName("xml_url").HasMaxLength(500);
        builder.Property(x => x.SignedAt).HasColumnName("signed_at");
        builder.Property(x => x.CancelReason).HasColumnName("cancel_reason");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.LastError).HasColumnName("last_error");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
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
