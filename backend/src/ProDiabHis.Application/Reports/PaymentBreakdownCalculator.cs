using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Reports;

/// <summary>
/// Tinh breakdown doanh thu theo phuong thuc thanh toan tu danh sach ban ghi diab_his_bil_payments.
/// Tach ra pure function de unit test doc lap khoi DB (theo dung pattern DiabetesCohortCalculator).
/// Logic loai VOID/REFUNDED khop voi CashierShiftServiceImpl.CalculateShiftSummaryAsync.
/// </summary>
public static class PaymentBreakdownCalculator
{
    private static readonly Dictionary<string, string> MethodLabels = new()
    {
        ["CASH"] = "Tiền mặt",
        ["BANK_TRANSFER"] = "Chuyển khoản",
        ["VISA"] = "Thẻ Visa",
        ["MASTER"] = "Thẻ Master",
        ["QR_VIETQR"] = "QR VietQR",
        ["QR_MOMO"] = "QR Momo",
        ["QR_VNPAY"] = "QR VNPay",
        ["OTHER"] = "Khác",
    };

    public static string LabelFor(string method) =>
        MethodLabels.TryGetValue(method, out var label) ? label : (string.IsNullOrWhiteSpace(method) ? "Khác" : method);

    /// <summary>Danh sach breakdown theo method, sap xep giam dan theo gia tri.</summary>
    public static IReadOnlyList<PaymentMethodBreakdownResponse> CalculateBreakdown(
        IEnumerable<(string Method, decimal Amount, string Status)> payments)
    {
        var accepted = payments
            .Where(p => p.Status != PaymentStatus.Void && p.Status != PaymentStatus.Refunded && p.Amount >= 0)
            .ToList();

        var totalRevenue = accepted.Sum(p => p.Amount);

        return accepted
            .GroupBy(p => p.Method)
            .Select(g =>
            {
                var value = g.Sum(x => x.Amount);
                var count = g.Count();
                var pct = totalRevenue > 0 ? Math.Round(value / totalRevenue * 100, 1) : 0m;
                return new PaymentMethodBreakdownResponse(LabelFor(g.Key), value, count, pct);
            })
            .OrderByDescending(x => x.Value)
            .ToList();
    }

    /// <summary>Tong doanh thu (khong tinh refund/void), so giao dich hop le, va tong tien hoan tra.</summary>
    public static (decimal TotalRevenue, int TotalTransactions, decimal TotalRefunds) Summarize(
        IEnumerable<(string Method, decimal Amount, string Status)> payments)
    {
        decimal revenue = 0, refunds = 0;
        var count = 0;

        foreach (var p in payments)
        {
            if (p.Status == PaymentStatus.Void) continue;
            if (p.Status == PaymentStatus.Refunded || p.Amount < 0)
            {
                refunds += Math.Abs(p.Amount);
                continue;
            }

            revenue += p.Amount;
            count++;
        }

        return (revenue, count, refunds);
    }
}
