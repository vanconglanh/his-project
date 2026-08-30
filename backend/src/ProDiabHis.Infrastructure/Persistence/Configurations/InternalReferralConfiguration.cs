using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class InternalReferralConfiguration : IEntityTypeConfiguration<InternalReferral>
{
    public void Configure(EntityTypeBuilder<InternalReferral> builder)
    {
        builder.ToTable("diab_his_clinic_internal_referrals");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(r => r.SourceBranchId).HasColumnName("source_branch_id").IsRequired();
        builder.Property(r => r.TargetBranchId).HasColumnName("target_branch_id").IsRequired();
        builder.Property(r => r.EncounterId).HasColumnName("encounter_id").HasMaxLength(36);
        builder.Property(r => r.ReferringDoctorId).HasColumnName("referring_doctor_id");
        builder.Property(r => r.Reason).HasColumnName("reason");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue(InternalReferralStatus.Sent);
        builder.Property(r => r.Note).HasColumnName("note");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.CreatedBy).HasColumnName("created_by");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(r => new { r.TenantId, r.TargetBranchId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.PatientId });
    }
}
