namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Annotation (khoanh vùng/mũi tên/ghi chú) trên ảnh lâm sàng.
/// Layer JSON riêng, KHÔNG sửa ảnh gốc (non-destructive).
/// Map bảng diab_his_fil_file_annotations.
/// </summary>
public class FileAnnotation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public Guid FileId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? EncounterId { get; set; }

    /// <summary>
    /// JSON: danh sách shape { type: rectangle|circle|arrow|text, x, y, width, height, color, text }
    /// </summary>
    public string AnnotationData { get; set; } = "[]";

    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
