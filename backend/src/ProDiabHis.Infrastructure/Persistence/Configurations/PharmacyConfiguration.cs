using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities.Pharmacy;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.ToTable("diab_his_pha_drugs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.GenericName).HasColumnName("generic_name").HasMaxLength(255);
        builder.Property(e => e.BrandName).HasColumnName("brand_name").HasMaxLength(255);
        builder.Property(e => e.DrugForm).HasColumnName("drug_form").HasMaxLength(50);
        builder.Property(e => e.Strength).HasColumnName("strength").HasMaxLength(100);
        builder.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
        builder.Property(e => e.AtcCode).HasColumnName("atc_code").HasMaxLength(20);
        builder.Property(e => e.DrugCategory).HasColumnName("drug_category").HasMaxLength(50);
        builder.Property(e => e.IsControlled).HasColumnName("is_controlled").HasDefaultValue(false);
        builder.Property(e => e.IsAntibiotic).HasColumnName("is_antibiotic").HasDefaultValue(false);
        builder.Property(e => e.RequiresRx).HasColumnName("requires_rx").HasDefaultValue(true);
        builder.Property(e => e.SellPrice).HasColumnName("sell_price").HasColumnType("DECIMAL(12,2)");
        builder.Property(e => e.BhytPrice).HasColumnName("bhyt_price").HasColumnType("DECIMAL(12,2)");
        builder.Property(e => e.ReorderLevel).HasColumnName("reorder_level").HasDefaultValue(10);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Name });
        builder.HasIndex(e => new { e.TenantId, e.IsActive });
    }
}

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("diab_his_pha_stock");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.DrugId).HasColumnName("drug_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.LotNumber).HasColumnName("lot_number").HasMaxLength(50).IsRequired();
        builder.Property(e => e.MfgDate).HasColumnName("mfg_date");
        builder.Property(e => e.ExpDate).HasColumnName("exp_date").IsRequired();
        builder.Property(e => e.Quantity).HasColumnName("quantity").HasDefaultValue(0);
        builder.Property(e => e.ImportPrice).HasColumnName("import_price").HasColumnType("DECIMAL(12,2)");
        builder.Property(e => e.Location).HasColumnName("location").HasMaxLength(50);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        // Stock khong co deleted_at theo migration
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.DrugId });
        builder.HasIndex(e => new { e.TenantId, e.ExpDate });
    }
}

public class PrescriptionEntityConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("diab_his_pha_prescriptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DoctorId).HasColumnName("doctor_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.PrescriptionNo).HasColumnName("prescription_no").HasMaxLength(30);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("DRAFT");
        builder.Property(e => e.DtqgCode).HasColumnName("dtqg_code").HasMaxLength(50);
        builder.Property(e => e.DtqgQr).HasColumnName("dtqg_qr").HasMaxLength(500);
        builder.Property(e => e.DtqgPushedAt).HasColumnName("dtqg_pushed_at");
        builder.Property(e => e.DiagnosisIcd10).HasColumnName("diagnosis_icd10").HasMaxLength(10);
        builder.Property(e => e.SignedAt).HasColumnName("signed_at");
        builder.Property(e => e.DispensedAt).HasColumnName("dispensed_at");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => e.DtqgCode);

        builder.HasMany(e => e.Items)
            .WithOne(i => i.Prescription)
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("diab_his_pha_prescription_items");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PrescriptionId).HasColumnName("prescription_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DrugId).HasColumnName("drug_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DrugName).HasColumnName("drug_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.DrugStrength).HasColumnName("drug_strength").HasMaxLength(100);
        builder.Property(e => e.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Dosage).HasColumnName("dosage").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Frequency).HasColumnName("frequency").HasMaxLength(100);
        builder.Property(e => e.DurationDays).HasColumnName("duration_days");
        builder.Property(e => e.Route).HasColumnName("route").HasMaxLength(50);
        builder.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("DECIMAL(12,2)");
        builder.Property(e => e.LineTotal).HasColumnName("line_total").HasColumnType("DECIMAL(12,2)");
        builder.Property(e => e.BhytApplicable).HasColumnName("bhyt_applicable").HasDefaultValue(false);
        builder.Property(e => e.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        // prescription_items chi co created_at theo migration
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.PrescriptionId });
    }
}

public class DispenseConfiguration : IEntityTypeConfiguration<Dispense>
{
    public void Configure(EntityTypeBuilder<Dispense> builder)
    {
        builder.ToTable("diab_his_pha_dispenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PrescriptionId).HasColumnName("prescription_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DispensedBy).HasColumnName("dispensed_by").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DispensedAt).HasColumnName("dispensed_at");
        builder.Property(e => e.ItemsJson).HasColumnName("items_json").HasColumnType("JSON").IsRequired();
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.PrescriptionId });
        builder.HasIndex(e => new { e.TenantId, e.DispensedAt });
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("diab_his_pha_suppliers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContactName).HasColumnName("contact_name").HasMaxLength(100);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
        builder.Property(e => e.Address).HasColumnName("address");
        builder.Property(e => e.TaxCode).HasColumnName("tax_code").HasMaxLength(20);
        builder.Property(e => e.DrugLicense).HasColumnName("drug_license").HasMaxLength(50);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.IsActive });
    }
}

public class PurchaseOrderEntityConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("diab_his_pha_purchase_orders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PoNo).HasColumnName("po_no").HasMaxLength(30).IsRequired();
        builder.Property(e => e.SupplierId).HasColumnName("supplier_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("DRAFT");
        builder.Property(e => e.OrderDate).HasColumnName("order_date").IsRequired();
        builder.Property(e => e.ExpectedDate).HasColumnName("expected_date");
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.PoNo }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.SupplierId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class GrnConfiguration : IEntityTypeConfiguration<Grn>
{
    public void Configure(EntityTypeBuilder<Grn> builder)
    {
        builder.ToTable("diab_his_pha_grn");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.GrnNo).HasColumnName("grn_no").HasMaxLength(30).IsRequired();
        builder.Property(e => e.PoId).HasColumnName("po_id").HasMaxLength(36);
        builder.Property(e => e.SupplierId).HasColumnName("supplier_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.ReceivedDate).HasColumnName("received_date").IsRequired();
        builder.Property(e => e.InvoiceNo).HasColumnName("invoice_no").HasMaxLength(50);
        builder.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.ItemsJson).HasColumnName("items_json").HasColumnType("JSON").IsRequired();
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.GrnNo }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.PoId });
        builder.HasIndex(e => new { e.TenantId, e.SupplierId });
        builder.HasIndex(e => new { e.TenantId, e.ReceivedDate });
    }
}
