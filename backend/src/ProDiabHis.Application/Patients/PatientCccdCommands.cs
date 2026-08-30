using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

// ── Check trung theo CCCD (luong quet QR) — phan biet 3 case theo PRD quet-qr-cccd ──
public static class CccdDuplicateCase
{
    /// <summary>Case 1 — chua ton tai</summary>
    public const string None = "NONE";
    /// <summary>Case 2 — ton tai, khop hoan toan</summary>
    public const string ExactMatch = "EXACT_MATCH";
    /// <summary>Case 3 — ton tai nhung co truong lech</summary>
    public const string FieldMismatch = "FIELD_MISMATCH";
}

/// <summary>Ten cac field duoc phep so sanh/cap nhat tu luong quet CCCD</summary>
public static class CccdComparableField
{
    public const string FullName = "full_name";
    public const string Gender = "gender";
    public const string DateOfBirth = "date_of_birth";
    public const string Address = "address";
}

public record CccdFieldDiff(string Field, string? OldValue, string? NewValue);

public record CccdDuplicateCheckResult(
    string Case,
    Guid? PatientId,
    string? PatientCode,
    string? PatientFullName,
    DateOnly? PatientDateOfBirth,
    List<CccdFieldDiff> FieldDiffs);

public record CheckCccdDuplicateQuery(
    string IdNumber,
    string? FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address) : IRequest<Result<CccdDuplicateCheckResult>>;

// ── Cap nhat chon loc tung truong sau khi lien tan xac nhan tren dialog so sanh (Case 3) ──
public record CccdFieldUpdateItem(string Field, string NewValue);

public record ApplyCccdFieldUpdatesCommand(Guid PatientId, List<CccdFieldUpdateItem> Fields)
    : IRequest<Result<PatientResponse>>;
