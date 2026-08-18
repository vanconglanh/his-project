using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Ban dinh chinh (addendum) cua benh an DA KHOA — maps bang diab_his_cli_encounter_addenda.
/// Bat bien: chi INSERT + SELECT, KHONG ghi de ban goc (Luat KCB 2023 / TT 32-2023).
/// </summary>
public class EncounterAddendum : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public string Section { get; set; } = AddendumSection.Other;
    public string? TargetTable { get; set; }
    public string? TargetId { get; set; }
    public string Operation { get; set; } = AddendumOperation.Update;
    public string? ContentBefore { get; set; }
    public string? ContentAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool BhytSubmittedFlag { get; set; }
    public int? BhytExportId { get; set; }
    public DateTime? BhytResubmitAt { get; set; }
    public string? AuditLogId { get; set; }
}

public static class AddendumSection
{
    public const string Diagnosis    = "DIAGNOSIS";
    public const string ClinicalNote = "CLINICAL_NOTE";
    public const string Prescription = "PRESCRIPTION";
    public const string VitalSign    = "VITAL_SIGN";
    public const string ClsOrder     = "CLS_ORDER";
    public const string Other        = "OTHER";

    public static readonly IReadOnlyList<string> All =
        new[] { Diagnosis, ClinicalNote, Prescription, VitalSign, ClsOrder, Other };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

public static class AddendumOperation
{
    public const string Update = "UPDATE";
    public const string Add    = "ADD";
    public const string Remove = "REMOVE";

    public static readonly IReadOnlyList<string> All = new[] { Update, Add, Remove };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
