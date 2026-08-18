using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>
/// Dot chi dinh CLS - 1 lan bac si chi dinh 1 nhom dich vu CLS trong 1 luot kham.
/// Day la don vi THU TIEN va don vi GATE thuc hien. Map bang diab_his_cls_order_rounds.
/// </summary>
public class ClsOrderRound : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    /// <summary>So thu tu dot trong luot kham, bat dau tu 1</summary>
    public int RoundNo { get; set; }
    public string Status { get; set; } = ClsRoundStatus.Open;
    public string PaymentStatus { get; set; } = ClsRoundPaymentStatus.Unpaid;
    public decimal TotalAmount { get; set; }
    public string? BillingId { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidBy { get; set; }
    public string? WaivedReason { get; set; }
    public string? CancelReason { get; set; }
    public string? Note { get; set; }
}

public static class ClsRoundStatus
{
    public const string Open       = "OPEN";
    public const string Submitted  = "SUBMITTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed  = "COMPLETED";
    public const string Cancelled  = "CANCELLED";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Open]       = new[] { Submitted, Cancelled },
        [Submitted]  = new[] { InProgress, Cancelled },
        [InProgress] = new[] { Completed, Cancelled },
        [Completed]  = Array.Empty<string>(),
        [Cancelled]  = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

public static class ClsRoundPaymentStatus
{
    public const string Unpaid = "UNPAID";
    public const string Paid   = "PAID";
    public const string Waived = "WAIVED";

    private static readonly Dictionary<string, IReadOnlyList<string>> ValidTransitions = new()
    {
        [Unpaid] = new[] { Paid, Waived },
        [Waived] = new[] { Paid },
        [Paid]   = Array.Empty<string>()
    };

    public static bool CanTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Dot da du dieu kien cho phep thuc hien CLS (PAID hoac WAIVED)</summary>
    public static bool AllowsExecution(string paymentStatus)
        => paymentStatus == Paid || paymentStatus == Waived;
}
