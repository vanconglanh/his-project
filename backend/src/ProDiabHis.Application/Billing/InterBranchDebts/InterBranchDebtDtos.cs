namespace ProDiabHis.Application.Billing.InterBranchDebts;

/// <summary>Nguon phat sinh cong no noi bo (BR-85 tra no cheo / BR-87 dieu chuyen kho).</summary>
public static class InterBranchDebtSourceType
{
    public const string CrossBranchPayment = "CROSS_BRANCH_PAYMENT";
    public const string StockTransfer = "STOCK_TRANSFER";
}

public static class InterBranchDebtStatus
{
    public const string Open = "OPEN";
    public const string Settled = "SETTLED";
}

public record InterBranchDebtResponse(
    Guid Id,
    int TenantId,
    int DebtorBranchId,
    string? DebtorBranchName,
    int CreditorBranchId,
    string? CreditorBranchName,
    decimal Amount,
    string SourceType,
    Guid? SourceRefId,
    string? SourceRefCode,
    string Status,
    string? Note,
    DateTime? SettledAt,
    DateTime CreatedAt);

public record SettleInterBranchDebtRequest(string? Note);

/// <summary>
/// BR-85: khi thu ngan tao Payment cho hoa don thuoc chi nhanh KHAC chi nhanh dang thu tien,
/// tinh xem co phat sinh but toan cong no noi bo hay khong va debtor/creditor la chi nhanh nao.
/// Tach thanh ham thuan (pure function) de unit test khong can DB that (xem InterBranchDebtLogicTests).
///   - Cung chi nhanh (billingBranchId == currentBranchId) -> KHONG sinh (tra ve null).
///   - billingBranchId = null (hoa don chua gan chi nhanh, du lieu cu) -> KHONG sinh, coi la an toan.
///   - Khac chi nhanh -> debtor = currentBranchId (noi thu tien, dang giu ho tien cua billing branch
///     nen "no" billing branch), creditor = billingBranchId (chi nhanh phat sinh hoa don, duoc no).
/// </summary>
public static class InterBranchDebtCalculator
{
    public static (int DebtorBranchId, int CreditorBranchId)? ComputeForCrossBranchPayment(
        int? billingBranchId, int currentBranchId)
    {
        if (!billingBranchId.HasValue) return null;
        if (billingBranchId.Value == currentBranchId) return null;
        return (currentBranchId, billingBranchId.Value);
    }
}
