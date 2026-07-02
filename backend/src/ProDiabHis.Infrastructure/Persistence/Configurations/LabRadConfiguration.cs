using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class LabResultConfiguration : IEntityTypeConfiguration<LabResult>
{
    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("diab_his_lab_results");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.LabOrderId).HasColumnName("lab_order_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.LabOrderItemId).HasColumnName("lab_order_item_id").HasMaxLength(36);
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.TestCode).HasColumnName("test_code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.TestName).HasColumnName("test_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
        builder.Property(e => e.ValueNumeric).HasColumnName("value_numeric").HasColumnType("DECIMAL(12,4)");
        builder.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(50);
        builder.Property(e => e.ReferenceRangeLow).HasColumnName("reference_range_low").HasColumnType("DECIMAL(12,4)");
        builder.Property(e => e.ReferenceRangeHigh).HasColumnName("reference_range_high").HasColumnType("DECIMAL(12,4)");
        builder.Property(e => e.Flag).HasColumnName("flag").HasMaxLength(20).HasDefaultValue("NORMAL");
        builder.Property(e => e.Method).HasColumnName("method").HasMaxLength(100);
        builder.Property(e => e.PerformedAt).HasColumnName("performed_at");
        builder.Property(e => e.PerformedBy).HasColumnName("performed_by").HasMaxLength(36);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PRELIMINARY");
        builder.Property(e => e.VerifiedAt).HasColumnName("verified_at");
        builder.Property(e => e.VerifiedBy).HasColumnName("verified_by").HasMaxLength(36);
        builder.Property(e => e.Note).HasColumnName("note").HasColumnType("TEXT");
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(20).HasDefaultValue("MANUAL");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.LabOrderId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class LabPartnerConfiguration : IEntityTypeConfiguration<LabPartner>
{
    public void Configure(EntityTypeBuilder<LabPartner> builder)
    {
        builder.ToTable("diab_his_int_lab_partners");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(500).IsRequired();
        builder.Property(e => e.AuthType).HasColumnName("auth_type").HasMaxLength(30).HasDefaultValue("API_KEY");
        // HasConversion identity de tranh Pomelo 8.0.3 bug FindCollectionMapping NullRef voi byte[] properties
        builder.Property(e => e.ApiKeyEncrypted).HasColumnName("api_key_encrypted").HasColumnType("BLOB")
            .HasConversion(v => v, v => v);
        builder.Property(e => e.BearerTokenEncrypted).HasColumnName("bearer_token_encrypted").HasColumnType("BLOB")
            .HasConversion(v => v, v => v);
        builder.Property(e => e.ApiKeyMasked).HasColumnName("api_key_masked").HasMaxLength(100);
        builder.Property(e => e.Transport).HasColumnName("transport").HasMaxLength(20).HasDefaultValue("REST");
        builder.Property(e => e.SupportedTests).HasColumnName("supported_tests").HasColumnType("JSON");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("INACTIVE");
        builder.Property(e => e.ContactEmail).HasColumnName("contact_email").HasMaxLength(255);
        builder.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}


public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("diab_his_lab_orders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.TestCode).HasColumnName("test_code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.TestName).HasColumnName("test_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.SampleType).HasColumnName("sample_type").HasMaxLength(50);
        builder.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue("NORMAL");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ordered");
        builder.Property(e => e.OrderedAt).HasColumnName("ordered_at");
        builder.Property(e => e.OrderedBy).HasColumnName("ordered_by").HasMaxLength(36);
        builder.Property(e => e.ScheduledFor).HasColumnName("scheduled_for");
        builder.Property(e => e.LabPartnerId).HasColumnName("lab_partner_id").HasMaxLength(36);
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class RadOrderConfiguration : IEntityTypeConfiguration<RadOrder>
{
    public void Configure(EntityTypeBuilder<RadOrder> builder)
    {
        builder.ToTable("diab_his_rad_orders");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Modality).HasColumnName("modality").HasMaxLength(20).IsRequired();
        builder.Property(e => e.BodyPart).HasColumnName("body_part").HasMaxLength(100);
        builder.Property(e => e.Contrast).HasColumnName("contrast").HasDefaultValue(false);
        builder.Property(e => e.ProcedureCode).HasColumnName("procedure_code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProcedureName).HasColumnName("procedure_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(20).HasDefaultValue("NORMAL");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ordered");
        builder.Property(e => e.OrderedAt).HasColumnName("ordered_at");
        builder.Property(e => e.OrderedBy).HasColumnName("ordered_by").HasMaxLength(36);
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class ClsUploadConfiguration : IEntityTypeConfiguration<ClsUpload>
{
    public void Configure(EntityTypeBuilder<ClsUpload> builder)
    {
        builder.ToTable("diab_his_cls_uploads");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.PatientId).HasColumnName("patient_id");
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36);
        builder.Property(e => e.DocType).HasColumnName("doc_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.FileId).HasColumnName("file_id").HasMaxLength(36);
        builder.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(500).IsRequired();
        builder.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100);
        builder.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(36);
        builder.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.DocType });
    }
}
