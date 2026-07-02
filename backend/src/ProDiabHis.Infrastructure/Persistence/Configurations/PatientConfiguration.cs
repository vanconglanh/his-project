using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("diab_his_pat_patients");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(10);
        builder.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(e => e.IdNumberEnc).HasColumnName("id_number_enc").HasMaxLength(500);
        builder.Property(e => e.IdNumberMasked).HasColumnName("id_number_masked").HasMaxLength(20);
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
        builder.Property(e => e.ProvinceCode).HasColumnName("province_code").HasMaxLength(10);
        builder.Property(e => e.DistrictCode).HasColumnName("district_code").HasMaxLength(10);
        builder.Property(e => e.WardCode).HasColumnName("ward_code").HasMaxLength(10);
        builder.Property(e => e.Street).HasColumnName("street").HasMaxLength(255);
        builder.Property(e => e.Occupation).HasColumnName("occupation").HasMaxLength(100);
        builder.Property(e => e.Ethnicity).HasColumnName("ethnicity").HasMaxLength(50);
        builder.Property(e => e.BloodType).HasColumnName("blood_type").HasMaxLength(5);
        builder.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
        builder.Property(e => e.ReceptionNote).HasColumnName("reception_note");
        builder.Property(e => e.AllergiesSummary).HasColumnName("allergies_summary").HasMaxLength(500);
        builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("ACTIVE");
        builder.Property(e => e.IdCardIssuedDate).HasColumnName("id_card_issued_date");
        builder.Property(e => e.IdCardIssuedPlace).HasColumnName("id_card_issued_place").HasMaxLength(100);
        builder.Property(e => e.Nationality).HasColumnName("nationality").HasMaxLength(5).HasDefaultValue("VN");
        builder.Property(e => e.PatientType).HasColumnName("patient_type").HasMaxLength(20).HasDefaultValue("SERVICE");
        builder.Property(e => e.MaritalStatus).HasColumnName("marital_status").HasMaxLength(20);
        builder.Property(e => e.VisitType).HasColumnName("visit_type").HasMaxLength(20);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(36);

        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.FullName });
        builder.HasIndex(e => new { e.TenantId, e.Phone });
    }
}

public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.ToTable("diab_his_pat_allergies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id");
        builder.Property(e => e.Allergen).HasColumnName("allergen").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Reaction).HasColumnName("reaction").HasMaxLength(255);
        builder.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        builder.Property(e => e.OnsetDate).HasColumnName("onset_date");
        builder.Property(e => e.Note).HasColumnName("note");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
    }
}

public class InsuranceConfiguration : IEntityTypeConfiguration<Insurance>
{
    public void Configure(EntityTypeBuilder<Insurance> builder)
    {
        builder.ToTable("diab_his_pat_insurances");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id");
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(20).HasDefaultValue("BHYT");
        builder.Property(e => e.CardNoEnc).HasColumnName("card_no_enc").HasMaxLength(500).IsRequired();
        builder.Property(e => e.CardNoMasked).HasColumnName("card_no_masked").HasMaxLength(30);
        builder.Property(e => e.ValidFrom).HasColumnName("valid_from");
        builder.Property(e => e.ValidTo).HasColumnName("valid_to");
        builder.Property(e => e.HospitalCode).HasColumnName("hospital_code").HasMaxLength(20);
        builder.Property(e => e.CoveragePercent).HasColumnName("coverage_percent");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.ValidTo });
    }
}

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("diab_his_pat_emergency_contacts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id");
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Relationship).HasColumnName("relationship").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
    }
}

public class ConsentConfiguration : IEntityTypeConfiguration<Consent>
{
    public void Configure(EntityTypeBuilder<Consent> builder)
    {
        builder.ToTable("diab_his_pat_consents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id");
        builder.Property(e => e.ConsentType).HasColumnName("consent_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.SignedAt).HasColumnName("signed_at").IsRequired();
        builder.Property(e => e.SignedBy).HasColumnName("signed_by").HasMaxLength(255);
        builder.Property(e => e.DocumentFileId).HasColumnName("document_file_id").HasMaxLength(36);
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.ConsentType });
    }
}
