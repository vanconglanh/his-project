using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Notifications;
using ProDiabHis.Infrastructure.Notifications;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H01-xx — H-1 (FR-112) facade gui thong bao nhac lich hen (SMS / Zalo ZNS).
/// Trong tam: routing dung kenh, guard so dien thoai, guard kenh chua cau hinh,
/// va viec doc lai credential moi lan gui (doi config qua UI co hieu luc ngay).
/// </summary>
public class NotificationSenderTests
{
    private readonly INotificationChannelCredentialProvider _credentials =
        Substitute.For<INotificationChannelCredentialProvider>();
    private readonly ILogger<NotificationSender> _logger = Substitute.For<ILogger<NotificationSender>>();

    private static IChannelSender FakeSender(NotificationChannel channel)
    {
        var s = Substitute.For<IChannelSender>();
        s.Channel.Returns(channel);
        return s;
    }

    private static NotificationChannelConfig Config(NotificationChannel ch) =>
        new(ch, "test-provider", new Dictionary<string, string> { ["api_key"] = "k", ["secret_key"] = "s" });

    private static Dictionary<string, string> Data() => new()
    {
        ["message"] = "Nhắc lịch hẹn tái khám 08:30 ngày 31/08/2026",
        ["patient_name"] = "Nguyễn Văn Đức",
    };

