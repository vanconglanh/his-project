using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities.Bhyt;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class BhytExportConfiguration : IEntityTypeConfiguration<BhytExport>
{
    public void Configure(EntityTypeBuilder<BhytExport> builder)
    {
        builder.ToTable("diab_his_int_bhyt_exports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PeriodMonth).HasColumnName("period_month").HasMaxLength(7).IsRequired();
        builder.Property(e => e.ScopeFilterJson).HasColumnName("scope_filter_json").HasColumnType("JSON");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("DRAFT");
        builder.Property(e => e.EncounterCount).HasColumnName("encounter_count").HasDefaultValue(0);
        builder.Property(e => e.TotalRequestedAmount).HasColumnName("total_requested_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.TotalApprovedAmount).HasColumnName("total_approved_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.TotalRejectedAmount).HasColumnName("total_rejected_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.XmlFilePath).HasColumnName("xml_file_path").HasMaxLength(500);
        builder.Property(e => e.ResponseMessage).HasColumnName("response_message");
        builder.Property(e => e.BhytReference).HasColumnName("bhyt_reference").HasMaxLength(100);
        builder.Property(e => e.GeneratedAt).HasColumnName("generated_at");
        builder.Property(e => e.ValidatedAt).HasColumnName("validated_at");
        builder.Property(e => e.SignedAt).HasColumnName("signed_at");
        builder.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(e => e.ResponseAt).HasColumnName("response_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.PeriodMonth }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}

public class BhytExportItemConfiguration : IEntityTypeConfiguration<BhytExportItem>
{
    public void Configure(EntityTypeBuilder<BhytExportItem> builder)
    {
        builder.ToTable("diab_his_int_bhyt_export_items");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ExportId).HasColumnName("export_id").IsRequired();
        builder.Property(e => e.TableNo).HasColumnName("table_no").IsRequired();
        builder.Property(e => e.RecordIndex).HasColumnName("record_index").HasDefaultValue(0);
        builder.Property(e => e.RowDataJson).HasColumnName("payload_json").HasColumnType("JSON");
        builder.Property(e => e.SourceEncounterId).HasColumnName("source_encounter_id").HasMaxLength(36);
        builder.Property(e => e.SourceBillingId).HasColumnName("source_billing_id").HasMaxLength(36);
        builder.Property(e => e.MaLienKet).HasColumnName("ma_lien_ket").HasMaxLength(200);
        builder.Property(e => e.RequestAmount).HasColumnName("request_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.ApprovedAmount).HasColumnName("approved_amount").HasColumnType("DECIMAL(15,2)");
        builder.Property(e => e.RejectionCode).HasColumnName("rejection_code").HasMaxLength(50);
        builder.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => new { e.ExportId, e.TableNo }).IsUnique();
        builder.HasIndex(e => e.ExportId);
    }
}

public class BhytReconcileUploadConfiguration : IEntityTypeConfiguration<BhytReconcileUpload>
{
    public void Configure(EntityTypeBuilder<BhytReconcileUpload> builder)
    {
        builder.ToTable("diab_his_int_bhyt_reconcile_uploads");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ExportId).HasColumnName("export_id").IsRequired();
        builder.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(500).IsRequired();
        builder.Property(e => e.FileSize).HasColumnName("file_size");
        builder.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
        builder.Property(e => e.ParsedAt).HasColumnName("parsed_at");
        builder.Property(e => e.ParseStatus).HasColumnName("parse_status").HasMaxLength(20).HasDefaultValue("PENDING");
        builder.Property(e => e.ParseError).HasColumnName("parse_error");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => e.ExportId);
        builder.HasIndex(e => new { e.TenantId, e.ParseStatus });
    }
}

public class BhytReconcileItemConfiguration : IEntityTypeConfiguration<BhytReconcileItem>
{
    public void Configure(EntityTypeBuilder<BhytReconcileItem> builder)
    {
        builder.ToTable("diab_his_int_bhyt_reconcile_items");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.UploadId).HasColumnName("upload_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.ExportId).HasColumnName("export_id").IsRequired();
        builder.Property(e => e.ExportItemId).HasColumnName("export_item_id");
        builder.Property(e => e.TableNo).HasColumnName("table_no").IsRequired();
        builder.Property(e => e.MaLienKet).HasColumnName("ma_lien_ket").HasMaxLength(200).IsRequired();
        builder.Property(e => e.RequestAmount).HasColumnName("request_amount").HasColumnType("DECIMAL(18,2)");
        builder.Property(e => e.ApprovedAmount).HasColumnName("approved_amount").HasColumnType("DECIMAL(18,2)");
        builder.Property(e => e.RejectedAmount).HasColumnName("rejected_amount").HasColumnType("DECIMAL(18,2)");
        builder.Property(e => e.RejectionCode).HasColumnName("rejection_code").HasMaxLength(50);
        builder.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("APPROVED");
        builder.Property(e => e.DisputeReason).HasColumnName("dispute_reason");
        builder.Property(e => e.DisputeEvidencePath).HasColumnName("dispute_evidence_path").HasMaxLength(500);
        builder.Property(e => e.DisputedAt).HasColumnName("disputed_at");
        builder.Property(e => e.DisputedBy).HasColumnName("disputed_by").HasMaxLength(36);
        builder.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(e => e.AcceptedBy).HasColumnName("accepted_by").HasMaxLength(36);
        builder.Property(e => e.Note).HasColumnName("note").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => new { e.ExportId, e.Status });
        builder.HasIndex(e => e.UploadId);
        builder.HasIndex(e => e.MaLienKet);
        builder.HasIndex(e => new { e.TenantId, e.TableNo });
    }
}
