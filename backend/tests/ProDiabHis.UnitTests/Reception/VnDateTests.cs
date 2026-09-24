using FluentAssertions;
using ProDiabHis.Application.Reception;
using Xunit;

namespace ProDiabHis.UnitTests.Reception;

/// <summary>
/// BM-07 (tiep, Lark - "van lech gio giua Dang ky va Kham benh"): server chay UTC, truoc day
/// module Reception dung DateTime.Today (= ngay UTC) de xac dinh "hom nay" trong khi module
/// Encounters da quy doi sang gio VN -> lech nhau quanh khung 17h-24h UTC (0h-7h sang hom sau
/// theo VN). Test mo phong dung khung gio bi anh huong.
/// </summary>
public class VnDateTests
{
    [Fact]
    public void FromUtc_TruocNuaDemUtc_ChuaSangNgayMoiTheoVn()
    {
        // 2026-09-23 16:59 UTC = 2026-09-23 23:59 VN -> van la ngay 23 theo VN.
        var utc = new DateTime(2026, 9, 23, 16, 59, 0, DateTimeKind.Utc);
        VnDate.FromUtc(utc).Should().Be(new DateOnly(2026, 9, 23));
    }

    [Fact]
    public void FromUtc_SauNuaDemUtc_DaSangNgayMoiTheoVn()
    {
        // BM-07: benh nhan check-in 2026-09-23 17:30 UTC = 2026-09-24 00:30 VN.
        // Truoc fix (DateTime.Today = ngay UTC): van tinh la "23/09". Sau fix: dung "24/09"
        // theo VN, khop voi cach ListEncountersQueryHandler dang tinh "hom nay".
        var utc = new DateTime(2026, 9, 23, 17, 30, 0, DateTimeKind.Utc);
        VnDate.FromUtc(utc).Should().Be(new DateOnly(2026, 9, 24));
    }

    [Fact]
    public void FromUtc_GiuaTruaUtc_KhongAnhHuongGioHanhChinh()
    {
        // Giua gio hanh chinh (VN va UTC cung ngay lich) khong bi anh huong boi fix.
        var utc = new DateTime(2026, 9, 23, 6, 0, 0, DateTimeKind.Utc); // 13:00 VN
        VnDate.FromUtc(utc).Should().Be(new DateOnly(2026, 9, 23));
    }
}
