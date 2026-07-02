using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class PortalAccountConfiguration : IEntityTypeConfiguration<PortalAccount>
{
    public void Configure(EntityTypeBuilder<PortalAccount> builder)
    {
        builder.ToTable("diab_his_pat_portal_accounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone_e164").HasMaxLength(20).IsRequired();
        builder.Property(e => e.FailedAttempts).HasColumnName("failed_login_count").HasDefaultValue(0);
        builder.Property(e => e.LockedUntil).HasColumnName("locked_until");
        builder.Property(e => e.LastOtpSentAt).HasColumnName("last_login_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(36);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(36);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.Phone }).IsUnique();
        builder.HasIndex(e => e.PatientId);
    }
}

public class PortalOtpLogConfiguration : IEntityTypeConfiguration<PortalOtpLog>
{
    public void Configure(EntityTypeBuilder<PortalOtpLog> builder)
    {
        builder.ToTable("diab_his_pat_portal_otp_log");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(e => e.OtpHash).HasColumnName("otp_code_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Purpose).HasColumnName("purpose").HasMaxLength(20).IsRequired();
        builder.Property(e => e.SentAt).HasColumnName("created_at");
        builder.Property(e => e.VerifiedAt).HasColumnName("used_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("otp_expires_at");
        builder.Property(e => e.Attempts).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Ignore(e => e.CreatedAt);
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => e.ExpiresAt);
    }
}

public class PortalSessionConfiguration : IEntityTypeConfiguration<PortalSession>
{
    public void Configure(EntityTypeBuilder<PortalSession> builder)
    {
        builder.ToTable("diab_his_pat_portal_sessions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PatientId).HasColumnName("patient_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Jti).HasColumnName("jti").HasMaxLength(64).IsRequired();
        builder.Property(e => e.IssuedAt).HasColumnName("issued_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => e.Jti).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.PatientId });
    }
}
