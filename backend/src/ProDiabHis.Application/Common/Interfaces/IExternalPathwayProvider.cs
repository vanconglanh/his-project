namespace ProDiabHis.Application.Common.Interfaces;

/// <summary>
/// §4.7.3 — Cong tich hop lay lo trinh/goi dich vu tu he thong ngoai (diaB) theo dinh danh benh nhan.
/// HIS KHONG tu tinh lo trinh; chi hien thi nguyen van du lieu he ngoai tra ve.
/// Trien khai hien tai: NullExternalPathwayProvider (luon tra NotConfigured) - vi diaB CHUA co endpoint.
/// Khi diaB co API that -> them DiabPathwayProvider, khong doi tang Application/UI.
/// </summary>
public interface IExternalPathwayProvider
{
    /// <summary>Lay lo trinh/goi tu he thong ngoai theo dinh danh benh nhan. KHONG BAO GIO nem loi lam chan luong kham.</summary>
    Task<ExternalPathwayResult> GetPathwayAsync(ExternalPathwayQuery query, CancellationToken ct);
}

/// <summary>Tham so tra cuu — theo tenant + dinh danh benh nhan (SDT / CCCD / account id ben ngoai).</summary>
public record ExternalPathwayQuery(int TenantId, string? Phone, string? CitizenId, string? ExternalAccountId);

/// <summary>Trang thai ket qua tra cuu lo trinh.</summary>
public enum ExternalPathwayStatus
{
    /// <summary>Lay duoc du lieu lo trinh.</summary>
    Ok,
    /// <summary>Khong tim thay benh nhan / benh nhan khong co goi ben he ngoai.</summary>
    NotFound,
    /// <summary>He ngoai loi/timeout/circuit-breaker mo — thu lai sau.</summary>
    Unavailable,
    /// <summary>Tenant chua cau hinh tich hop (mac dinh cua NullExternalPathwayProvider).</summary>
    NotConfigured
}

/// <summary>Mot moc (tuan) trong lo trinh + trang thai hoan thanh.</summary>
public record ExternalPathwayMilestone(int Week, string State);

/// <summary>§4.7.3 — Ket qua tra cuu lo trinh he ngoai.</summary>
public record ExternalPathwayResult(
    ExternalPathwayStatus Status,
    string? PackageName,
    string? DisplayLabel,              // "Tuan 6/24" — lay nguyen van tu diaB, HIS khong tu tinh
    int? CurrentWeek,
    int? TotalWeeks,
    DateTime? ActivationDate,
    DateTime? ExpirationDate,
    IReadOnlyList<ExternalPathwayMilestone> Milestones,
    DateTime FetchedAt,
    string? ErrorMessage)
{
    /// <summary>Ket qua chua cau hinh tich hop — dung boi NullExternalPathwayProvider.</summary>
    public static ExternalPathwayResult NotConfiguredResult() => new(
        ExternalPathwayStatus.NotConfigured,
        PackageName: null,
        DisplayLabel: null,
        CurrentWeek: null,
        TotalWeeks: null,
        ActivationDate: null,
        ExpirationDate: null,
        Milestones: Array.Empty<ExternalPathwayMilestone>(),
        FetchedAt: DateTime.UtcNow,
        ErrorMessage: null);
}
