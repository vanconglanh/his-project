using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.CLS;

/// <summary>
/// Kiem tra hop le cua dot chi dinh khi gan chi dinh CLS moi vao dot (G01).
/// Tach rieng thanh ham thuan (pure) de unit test khong can DB.
/// </summary>
public static class ClsRoundGuard
{
    /// <summary>
    /// Validate dot chi dinh cho thao tac "them chi dinh vao dot".
    /// </summary>
    /// <param name="roundExists">Dot co ton tai trong DUNG tenant hien tai hay khong</param>
    /// <param name="roundEncounterId">encounter_id cua dot (null neu khong ton tai)</param>
    /// <param name="orderEncounterId">encounter_id cua chi dinh sap tao</param>
    /// <param name="status">status cua dot: OPEN|SUBMITTED|IN_PROGRESS|COMPLETED|CANCELLED</param>
    /// <param name="paymentStatus">payment_status cua dot: UNPAID|PAID|WAIVED</param>
    public static Result ValidateForAddingOrder(
        bool roundExists,
        string? roundEncounterId,
        string orderEncounterId,
        string? status,
        string? paymentStatus)
    {
        if (!roundExists)
            return Result.Failure("CLS_ROUND_NOT_FOUND", "Không tìm thấy đợt chỉ định");

        if (!string.Equals(roundEncounterId, orderEncounterId, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("CLS_ROUND_ENCOUNTER_MISMATCH", "Đợt chỉ định không thuộc lượt khám này");

        // Da thu tien (PAID) hoac da mien phi (WAIVED) -> them dich vu nua = that thu
        if (ClsRoundPaymentStatus.AllowsExecution(paymentStatus ?? string.Empty)
            || status == ClsRoundStatus.Cancelled
            || status == ClsRoundStatus.Completed)
            return Result.Failure("CLS_ROUND_LOCKED", "Đợt chỉ định đã chốt — hãy tạo đợt mới");

        return Result.Success();
    }
}
