namespace ProDiabHis.Domain.Common;

/// <summary>Interface danh dau entity co soft delete</summary>
public interface ISoftDelete
{
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
