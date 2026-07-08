namespace ProDiabHis.Domain.Common;

/// <summary>
/// Danh dau entity co cot thoi gian audit (created_at / updated_at) de
/// AppDbContext.SaveChangesAsync tu dong stamp. Tach rieng khoi BaseEntity
/// vi Tenant dung int PK + int audit cols (khong theo Guid cua BaseEntity).
/// </summary>
public interface IAuditTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
