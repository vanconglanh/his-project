using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Security;
using Xunit;

namespace ProDiabHis.UnitTests.Security;

/// <summary>
/// Hang muc 6 — ma hoa PII AES-256-GCM + blind index HMAC-SHA256.
/// Khoa dung trong test duoc sinh ngau nhien tai runtime (KHONG hardcode secret).
/// </summary>
public class PiiProtectorTests
{
    private static PiiProtector CreateProtector(bool withBlindIndexKey = true)
    {
        var masterKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var bidxKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var settings = new Dictionary<string, string?>
        {
            ["Encryption:MasterKey"] = masterKey
        };
        if (withBlindIndexKey) settings["Encryption:BlindIndexKey"] = bidxKey;

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new PiiProtector(new AesGcmEncryptor(config), config);
    }

    // ──────────────────────────────────────────
    // Encrypt / Decrypt round-trip
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("0912345678")]
    [InlineData("Số 12, ngõ 34, phường Láng Hạ, Hà Nội")]
    [InlineData("Bệnh nhân dị ứng Penicillin — theo dõi sát")]
    public void Protect_Unprotect_RoundTrip_TraVeDungPlaintext(string plaintext)
    {
        var sut = CreateProtector();

        var protectedValue = sut.Protect(plaintext);

        protectedValue.Should().NotBe(plaintext);
        protectedValue.Should().StartWith(PiiProtector.Marker);
        sut.Unprotect(protectedValue).Should().Be(plaintext);
    }

    [Fact]
    public void Protect_CungPlaintext_ChoRaCiphertextKhacNhau_NonceNgauNhien()
    {
        var sut = CreateProtector();

        var a = sut.Protect("0912345678");
        var b = sut.Protect("0912345678");

        // Day chinh la ly do KHONG the LIKE/= tren ciphertext -> phai co blind index
        a.Should().NotBe(b);
        sut.Unprotect(a).Should().Be(sut.Unprotect(b));
    }

    [Fact]
    public void Unprotect_DuLieuChuaMaHoa_TraVeNguyenVen()
    {
        var sut = CreateProtector();

        // Du lieu cu chua backfill -> khong duoc lam hong
        sut.Unprotect("0912345678").Should().Be("0912345678");
        sut.Unprotect(null).Should().BeNull();
        sut.Unprotect("").Should().Be("");
    }

    // ──────────────────────────────────────────
    // Idempotent — nhan biet ban ghi da ma hoa
    // ──────────────────────────────────────────

    [Fact]
    public void Protect_GoiNhieuLan_KhongMaHoaChong_Idempotent()
    {
        var sut = CreateProtector();

        var once = sut.Protect("0912345678");
        var twice = sut.Protect(once);
        var thrice = sut.Protect(twice);

        twice.Should().Be(once);
        thrice.Should().Be(once);
        sut.Unprotect(thrice).Should().Be("0912345678");
    }

    [Fact]
    public void IsProtected_PhanBietDuocBanGhiDaMaHoa()
    {
        var sut = CreateProtector();

        sut.IsProtected(sut.Protect("0912345678")).Should().BeTrue();
        sut.IsProtected("0912345678").Should().BeFalse();
        sut.IsProtected(null).Should().BeFalse();
        sut.IsProtected("").Should().BeFalse();
    }

    // ──────────────────────────────────────────
    // Blind index — on dinh voi input CHUA chuan hoa
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("0912345678")]
    [InlineData(" 0912345678 ")]
    [InlineData("0912 345 678")]
    [InlineData("0912-345-678")]
    [InlineData("0912.345.678")]
    [InlineData("+84912345678")]
    [InlineData("84912345678")]
    [InlineData("0084912345678")]
    public void BlindIndex_SoDienThoai_OnDinhVoiMoiDinhDangNhapLieu(string input)
    {
        var sut = CreateProtector();
        var expected = sut.BlindIndex("0912345678", PiiField.Phone);

        sut.BlindIndex(input, PiiField.Phone).Should().Be(expected);
    }

    [Theory]
    [InlineData("HC4 010 001234")]
    [InlineData("hc4010001234")]
    [InlineData("  HC4-010-001234  ")]
    public void BlindIndex_SoTheBhyt_OnDinhVoiKhoangTrangGachNoiChuThuong(string input)
    {
        var sut = CreateProtector();
        var expected = sut.BlindIndex("HC4010001234", PiiField.InsuranceCardNo);

        sut.BlindIndex(input, PiiField.InsuranceCardNo).Should().Be(expected);
    }

    [Fact]
    public void BlindIndex_CmndCoKhoangTrang_OnDinh()
    {
        var sut = CreateProtector();

        sut.BlindIndex(" 001 234 567 890 ", PiiField.IdNumber)
           .Should().Be(sut.BlindIndex("001234567890", PiiField.IdNumber));
    }

    [Fact]
    public void BlindIndex_LaHexSha256_64KyTu()
    {
        var sut = CreateProtector();

        var bidx = sut.BlindIndex("0912345678", PiiField.Phone);

        bidx.Should().NotBeNull();
        bidx!.Length.Should().Be(64);
        bidx.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void BlindIndex_CungGiaTriKhacLoaiTruong_ChoHashKhacNhau_DomainSeparation()
    {
        var sut = CreateProtector();

        var asPhone = sut.BlindIndex("123456789012", PiiField.Phone);
        var asId = sut.BlindIndex("123456789012", PiiField.IdNumber);

        asPhone.Should().NotBe(asId);
    }

    [Fact]
    public void BlindIndex_GiaTriRong_TraVeNull()
    {
        var sut = CreateProtector();

        sut.BlindIndex(null, PiiField.Phone).Should().BeNull();
        sut.BlindIndex("   ", PiiField.Phone).Should().BeNull();
        sut.BlindIndex("abc", PiiField.Phone).Should().BeNull(); // khong co chu so
    }

    [Fact]
    public void BlindIndex_ThieuKhoa_TraVeNull_VaKhongLamHongMaHoa()
    {
        var sut = CreateProtector(withBlindIndexKey: false);

        sut.BlindIndexEnabled.Should().BeFalse();
        sut.BlindIndex("0912345678", PiiField.Phone).Should().BeNull();
        sut.Unprotect(sut.Protect("0912345678")).Should().Be("0912345678");
    }

    [Fact]
    public void BlindIndex_KhoaKhacNhau_ChoHashKhacNhau()
    {
        var a = CreateProtector();
        var b = CreateProtector();

        a.BlindIndex("0912345678", PiiField.Phone)
         .Should().NotBe(b.BlindIndex("0912345678", PiiField.Phone));
    }

    // ──────────────────────────────────────────
    // Chuan hoa so dien thoai
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("+84 912 345 678", "0912345678")]
    [InlineData("0912345678", "0912345678")]
    [InlineData("912345678", "0912345678")]
    [InlineData("0084912345678", "0912345678")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void NormalizePhone_ChuanHoaDungDangQuocGia(string? input, string? expected)
    {
        PiiNormalizer.NormalizePhone(input).Should().Be(expected);
    }
}
