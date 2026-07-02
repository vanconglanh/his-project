using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("diab_his_nti_notifications");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(e => e.Body).HasColumnName("body").IsRequired();
        builder.Property(e => e.DataJson).HasColumnName("data_json").HasColumnType("JSON");
        builder.Property(e => e.ReadAt).HasColumnName("read_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        // Notification chi co created_at theo migration 0050
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.UserId, e.CreatedAt });
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.ReadAt });
    }
}

public class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription>
{
    public void Configure(EntityTypeBuilder<WebPushSubscription> builder)
    {
        builder.ToTable("diab_his_nti_web_push_subs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Endpoint).HasColumnName("endpoint").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.P256dhKey).HasColumnName("p256dh_key").HasMaxLength(200).IsRequired();
        builder.Property(e => e.AuthKey).HasColumnName("auth_key").HasMaxLength(100).IsRequired();
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedAt);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => e.Endpoint).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.UserId });
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("diab_his_nti_preferences");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(36).IsRequired();
        builder.Property(e => e.Position).HasColumnName("position").HasMaxLength(20).HasDefaultValue("TOP_RIGHT");
        builder.Property(e => e.SoundEnabled).HasColumnName("sound_enabled").HasDefaultValue(true);
        builder.Property(e => e.SoundName).HasColumnName("sound_name").HasMaxLength(50).HasDefaultValue("default");
        builder.Property(e => e.BrowserPushEnabled).HasColumnName("browser_push_enabled").HasDefaultValue(false);
        builder.Property(e => e.TypesDisabledJson).HasColumnName("types_disabled").HasColumnType("JSON");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();
    }
}

public class VapidKeyConfiguration : IEntityTypeConfiguration<VapidKey>
{
    public void Configure(EntityTypeBuilder<VapidKey> builder)
    {
        builder.ToTable("diab_his_nti_vapid_keys");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasMaxLength(36);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.PublicKey).HasColumnName("public_key").HasMaxLength(255).IsRequired();
        // HasConversion identity de tranh Pomelo 8.0.3 bug FindCollectionMapping NullRef voi byte[] properties
        builder.Property(e => e.PrivateKeyEncrypted).HasColumnName("private_key_encrypted")
            .HasColumnType("VARBINARY(512)").IsRequired()
            .HasConversion(v => v, v => v);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(e => e.CreatedBy);
        builder.Ignore(e => e.UpdatedBy);
        builder.Ignore(e => e.DeletedAt);
        builder.Ignore(e => e.DeletedBy);

        builder.HasIndex(e => e.TenantId).IsUnique();
    }
}
