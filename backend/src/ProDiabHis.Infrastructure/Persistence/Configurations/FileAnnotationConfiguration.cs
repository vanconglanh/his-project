using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class FileAnnotationConfiguration : IEntityTypeConfiguration<FileAnnotation>
{
    public void Configure(EntityTypeBuilder<FileAnnotation> builder)
    {
        builder.ToTable("diab_his_fil_file_annotations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.FileId).HasColumnName("file_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36);
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36);
        builder.Property(e => e.AnnotationData).HasColumnName("annotation_data")
            .HasColumnType("json").IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.FileId });
        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
    }
}
