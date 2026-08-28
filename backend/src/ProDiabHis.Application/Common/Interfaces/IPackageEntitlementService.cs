namespace ProDiabHis.Application.Common.Interfaces;

/// <summary>
/// FR-1204 (D7) - abstraction dung de tru/hoan/tra ve dinh muc goi tra truoc.
/// Dat o Common/Interfaces de Appointment / LabRad / Prescription phu thuoc vao
/// ABSTRACTION nay, khong phu thuoc nguoc vao module Packages (chong circular dependency).
/// Trien khai: ProDiabHis.Infrastructure/Services/PackageEntitlementService.cs
/// (can IDbConnection truc tiep de SELECT ... FOR UPDATE - xem doc muc 6).
/// </summary>
public interface IPackageEntitlementService
{
    /// <summary>Chi kiem tra dinh muc con lai, KHONG tru - dung de preview gia truoc khi luu (vd form ke don).</summary>
    Task<PackageCoverageQuote> QuoteAsync(PackageCoverageRequest request, CancellationToken ct);

    /// <summary>
    /// Tru dinh muc that su. Idempotent theo (tenant_id, idempotency_key, action=DEDUCT) -
    /// goi lai voi cung idempotency key se KHONG tru lan 2, tra lai ket qua cu.
    /// </summary>
    Task<PackageCoverageQuote> ConsumeAsync(PackageCoverageRequest request, CancellationToken ct);

    /// <summary>Hoan dinh muc da tru cho 1 source (huy chi dinh CLS/don thuoc/check-in...).</summary>
    Task<PackageReverseResult> ReverseAsync(string sourceType, Guid sourceId, string reason, Guid? performedBy, CancellationToken ct);

    /// <summary>FR-1205 - tong hop cac goi cua 1 benh nhan (hien thi "con X/Y") + canh bao cong no/sap het han.</summary>
    Task<PackagePatientSummary> GetPatientSummaryAsync(Guid patientId, CancellationToken ct);
}

public enum PackageItemType
{
    VISIT,
    SERVICE,
    DRUG
}

public record PackageCoverageLineRequest(
    PackageItemType ItemType,
    Guid ItemRefId,
    decimal RequestedQuantity,
    /// <summary>Duy nhat theo dong item trong nguon (vd prescription_item.id). Dung de sinh idempotency key.</summary>
    Guid? SourceItemId = null);

public record PackageCoverageRequest(
    Guid PatientId,
    string SourceType,     // APPOINTMENT|ENCOUNTER|LAB_ORDER|RAD_ORDER|PRESCRIPTION
    Guid SourceId,
    IReadOnlyList<PackageCoverageLineRequest> Lines,
    Guid? PerformedBy = null,
    int? BranchId = null);

public record PackageCoverageLineResult(
    PackageItemType ItemType,
    Guid ItemRefId,
    decimal RequestedQuantity,
    decimal CoveredQuantity,
    decimal ExcessQuantity,
    decimal CoveredAmount,
    Guid? SubscriptionId,
    Guid? BalanceId,
    Guid? UsageLogId);

public record PackageCoverageQuote(
    IReadOnlyList<PackageCoverageLineResult> Lines,
    IReadOnlyList<string> Warnings);

public record PackageReverseResult(int ReversedCount, IReadOnlyList<string> Warnings);

public record PackageBalanceSummary(
    string ItemType, string ItemCode, string ItemName, string Unit,
    decimal TotalQuantity, decimal UsedQuantity, decimal RemainingQuantity,
    string Display, bool IsLow);

public record PackageSubscriptionSummary(
    Guid Id, string SubscriptionNo, string PackageName,
    string Status, string PaymentStatus,
    DateOnly ExpiryDate, int DaysToExpiry, decimal AmountDue,
    IReadOnlyList<PackageBalanceSummary> Balances);

public record PackagePatientSummary(
    decimal TotalOutstandingDebt,
    bool HasExpiringSoon,
    IReadOnlyList<PackageSubscriptionSummary> Subscriptions);

/// <summary>Nem khi L2 (UPDATE co dieu kien version) phat hien xung dot dong thoi - HTTP 409 PACKAGE_BALANCE_CONFLICT.</summary>
public class PackageBalanceConflictException : Exception
{
    public string BalanceId { get; }
    public PackageBalanceConflictException(string balanceId)
        : base($"PACKAGE_BALANCE_CONFLICT: balance {balanceId} bi thay doi dong thoi, can retry")
    {
        BalanceId = balanceId;
    }
}

/// <summary>
/// Nem khi ReverseAsync bi tu choi vi nguon goc usage log da gan voi 1 hoa don da PAID
/// hoac 1 phieu phat thuoc da hoan tat (DISPENSED) - Q6. Phai xu ly hoan tien/hoan thuoc
/// qua quy trinh rieng (refund/return), khong tu dong hoan dinh muc.
/// </summary>
public class PackageReverseNotAllowedException : Exception
{
    public string Reason { get; }
    public PackageReverseNotAllowedException(string reason)
        : base($"PACKAGE_REVERSE_NOT_ALLOWED: {reason}")
    {
        Reason = reason;
    }
}
