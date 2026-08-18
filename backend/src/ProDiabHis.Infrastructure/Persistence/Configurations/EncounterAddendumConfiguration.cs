using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

/// <summary>[G03] Ban dinh chinh benh an — bang diab_his_cli_encounter_addenda.</summary>
public class EncounterAddendumConfiguration : IEntityTypeConfiguration<EncounterAddendum>
{
    public void Configure(EntityTypeBuilder<EncounterAddendum> builder)
    {
        builder.ToTable("diab_his_cli_encounter_addenda");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Section).HasColumnName("section").HasMaxLength(30).IsRequired();
        builder.Property(e => e.TargetTable).HasColumnName("target_table").HasMaxLength(64);
        builder.Property(e => e.TargetId).HasColumnName("target_id").HasMaxLength(36);
        builder.Property(e => e.Operation).HasColumnName("operation").HasMaxLength(10).HasDefaultValue("UPDATE");
        builder.Property(e => e.ContentBefore).HasColumnName("content_before").HasColumnType("JSON");
        builder.Property(e => e.ContentAfter).HasColumnName("content_after").HasColumnType("JSON");
        builder.Property(e => e.Reason).HasColumnName("reason").IsRequired();
        builder.Property(e => e.BhytSubmittedFlag).HasColumnName("bhyt_submitted_flag").HasDefaultValue(false);
        builder.Property(e => e.BhytExportId).HasColumnName("bhyt_export_id");
        builder.Property(e => e.BhytResubmitAt).HasColumnName("bhyt_resubmit_at");
        builder.Property(e => e.AuditLogId).HasColumnName("audit_log_id").HasMaxLength(36);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId, e.CreatedAt });
        builder.HasIndex(e => new { e.TenantId, e.Section, e.CreatedAt });
    }
}
