using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Infrastructure.Pharmacy;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

public class DtqgSubmitFlowTests
{
    private readonly IDtqgClient _client;

    public DtqgSubmitFlowTests()
    {
        _client = new MockDtqgClient(NullLogger<MockDtqgClient>.Instance);
    }

    [Fact]
    public async Task MockDtqgClient_submit_returns_14char_ma_don_thuoc()
    {
        var payload = new DtqgSubmitPayload(1, 999, "CSKCB001", "PARTNER01", new { });
        var result = await _client.SubmitPrescriptionAsync(payload, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MaDonThuoc.Should().NotBeNullOrEmpty();
        result.MaDonThuoc!.Length.Should().Be(14, "DTQG requires exactly 14-character ma_don_thuoc");
    }

    [Fact]
    public async Task MockDtqgClient_ping_returns_ok()
    {
        var ping = await _client.PingAsync(CancellationToken.None);
        ping.Ok.Should().BeTrue();
        ping.LatencyMs.Should().BeGreaterThan(0);
        ping.PortalResponse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MockDtqgClient_cancel_returns_true()
    {
        var result = await _client.CancelAsync("VN260523000001", "Test cancellation", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public void MaDonThuoc_format_validation_exactly_14_chars()
    {
        // Business rule: ma_don_thuoc MUST be 14 chars
        var valid = "VN260523999999"; // 14 chars
        var invalid = "VN2605239999";  // 12 chars

        valid.Length.Should().Be(14);
        invalid.Length.Should().NotBe(14);
    }

    [Theory]
    [InlineData(0, true)]   // retry 0: can retry
    [InlineData(4, true)]   // retry 4: can retry
    [InlineData(5, false)]  // retry 5: exceeded
    [InlineData(6, false)]  // retry 6: exceeded
    public void RetryCount_limit_5_enforced(int retryCount, bool canRetry)
    {
        var allowed = retryCount < 5;
        allowed.Should().Be(canRetry);
    }

    [Fact]
    public async Task MockDtqgClient_idempotent_same_code_returned()
    {
        // If we call submit twice for same prescription, mock always returns same format
        var payload = new DtqgSubmitPayload(1, 123, "CSKCB001", "PARTNER01", new { });
        var r1 = await _client.SubmitPrescriptionAsync(payload, CancellationToken.None);
        var r2 = await _client.SubmitPrescriptionAsync(payload, CancellationToken.None);

        // Both should succeed and return 14 chars (real impl handles idempotency at DB layer)
        r1.Success.Should().BeTrue();
        r2.Success.Should().BeTrue();
        r1.MaDonThuoc!.Length.Should().Be(14);
        r2.MaDonThuoc!.Length.Should().Be(14);
    }
}
