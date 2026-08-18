using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.CLS;

/// <summary>Loai chi dinh CLS dung cho gate thanh toan</summary>
public static class ClsOrderKind
{
    public const string Lab = "LAB";
    public const string Rad = "RAD";
}

/// <summary>
/// Gate thanh toan CLS (G02): chan thuc hien / nhap ket qua khi dot chi dinh con UNPAID.
/// Quy tac:
///   1. Order khong thuoc dot nao (round_id NULL) -> BO QUA gate (don legacy).
///   2. Dot PAID / WAIVED -> cho phep.
///   3. Dot UNPAID + tenant.cho_phep_no_vien_phi = 0 -> loi CLS_ORDER_UNPAID.
///   4. Dot UNPAID + tenant.cho_phep_no_vien_phi = 1 -> cho phep NHUNG bat buoc ghi audit log.
/// </summary>
public interface IClsPaymentGate
{
    /// <param name="orderId">Id lab order hoac rad order</param>
    /// <param name="orderKind">ClsOrderKind.Lab | ClsOrderKind.Rad</param>
    Task<Result<bool>> EnsureRoundPayableAsync(Guid orderId, string orderKind, CancellationToken ct = default);
}
