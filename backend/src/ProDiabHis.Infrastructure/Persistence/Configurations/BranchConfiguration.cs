using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("diab_his_sys_branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(b => b.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(b => b.ClinicId).HasColumnName("clinic_id");
        builder.Property(b => b.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(b => b.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(b => b.CskcbCode).HasColumnName("cskcb_code").HasMaxLength(20);
        builder.Property(b => b.Address).HasColumnName("address");
        builder.Property(b => b.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(b => b.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(b => b.WorkingHours).HasColumnName("working_hours").HasMaxLength(255);
        builder.Property(b => b.Timezone).HasColumnName("timezone").HasMaxLength(50).HasDefaultValue("Asia/Ho_Chi_Minh");
        builder.Property(b => b.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(b => b.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(b => b.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(b => b.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue(Domain.Entities.BranchStatus.Active);
        builder.Property(b => b.GroupId).HasColumnName("group_id");
        builder.Property(b => b.HospitalRank).HasColumnName("hospital_rank").HasMaxLength(50);
        builder.Property(b => b.KcbTuyen).HasColumnName("kcb_tuyen").HasMaxLength(50);
        builder.Property(b => b.BhytContractCode).HasColumnName("bhyt_contract_code").HasMaxLength(100);
        builder.Property(b => b.BhytContractValidFrom).HasColumnName("bhyt_contract_valid_from");
        builder.Property(b => b.BhytContractValidTo).HasColumnName("bhyt_contract_valid_to");
        builder.Property(b => b.BhytEnabled).HasColumnName("bhyt_enabled").HasDefaultValue(false);
        builder.Property(b => b.DtqgEnabled).HasColumnName("dtqg_enabled").HasDefaultValue(false);
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.CreatedBy).HasColumnName("created_by");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");
        builder.Property(b => b.UpdatedBy).HasColumnName("updated_by");
        builder.Property(b => b.DeletedAt).HasColumnName("deleted_at");
        builder.Property(b => b.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique();
        builder.HasIndex(b => new { b.TenantId, b.IsActive, b.SortOrder });
        builder.HasIndex(b => new { b.TenantId, b.IsDefault });
    }
}

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("diab_his_sec_user_branches");

        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.Id).HasColumnName("id");
        builder.Property(ub => ub.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(ub => ub.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(ub => ub.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(ub => ub.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        builder.Property(ub => ub.CreatedAt).HasColumnName("created_at");
        builder.Property(ub => ub.CreatedBy).HasColumnName("created_by");
        builder.Property(ub => ub.UpdatedAt).HasColumnName("updated_at");
        builder.Property(ub => ub.UpdatedBy).HasColumnName("updated_by");
        builder.Property(ub => ub.DeletedAt).HasColumnName("deleted_at");
        builder.Ignore(ub => ub.DeletedBy); // bang khong co cot deleted_by

        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique();
        builder.HasIndex(ub => new { ub.TenantId, ub.BranchId });
        builder.HasIndex(ub => new { ub.TenantId, ub.UserId, ub.IsPrimary });
    }
}
