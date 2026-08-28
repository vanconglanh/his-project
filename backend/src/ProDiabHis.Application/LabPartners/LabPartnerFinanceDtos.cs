namespace ProDiabHis.Application.LabPartners;

// ═══════════════════════════════════════════════
// FR-511: Canh bao qua han SLA
// ═══════════════════════════════════════════════
public record LabOrderOverdueResponse(
    Guid     Id,
    Guid     EncounterId,
    Guid?    PatientId,
    string?  PatientName,
    string   TestCode,
    string   TestName,
    string   Status,
    Guid?    LabPartnerId,
    string?  LabPartnerName,
    int      SlaDays,
    DateTime OrderedAt,
    DateTime DueDate,
    int      DaysOverdue,
    int?     BranchId);

// ═══════════════════════════════════════════════
// FR-512: Chi phi/hoa hong tung LabOrder
// ═══════════════════════════════════════════════
public record LabPartnerCostResponse(
    Guid     Id,
    Guid     LabPartnerId,
    Guid     LabOrderId,
    string   TestCode,
    decimal  CostAmount,
    string   Currency,
    DateTime IncurredAt,
    string   PeriodMonth,
    Guid?    ReconciliationId,
    string?  Note,
    DateTime CreatedAt);

public record CreateLabPartnerCostRequest(
    Guid     LabOrderId,
    decimal? CostAmount,
    string?  Note);

public record UpdateLabPartnerCostRequest(
    decimal CostAmount,
    string? Note);

// ═══════════════════════════════════════════════
// FR-512: Ky doi soat cong no theo thang
// ═══════════════════════════════════════════════
public record LabPartnerReconciliationResponse(
    Guid     Id,
    Guid     LabPartnerId,
    string   LabPartnerName,
    string   PeriodMonth,
    int      TotalOrders,
    decimal  TotalCost,
    string   Currency,
    string   Status,
    DateTime? ConfirmedAt,
    DateTime? PaidAt,
    string?  Note,
    DateTime CreatedAt);

public record CreateLabPartnerReconciliationRequest(string PeriodMonth, string? Note);

public record UpdateLabPartnerReconciliationStatusRequest(string Status, string? Note);
