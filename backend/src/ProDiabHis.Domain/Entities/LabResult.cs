using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Ket qua xet nghiem. Maps diab_his_lab_results</summary>
public class LabResult : BaseEntity, ITenantScoped, IBranchScoped
{
    public int TenantId { get; set; }
    public int? BranchId { get; set; }
    public string LabOrderId { get; set; } = string.Empty;
    /// <summary>Cot legacy order_id (NOT NULL) trong diab_his_lab_results, dung boi cac report join.
    /// Luon set = LabOrderId (id chi dinh XN) de dam bao NOT NULL va nhat quan voi du lieu cu.</summary>
    public string OrderId { get; set; } = string.Empty;
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
    public int SlaDays { get; set; } = 3;
    public decimal? DefaultCostAmount { get; set; }
}

/// <summary>Chi phi/hoa hong tra doi tac cho 1 LabOrder. Maps diab_his_int_lab_partner_costs</summary>
public class LabPartnerCost : BaseEntity, ITenantScoped, IBranchScoped
{
    public int TenantId { get; set; }
    public int? BranchId { get; set; }
    public string LabPartnerId { get; set; } = string.Empty;
    public string LabOrderId { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public decimal CostAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime IncurredAt { get; set; } = DateTime.UtcNow;
    public string PeriodMonth { get; set; } = string.Empty; // YYYY-MM
    public string? ReconciliationId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Ky doi soat cong no/hoa hong voi doi tac XN theo thang. Maps diab_his_int_lab_partner_reconciliations</summary>
public class LabPartnerReconciliation : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string LabPartnerId { get; set; } = string.Empty;
    public string PeriodMonth { get; set; } = string.Empty; // YYYY-MM
    public int TotalOrders { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = LabPartnerReconciliationStatus.Draft;
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidBy { get; set; }
    public string? Note { get; set; }
}

public static class LabPartnerReconciliationStatus
{
    public const string Draft     = "draft";
    public const string Confirmed = "confirmed";
    public const string Paid      = "paid";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Draft]     = new[] { Confirmed },
        [Confirmed] = new[] { Paid },
        [Paid]      = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
