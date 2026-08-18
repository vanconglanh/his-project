namespace ProDiabHis.Application.Common;

/// <summary>Ma loi chuan cua G03 — khoa benh an / dinh chinh.</summary>
public static class EncounterLockErrors
{
    public const string EncounterLocked         = "ENCOUNTER_LOCKED";
    public const string EncounterNotFound       = "ENCOUNTER_NOT_FOUND";
    public const string AddendumNotApplicable   = "ADDENDUM_NOT_APPLICABLE";
    public const string AmendmentReasonRequired = "AMENDMENT_REASON_REQUIRED";
    public const string AddendumTargetNotFound  = "ADDENDUM_TARGET_NOT_FOUND";
    public const string AddendumInvalidSection  = "ADDENDUM_INVALID_SECTION";
    public const string BhytResubmitAckRequired = "BHYT_RESUBMIT_ACK_REQUIRED";
    public const string Forbidden               = "FORBIDDEN";

    public const string LockedMessage    = "Bệnh án đã khoá — chỉ xem";
    public const string ReasonMessage    = "Phải nhập lý do đính chính";
    public const string BhytWarnMessage  = "Hồ sơ đã gửi giám định — đính chính cần gửi lại XML";
    public const string ForbiddenMessage = "Bạn không có quyền đính chính bệnh án";

    /// <summary>Quyen bat buoc de tao ban dinh chinh.</summary>
    public const string AmendPermission     = "encounter.amend";
    public const string AmendReadPermission = "encounter.amend.read";
}

/// <summary>Cảnh báo (KHONG chan) khi benh an da nam trong ho so BHYT da gui giam dinh.</summary>
public record BhytWarningDto(
    bool Submitted,
    int? ExportId,
    string? PeriodMonth,
    string? ExportStatus,
    DateTime? SubmittedAt,
    string Message,
    // Heuristic = true: suy luan theo ky thang, khong tra nguoc duoc chinh xac tung luot kham.
    bool Heuristic = false);

/// <summary>Trang thai khoa cua mot luot kham.</summary>
public record EncounterLockInfo(
    Guid EncounterId,
    string Status,
    bool IsLocked,
    DateTime? LockedAt,
    Guid? LockedById,
    string? LockedByName,
    DateTime? FinishedAt,
    bool CanAmend,
    int AmendmentCount,
    BhytWarningDto? BhytWarning);

/// <summary>
/// Guard kiem tra benh an da khoa hay chua (G03). Moi command ghi len du lieu lam sang
/// cua mot luot kham deu phai di qua guard nay (tu dong qua EncounterLockBehavior).
/// </summary>
public interface IEncounterLockGuard
{
    /// <summary>Fail ENCOUNTER_LOCKED neu benh an dang o trang thai khoa (DONE/CANCELLED).</summary>
    Task<Result> EnsureEditableAsync(Guid encounterId, CancellationToken ct);

    /// <summary>Lay trang thai khoa + canh bao BHYT.</summary>
    Task<Result<EncounterLockInfo>> GetLockStateAsync(Guid encounterId, CancellationToken ct);

    /// <summary>Tra ve encounter_id cua mot ban ghi con (sinh hieu / don thuoc / chi dinh CLS).</summary>
    Task<Guid?> ResolveEncounterIdAsync(string childKind, Guid childId, CancellationToken ct);
}

/// <summary>Marker: command thao tac truc tiep tren mot luot kham -> tu dong bi guard chan khi khoa.</summary>
public interface IEncounterScopedCommand
{
    Guid EncounterId { get; }
}

/// <summary>Cac loai ban ghi con co the tra nguoc ve encounter.</summary>
public static class EncounterChildKind
{
    public const string VitalSigns   = "VITAL_SIGNS";
    public const string Prescription = "PRESCRIPTION";
    public const string LabOrder     = "LAB_ORDER";
    public const string RadOrder     = "RAD_ORDER";
    public const string Diagnosis    = "DIAGNOSIS";
}

/// <summary>
/// Marker: command thao tac tren BAN GHI CON theo id cua chinh no (khong co EncounterId trong payload).
/// Guard se tra nguoc encounter_id roi moi kiem tra khoa.
/// </summary>
public interface IEncounterChildScopedCommand
{
    Guid ChildId { get; }
    string ChildKind { get; }
}

/// <summary>Marker: command duoc phep chay ngay ca khi benh an da khoa (vd tao ban dinh chinh).</summary>
public interface IBypassEncounterLock
{
}
