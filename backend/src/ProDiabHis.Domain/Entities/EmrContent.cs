using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>EMR content (1 per encounter). Maps diab_his_cli_emr_content</summary>
public class EmrContent : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    public string? ContentHtml { get; set; }
    public string? TemplateId { get; set; }
    /// <summary>§5.8.1 (QD4) — PHI: gia tri form dang soan (working copy) {key: value}. Migration 9182.</summary>
    public string? StructuredValuesJson { get; set; }
    public int Version { get; set; } = 1;
    public DateTime? SignedAt { get; set; }
    public string? SignedBy { get; set; }
}

/// <summary>EMR version snapshot. Maps diab_his_cli_emr_versions</summary>
public class EmrVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmrId { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int Version { get; set; }
    public string ContentJson { get; set; } = "{}";
    /// <summary>§5.8.1 (QD4) — Mau benh an da dung (tham chieu logic, KHONG FK cung). Chi truy vet/bao cao. Migration 9182.</summary>
    public string? TemplateId { get; set; }
    /// <summary>§5.8.1 (QD4) — PHI: gia tri form cua PHIEN BAN nay {key: value}. Migration 9182.</summary>
    public string? StructuredValuesJson { get; set; }
    /// <summary>§5.8.2 (QD5) — Chup nguyen ven EmrTemplate.structured_json tai thoi diem tao ban ghi.
    /// LUON render benh an theo cot nay, KHONG doc lai template hien tai. NULL = ban ghi truoc migration 9182. Migration 9182.</summary>
    public string? SchemaSnapshotJson { get; set; }
    public int BytesSize { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public string? SavedBy { get; set; }
    public bool IsSigned { get; set; }
}

/// <summary>EMR digital signature. Maps diab_his_cli_emr_signatures</summary>
public class EmrSignature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TenantId { get; set; }
    public string EmrId { get; set; } = string.Empty;
    public string EncounterId { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignedBy { get; set; } = string.Empty;
    public string? CertificateSerial { get; set; }
    public string? CertificateSubject { get; set; }
    public string SignatureAlgorithm { get; set; } = "SHA256withRSA";
    public byte[] SignatureData { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>EMR template. Maps diab_his_cli_emr_templates</summary>
public class EmrTemplate : BaseEntity
{
    public int? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    /// <summary>§5.7.2 / §5.8.1 — DINH NGHIA cau truc form (mang field). Danh muc dung chung, KHONG chua PHI. Migration 9181.</summary>
    public string? StructuredJson { get; set; }
    public string Speciality { get; set; } = "GENERAL";
    public bool IsSystem { get; set; }
    /// <summary>§5.7 — Template mac dinh cua tenant theo speciality (goi y khi mo man kham). Migration 9181.</summary>
    public bool IsDefault { get; set; }
}