    // UTC-H01-01 — thieu so dien thoai -> chan truoc khi goi provider (khong ton tien SMS)
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_KhiThieuSoDienThoai_TraVe_RECIPIENT_INVALID(string phone)
    {
        var sms = FakeSender(NotificationChannel.Sms);
        _credentials.GetForCurrentAsync(NotificationChannel.Sms, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(Config(NotificationChannel.Sms)));

        var sut = new NotificationSender(_credentials, new[] { sms }, _logger);
        var result = await sut.SendAsync(NotificationChannel.Sms, phone, "APPOINTMENT_REMINDER", Data());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOTIFICATION_RECIPIENT_INVALID");
        await sms.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default!, default);
    }

    // UTC-H01-02 — kenh khong co sender dang ky -> UNSUPPORTED, khong nem exception
    [Fact]
    public async Task Send_KhiKenhKhongCoSender_TraVe_CHANNEL_UNSUPPORTED()
    {
        var sms = FakeSender(NotificationChannel.Sms);
        var sut = new NotificationSender(_credentials, new[] { sms }, _logger);

        var result = await sut.SendAsync(NotificationChannel.ZaloZns, "0901234567", "APPOINTMENT_REMINDER", Data());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOTIFICATION_CHANNEL_UNSUPPORTED");
    }

    // UTC-H01-03 — kenh chua cau hinh / dang tat -> NOT_CONFIGURED
    [Fact]
    public async Task Send_KhiChuaCauHinhKenh_TraVe_CHANNEL_NOT_CONFIGURED()
    {
        var zalo = FakeSender(NotificationChannel.ZaloZns);
        _credentials.GetForCurrentAsync(NotificationChannel.ZaloZns, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(null));

        var sut = new NotificationSender(_credentials, new[] { zalo }, _logger);
        var result = await sut.SendAsync(NotificationChannel.ZaloZns, "0901234567", "APPOINTMENT_REMINDER", Data());

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOTIFICATION_CHANNEL_NOT_CONFIGURED");
        await zalo.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default!, default);
    }

    // UTC-H01-04 — happy path: route dung sender theo Channel, truyen nguyen config/phone/template
    [Fact]
    public async Task Send_HopLe_RouteDungSenderVaTruyenDuThamSo()
    {
        var sms = FakeSender(NotificationChannel.Sms);
        var zalo = FakeSender(NotificationChannel.ZaloZns);
        var cfg = Config(NotificationChannel.ZaloZns);
        _credentials.GetForCurrentAsync(NotificationChannel.ZaloZns, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(cfg));
        zalo.SendAsync(cfg, "0901234567", "APPOINTMENT_REMINDER", Arg.Any<IDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NotificationSendResult>.Success(
                new NotificationSendResult(true, "msg-001", "{\"error\":0}"))));

        var sut = new NotificationSender(_credentials, new[] { sms, zalo }, _logger);
        var result = await sut.SendAsync(NotificationChannel.ZaloZns, "0901234567", "APPOINTMENT_REMINDER", Data());

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProviderMessageId.Should().Be("msg-001");
        await sms.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default!, default);
    }

    // UTC-H01-05 — job chay nen: phai resolve credential theo tenant/branch chi dinh, khong theo HTTP context
    [Fact]
    public async Task SendForTenant_ResolveCredentialTheoTenantVaBranch()
    {
        var sms = FakeSender(NotificationChannel.Sms);
        var cfg = Config(NotificationChannel.Sms);
        _credentials.GetAsync(7, 3, NotificationChannel.Sms, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(cfg));
        sms.SendAsync(cfg, "0987654321", "APPOINTMENT_REMINDER", Arg.Any<IDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NotificationSendResult>.Success(
                new NotificationSendResult(true, "sms-1", "100"))));

        var sut = new NotificationSender(_credentials, new[] { sms }, _logger);
        var result = await sut.SendForTenantAsync(7, 3, NotificationChannel.Sms, "0987654321",
            "APPOINTMENT_REMINDER", Data());

        result.IsSuccess.Should().BeTrue();
        await _credentials.Received(1).GetAsync(7, 3, NotificationChannel.Sms, Arg.Any<CancellationToken>());
        await _credentials.DidNotReceiveWithAnyArgs().GetForCurrentAsync(default, default);
    }

    // UTC-H01-06 — moi lan gui deu doc lai config (khong cache) -> reset credential qua UI hieu luc ngay
    [Fact]
    public async Task Send_GoiNhieuLan_DocLaiConfigMoiLan()
    {
        var sms = FakeSender(NotificationChannel.Sms);
        _credentials.GetForCurrentAsync(NotificationChannel.Sms, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(Config(NotificationChannel.Sms)));
        sms.SendAsync(Arg.Any<NotificationChannelConfig>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<NotificationSendResult>.Success(
                new NotificationSendResult(true, null, null))));

        var sut = new NotificationSender(_credentials, new[] { sms }, _logger);
        await sut.SendAsync(NotificationChannel.Sms, "0901234567", "APPOINTMENT_REMINDER", Data());
        await sut.SendAsync(NotificationChannel.Sms, "0901234567", "APPOINTMENT_REMINDER", Data());

        await _credentials.Received(2).GetForCurrentAsync(NotificationChannel.Sms, Arg.Any<CancellationToken>());
    }

    // UTC-H01-07 — Test ket noi khi chua luu cau hinh -> thong bao huong dan tieng Viet
    [Fact]
    public async Task TestConnection_KhiChuaCauHinh_TraVeThongBaoHuongDanLuuTruoc()
    {
        var sms = FakeSender(NotificationChannel.Sms);
        _credentials.GetForCurrentAsync(NotificationChannel.Sms, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NotificationChannelConfig?>(null));

        var sut = new NotificationSender(_credentials, new[] { sms }, _logger);
        var result = await sut.TestConnectionAsync(NotificationChannel.Sms);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOTIFICATION_CHANNEL_NOT_CONFIGURED");
        result.ErrorMessage.Should().Contain("Vui lòng lưu cấu hình trước khi test.");
    }

    // UTC-H01-08 — Test ket noi kenh khong ho tro -> UNSUPPORTED truoc khi cham credential provider
    [Fact]
    public async Task TestConnection_KhiKenhKhongHoTro_TraVeUnsupported_VaKhongDocCredential()
    {
        var sut = new NotificationSender(_credentials, Array.Empty<IChannelSender>(), _logger);

        var result = await sut.TestConnectionAsync(NotificationChannel.ZaloZns);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOTIFICATION_CHANNEL_UNSUPPORTED");
        await _credentials.DidNotReceiveWithAnyArgs().GetForCurrentAsync(default, default);
    }
}
