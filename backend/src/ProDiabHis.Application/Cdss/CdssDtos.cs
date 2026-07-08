namespace ProDiabHis.Application.Cdss;

// ─── Context evaluate CDSS ────────────────────────────────────────────────────
public record PrescribedDrug(string DrugId, string? Ingredient, string? AtcCode);

public record CdssEvaluationContext(
    int TenantId,
    Guid? PatientId,
    Guid? EncounterId,
    Guid? PrescriptionId,
    IReadOnlyList<PrescribedDrug> Drugs);

// ─── Alert output ──────────────────────────────────────────────────────────
public record CdssAlert(
    string RuleType,
    string? RuleCode,
    string Severity,
    bool IsInterruptive,
    string Title,
    string Detail,
    string? Management,
    IReadOnlyList<string> DrugRefs);

public record CdssCheckResponse(IReadOnlyList<CdssAlert> Alerts, bool HasInterruptive);

// ─── Request DTO tu Controller ────────────────────────────────────────────────
public record CdssDrugInput(string? DrugId, string? Ingredient, string? AtcCode);

public record CdssCheckRequest(
    Guid? PatientId,
    Guid? EncounterId,
    Guid? PrescriptionId,
    List<CdssDrugInput> Items);

public record CdssOverrideRequest(
    Guid? PrescriptionId,
    Guid? EncounterId,
    string RuleType,
    string? RuleCode,
    string Severity,
    string OverrideReason,
    string? ReasonCode);

// ─── Admin CRUD rule ───────────────────────────────────────────────────────
public record CdssRuleResponse(
    Guid Id,
    int? TenantId,
    string Code,
    string RuleName,
    string RuleType,
    string? Category,
    string? DefinitionJson,
    string? MessageVi,
    string? ManagementVi,
    string Severity,
    bool IsInterruptive,
    int Priority,
    bool IsActive);

public record CdssRuleUpsertRequest(
    Guid? Id,
    string Code,
    string RuleName,
    string RuleType,
    string? Category,
    string? DefinitionJson,
    string? MessageVi,
    string? ManagementVi,
    string Severity,
    bool IsInterruptive,
    int Priority,
    bool IsActive);
