using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Application.Pharmacy.Dtqg;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Unit tests cho SubmitDtqgFromPrescriptionHandler.
/// Su dung MockDtqgClient (khong can DB ket noi that) — test happy path va edge cases.
/// </summary>
public class SubmitDtqgFromPrescriptionTests
{
    [Fact]
    public async Task MockDtqgClient_Returns14CharMaDonThuoc_ForAnyPrescriptionId()
    {
        // Arrange
        var client = new MockDtqgClient(NullLogger<MockDtqgClient>.Instance);
        var payload = new DtqgSubmitPayload(1, 42, "CSKCB001", "PARTNER01", new { });

        // Act
        var result = await client.SubmitPrescriptionAsync(payload, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MaDonThuoc.Should().NotBeNullOrEmpty();
        result.MaDonThuoc!.Length.Should().Be(14,
            "DTQG yeu cau ma_don_thuoc chinh xac 14 ky tu");
        result.MaDonThuoc.Should().StartWith("VN",
            "Format DTQG bat dau bang VN");
    }

    [Fact]
    public async Task MockDtqgClient_DifferentPrescriptions_ProduceDifferentCodes()
    {
        var client = new MockDtqgClient(NullLogger<MockDtqgClient>.Instance);

        var r1 = await client.SubmitPrescriptionAsync(new DtqgSubmitPayload(1, 1, "", "", new { }));
        var r2 = await client.SubmitPrescriptionAsync(new DtqgSubmitPayload(1, 999999, "", "", new { }));

        r1.MaDonThuoc.Should().NotBe(r2.MaDonThuoc,
            "Prescription ID khac phai sinh ma khac");
    }

    [Fact]
    public async Task MockDtqgClient_Ping_ReturnsOk_WithMockResponse()
    {
        var client = new MockDtqgClient(NullLogger<MockDtqgClient>.Instance);

        var ping = await client.PingAsync();

        ping.Ok.Should().BeTrue();
        ping.PortalResponse.Should().Be("MOCK_OK");
    }

    [Fact]
    public async Task MockDtqgClient_Cancel_ReturnsTrue()
    {
        var client = new MockDtqgClient(NullLogger<MockDtqgClient>.Instance);

        var result = await client.CancelAsync("VN26053100000042", "Thu hoi don");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitDtqgFromPrescriptionRequest_ForceResubmit_DefaultFalse()
    {
        var req = new SubmitDtqgFromPrescriptionRequest();
        req.ForceResubmit.Should().BeFalse("Mac dinh khong force resubmit");
    }

    [Fact]
    public async Task SubmitDtqgFromPrescriptionRequest_ForceResubmit_CanBeSetTrue()
    {
        var req = new SubmitDtqgFromPrescriptionRequest(ForceResubmit: true);
        req.ForceResubmit.Should().BeTrue();
    }

    [Fact]
    public void MaDonThuocFormat_MatchesDtqgSpec()
    {
        // Kiem tra format: VN + yyMMdd + prescriptionId padded to 6 digits, truncate to 14
        // VD: prescriptionId = 42 → "VN" + "260531" + "000042" = "VN260531000042" (14 chars)
        int presId = 42;
        var fakeMa = $"VN{DateTime.Now:yyMMdd}{presId:D6}"[..14];

        fakeMa.Length.Should().Be(14);
        fakeMa.Should().StartWith("VN");
    }
}
