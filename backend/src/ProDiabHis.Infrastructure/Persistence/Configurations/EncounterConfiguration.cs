using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("diab_his_enc_encounters");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.DoctorId).HasColumnName("doctor_id").HasMaxLength(36);
        builder.Property(e => e.RoomId).HasColumnName("room_id").HasMaxLength(36);
        builder.Property(e => e.EncounterType).HasColumnName("encounter_type").HasMaxLength(20).HasDefaultValue("FIRST_VISIT");
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("WAITING");
        builder.Property(e => e.ReasonForVisit).HasColumnName("reason_for_visit").HasMaxLength(500);
        builder.Property(e => e.ChiefComplaint).HasColumnName("chief_complaint");
        builder.Property(e => e.StartedAt).HasColumnName("started_at");
        builder.Property(e => e.FinishedAt).HasColumnName("finished_at");
        builder.Property(e => e.AlertSentAt).HasColumnName("alert_sent_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.DoctorId });
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt });
    }
}

public class EncounterDiagnosisConfiguration : IEntityTypeConfiguration<EncounterDiagnosis>
{
    public void Configure(EntityTypeBuilder<EncounterDiagnosis> builder)
    {
        builder.ToTable("diab_his_enc_diagnoses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Icd10Code).HasColumnName("icd10_code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("PRIMARY");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.Icd10Code });
    }
}

public class VitalSignsConfiguration : IEntityTypeConfiguration<VitalSigns>
{
    public void Configure(EntityTypeBuilder<VitalSigns> builder)
    {
        builder.ToTable("diab_his_enc_vital_signs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.RecordedAt).HasColumnName("recorded_at");
        builder.Property(e => e.RecordedBy).HasColumnName("recorded_by").HasMaxLength(36);
        builder.Property(e => e.RecordSequence).HasColumnName("record_sequence").HasDefaultValue(1);
        builder.Property(e => e.TemperatureC).HasColumnName("temperature_c").HasColumnType("DECIMAL(4,1)");
        builder.Property(e => e.HeartRateBpm).HasColumnName("heart_rate_bpm");
        builder.Property(e => e.RespiratoryRate).HasColumnName("respiratory_rate");
        builder.Property(e => e.BpSystolic).HasColumnName("bp_systolic");
        builder.Property(e => e.BpDiastolic).HasColumnName("bp_diastolic");
        builder.Property(e => e.Spo2Percent).HasColumnName("spo2_percent");
        builder.Property(e => e.WeightKg).HasColumnName("weight_kg").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.HeightCm).HasColumnName("height_cm").HasColumnType("DECIMAL(5,1)");
        builder.Property(e => e.PainScale).HasColumnName("pain_scale");
        builder.Property(e => e.GlucoseMgDl).HasColumnName("glucose_mg_dl").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.EncounterId });
        builder.HasIndex(e => new { e.TenantId, e.PatientId });
    }
}

public class EmrContentConfiguration : IEntityTypeConfiguration<EmrContent>
{
    public void Configure(EntityTypeBuilder<EmrContent> builder)
    {
        builder.ToTable("diab_his_enc_emr_contents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.ContentJson).HasColumnName("content_json").HasColumnType("MEDIUMTEXT").IsRequired();
        builder.Property(e => e.ContentHtml).HasColumnName("content_html").HasColumnType("MEDIUMTEXT");
        builder.Property(e => e.TemplateId).HasColumnName("template_id").HasMaxLength(36);
        builder.Property(e => e.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(e => e.SignedAt).HasColumnName("signed_at");
        builder.Property(e => e.SignedBy).HasColumnName("signed_by").HasMaxLength(36);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => e.EncounterId).IsUnique();
        builder.HasIndex(e => e.TenantId);
    }
}

public class EmrVersionConfiguration : IEntityTypeConfiguration<EmrVersion>
{
    public void Configure(EntityTypeBuilder<EmrVersion> builder)
    {
        builder.ToTable("diab_his_cli_emr_versions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.EmrId).HasColumnName("emr_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Version).HasColumnName("version").IsRequired();
        builder.Property(e => e.ContentJson).HasColumnName("content_json").HasColumnType("MEDIUMTEXT").IsRequired();
        builder.Property(e => e.BytesSize).HasColumnName("bytes_size").HasDefaultValue(0);
        builder.Property(e => e.SavedAt).HasColumnName("saved_at");
        builder.Property(e => e.SavedBy).HasColumnName("saved_by").HasMaxLength(36);
        builder.Property(e => e.IsSigned).HasColumnName("is_signed").HasDefaultValue(false);

        builder.HasIndex(e => new { e.EmrId, e.Version });
        builder.HasIndex(e => e.TenantId);
    }
}

public class EmrSignatureConfiguration : IEntityTypeConfiguration<EmrSignature>
{
    public void Configure(EntityTypeBuilder<EmrSignature> builder)
    {
        builder.ToTable("diab_his_cli_emr_signatures");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.EmrId).HasColumnName("emr_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.SignedAt).HasColumnName("signed_at").IsRequired();
        builder.Property(e => e.SignedBy).HasColumnName("signed_by").HasMaxLength(36).IsRequired();
        builder.Property(e => e.CertificateSerial).HasColumnName("certificate_serial").HasMaxLength(100);
        builder.Property(e => e.CertificateSubject).HasColumnName("certificate_subject").HasMaxLength(500);
        builder.Property(e => e.SignatureAlgorithm).HasColumnName("signature_algorithm").HasMaxLength(50).HasDefaultValue("SHA256withRSA");
        // HasConversion identity de tranh Pomelo 8.0.3 bug FindCollectionMapping NullRef voi byte[] properties
        builder.Property(e => e.SignatureData).HasColumnName("signature_data").HasColumnType("BLOB").IsRequired()
            .HasConversion(v => v, v => v);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(e => e.EmrId);
        builder.HasIndex(e => e.EncounterId);
    }
}

public class EmrTemplateConfiguration : IEntityTypeConfiguration<EmrTemplate>
{
    public void Configure(EntityTypeBuilder<EmrTemplate> builder)
    {
        builder.ToTable("diab_his_cli_emr_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentJson).HasColumnName("content_json").HasColumnType("MEDIUMTEXT").IsRequired();
        builder.Property(e => e.Speciality).HasColumnName("speciality").HasMaxLength(50).HasDefaultValue("GENERAL");
        builder.Property(e => e.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.Speciality });
        builder.HasIndex(e => e.IsSystem);
    }
}
