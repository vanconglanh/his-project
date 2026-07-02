namespace ProDiabHis.Application.Pharmacy.Drugs;

public record DrugMasterRequest(
    string Code,
    string NameVi,
    string? NameEn,
    string? GenericName,
    string? AtcCode,
    string? Strength,
    string Unit,
    string Form,
    string? Manufacturer,
    string? Country,
    decimal? Price,
    int? CategoryId,
    bool RequiresPrescription,
    bool IsPsychotropic,
    bool IsNarcotic,
    string? DtqgDrugCode,
    string Status);

public record DrugMasterResponse(
    string Id,
    int TenantId,
    string Code,
    string NameVi,
    string? NameEn,
    string? GenericName,
    string? AtcCode,
    string? Strength,
    string Unit,
    string? Form,
    string? Manufacturer,
    string? Country,
    decimal? Price,
    int? CategoryId,
    bool RequiresPrescription,
    bool IsPsychotropic,
    bool IsNarcotic,
    string? DtqgDrugCode,
    string Status,
    int InteractionsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DrugCategory(string Id, string Code, string Name, string? ParentId);

public record DrugCategoryCreateRequest(string Code, string Name, string? ParentId);

public record DdiRule(
    string Id,
    string Drug1Id,
    string Drug1Name,
    string Drug2Id,
    string Drug2Name,
    string Severity,
    string Description,
    string EvidenceLevel);

public record DrugImportSummary(int TotalRows, int Inserted, int Updated, int Failed, IReadOnlyList<DrugImportRowError> Errors);
public record DrugImportRowError(int Row, string Message);

public record SyncCucQldRequest(string Mode, DateTime? Since);
public record SyncJobResponse(Guid JobId);
