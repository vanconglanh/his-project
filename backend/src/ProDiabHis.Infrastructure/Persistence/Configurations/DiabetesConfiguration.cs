using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class DiabetesAssessmentConfiguration : IEntityTypeConfiguration<DiabetesAssessment>
{
    public void Configure(EntityTypeBuilder<DiabetesAssessment> builder)
    {
        builder.ToTable("diab_his_cli_diabetes_assessments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        // string properties: dung HasMaxLength thay vi HasColumnType("char(36)") vi Pomelo khong map string->char
        builder.Property(e => e.EncounterId).HasColumnName("encounter_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Hba1c).HasColumnName("hba1c").HasColumnType("DECIMAL(4,2)");
        builder.Property(e => e.FastingGlucose).HasColumnName("fasting_glucose").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.PostprandialGlucose).HasColumnName("postprandial_glucose").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.RandomGlucose).HasColumnName("random_glucose").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.Egfr).HasColumnName("egfr").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.SerumCreatinine).HasColumnName("serum_creatinine").HasColumnType("DECIMAL(6,2)");
        builder.Property(e => e.UrineAcr).HasColumnName("urine_acr").HasColumnType("DECIMAL(8,2)");
        builder.Property(e => e.BpSystolic).HasColumnName("bp_systolic");
        builder.Property(e => e.BpDiastolic).HasColumnName("bp_diastolic");
        builder.Property(e => e.Bmi).HasColumnName("bmi").HasColumnType("DECIMAL(4,1)");
        builder.Property(e => e.WaistCircumference).HasColumnName("waist_circumference").HasColumnType("DECIMAL(5,1)");
        builder.Property(e => e.DiabetesType).HasColumnName("diabetes_type").HasMaxLength(20);
        builder.Property(e => e.ComplicationsJson).HasColumnName("complications_json").HasColumnType("JSON");
        builder.Property(e => e.TreatmentTargetJson).HasColumnName("treatment_target_json").HasColumnType("JSON");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.AssessedAt).HasColumnName("assessed_at");
        builder.Property(e => e.AssessedBy).HasColumnName("assessed_by").HasMaxLength(36);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.PatientId, e.AssessedAt });
        builder.HasIndex(e => e.EncounterId);
        builder.HasIndex(e => new { e.TenantId, e.AssessedAt });
    }
}

public class DiabetesTemplateConfiguration : IEntityTypeConfiguration<DiabetesTemplate>
{
    public void Configure(EntityTypeBuilder<DiabetesTemplate> builder)
    {
        builder.ToTable("diab_his_cli_diabetes_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.DefaultValuesJson).HasColumnName("template_json").HasColumnType("JSON");
        builder.Property(e => e.ChecklistJson).HasColumnName("checklist_json").HasColumnType("JSON");
        builder.Property(e => e.IsSystem).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.IsSystem });
    }
}
