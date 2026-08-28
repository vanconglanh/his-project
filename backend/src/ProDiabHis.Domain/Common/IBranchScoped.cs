namespace ProDiabHis.Domain.Common;

/// <summary>
/// Interface danh dau entity thuoc ve mot chi nhanh (branch) trong tenant.
/// Dung int? (nullable) de tuong thich giai doan migrate: NULL = du lieu chung/
/// truoc khi tach chi nhanh, luon thay o moi branch context (xem AppDbContext query filter).
/// </summary>
public interface IBranchScoped
{
    int? BranchId { get; set; }
}
