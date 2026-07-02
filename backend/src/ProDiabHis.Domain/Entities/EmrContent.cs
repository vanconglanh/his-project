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
    public string Speciality { get; set; } = "GENERAL";
    public bool IsSystem { get; set; }
}
