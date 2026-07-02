using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Ket qua xet nghiem. Maps diab_his_lab_results</summary>
public class LabResult : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string LabOrderId { get; set; } = string.Empty;
    public string? LabOrderItemId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string EncounterId { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal? ValueNumeric { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferenceRangeLow { get; set; }
    public decimal? ReferenceRangeHigh { get; set; }
    public string Flag { get; set; } = "NORMAL";
    public string? Method { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
    public string Status { get; set; } = "PRELIMINARY";
    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Note { get; set; }
    public string Source { get; set; } = "MANUAL";
}

/// <summary>Don vi xet nghiem doi tac. Maps diab_his_int_lab_partners</summary>
public class LabPartner : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string AuthType { get; set; } = "API_KEY";
    public byte[]? ApiKeyEncrypted { get; set; }
    public byte[]? BearerTokenEncrypted { get; set; }
    public string? ApiKeyMasked { get; set; }
    public string Transport { get; set; } = "REST";
    public string? SupportedTests { get; set; }  // JSON array
    public string Status { get; set; } = "INACTIVE";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}
