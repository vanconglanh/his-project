using FluentAssertions;
using ProDiabHis.Application.CLS;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.CLS;

/// <summary>
/// Unit test validate dot chi dinh khi them chi dinh CLS moi (G01).
/// Khong can DB - ClsRoundGuard la ham thuan.
/// </summary>
public class ClsRoundGuardTests
{
    private const string Enc = "11111111-1111-1111-1111-111111111111";
    private const string OtherEnc = "22222222-2222-2222-2222-222222222222";

    [Fact]
    public void RoundKhongTonTai_TraVe_CLS_ROUND_NOT_FOUND()
    {
        var r = ClsRoundGuard.ValidateForAddingOrder(false, null, Enc, null, null);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("CLS_ROUND_NOT_FOUND");
        r.ErrorMessage.Should().Be("Không tìm thấy đợt chỉ định");
    }

    [Fact]
    public void RoundKhacLuotKham_TraVe_CLS_ROUND_ENCOUNTER_MISMATCH()
    {
        var r = ClsRoundGuard.ValidateForAddingOrder(true, OtherEnc, Enc,
            ClsRoundStatus.Open, ClsRoundPaymentStatus.Unpaid);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("CLS_ROUND_ENCOUNTER_MISMATCH");
        r.ErrorMessage.Should().Be("Đợt chỉ định không thuộc lượt khám này");
    }

    [Theory]
    [InlineData("OPEN", "PAID")]        // da thu tien
    [InlineData("OPEN", "WAIVED")]      // da mien phi
    [InlineData("CANCELLED", "UNPAID")] // dot da huy
    [InlineData("COMPLETED", "UNPAID")] // dot da hoan tat
    public void RoundDaChot_TraVe_CLS_ROUND_LOCKED(string status, string paymentStatus)
    {
        var r = ClsRoundGuard.ValidateForAddingOrder(true, Enc, Enc, status, paymentStatus);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("CLS_ROUND_LOCKED");
        r.ErrorMessage.Should().Be("Đợt chỉ định đã chốt — hãy tạo đợt mới");
    }

    [Theory]
    [InlineData("OPEN")]
    [InlineData("SUBMITTED")]
    [InlineData("IN_PROGRESS")]
    public void RoundConMo_ChuaThanhToan_ChoPhepThem(string status)
    {
        var r = ClsRoundGuard.ValidateForAddingOrder(true, Enc, Enc, status, ClsRoundPaymentStatus.Unpaid);

        r.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SoSanhEncounterId_KhongPhanBietHoaThuong()
    {
        var r = ClsRoundGuard.ValidateForAddingOrder(true, Enc.ToUpperInvariant(), Enc,
            ClsRoundStatus.Open, ClsRoundPaymentStatus.Unpaid);

        r.IsSuccess.Should().BeTrue();
    }
}
