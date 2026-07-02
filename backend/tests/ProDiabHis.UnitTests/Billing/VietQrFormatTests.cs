using FluentAssertions;
using ProDiabHis.Infrastructure.Billing;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>Tests VietQR EMV QR string format compliance</summary>
public class VietQrFormatTests
{
    [Fact]
    public void BuildVietQrString_StartsWithPayloadFormatIndicator()
    {
        var qr = VietQrGateway.BuildVietQrString(100_000, "TT HOA DON PD001");
        qr.Should().StartWith("000201"); // ID=00, LEN=02, Value=01
    }

    [Fact]
    public void BuildVietQrString_ContainsDynamicQrIndicator()
    {
        var qr = VietQrGateway.BuildVietQrString(100_000, "TT HOA DON PD001");
        qr.Should().Contain("010212"); // Dynamic QR
    }

    [Fact]
    public void BuildVietQrString_ContainsAmount()
    {
        var amount = 250_000m;
        var qr = VietQrGateway.BuildVietQrString(amount, "TEST");
        qr.Should().Contain("250000"); // Amount in VND
    }

    [Fact]
    public void BuildVietQrString_ContainsCurrencyVND()
    {
        var qr = VietQrGateway.BuildVietQrString(100_000, "TEST");
        qr.Should().Contain("5303704"); // Currency = 704 (VND)
    }

    [Fact]
    public void BuildVietQrString_ContainsCountryVN()
    {
        var qr = VietQrGateway.BuildVietQrString(100_000, "TEST");
        qr.Should().Contain("5802VN"); // Country VN
    }

    [Fact]
    public void BuildVietQrString_EndsWithCRC4Chars()
    {
        var qr = VietQrGateway.BuildVietQrString(100_000, "TEST");
        // CRC is last 4 chars after "6304"
        var crcIdx = qr.LastIndexOf("6304");
        crcIdx.Should().BeGreaterThan(0);
        var crcValue = qr[(crcIdx + 4)..];
        crcValue.Should().HaveLength(4);
        // Should be valid hex
        Convert.FromHexString(crcValue).Should().HaveCount(2);
    }

    [Fact]
    public void BuildVietQrString_DifferentAmounts_ProduceDifferentStrings()
    {
        var qr1 = VietQrGateway.BuildVietQrString(100_000, "REF001");
        var qr2 = VietQrGateway.BuildVietQrString(200_000, "REF001");
        qr1.Should().NotBe(qr2);
    }

    [Fact]
    public void BuildVietQrString_DifferentRefs_ProduceDifferentStrings()
    {
        var qr1 = VietQrGateway.BuildVietQrString(100_000, "REF001");
        var qr2 = VietQrGateway.BuildVietQrString(100_000, "REF002");
        qr1.Should().NotBe(qr2);
    }

    [Fact]
    public async Task VietQrGateway_GenerateQr_ReturnsBase64Png()
    {
        var gateway = new VietQrGateway();
        var result = await gateway.GenerateQrAsync(new Application.Billing.QrGenerateRequest(
            Guid.NewGuid(), "VIETQR", 100_000, "PD20240101001", 900));

        result.QrPayloadBase64.Should().NotBeNullOrEmpty();

        // Valid base64
        var bytes = Convert.FromBase64String(result.QrPayloadBase64);
        bytes.Should().NotBeEmpty();

        // PNG magic bytes: 0x89 0x50 0x4E 0x47
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be(0x50); // P
        bytes[2].Should().Be(0x4E); // N
        bytes[3].Should().Be(0x47); // G
    }
}
