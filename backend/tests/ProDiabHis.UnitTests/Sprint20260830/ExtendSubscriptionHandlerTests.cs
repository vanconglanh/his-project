using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Packages;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H14-xx — H-14 (FR-1211) gia han goi dich vu da het han nhung con dinh muc.
/// Kiem tra thu tu guard: PACKAGE_EXTENSION_DISABLED -> PACKAGE_SUBSCRIPTION_NOT_FOUND
/// -> PACKAGE_NOT_EXPIRED -> PACKAGE_NO_REMAINING_ENTITLEMENT.
/// Dung FakeEmptyDbConnection nen chi cover duoc cac nhanh return som (khong cham transaction).
/// </summary>
public class ExtendSubscriptionHandlerTests
{
    private readonly ISettingsProvider _settings = Substitute.For<ISettingsProvider>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly FakeTenantProvider _tenant = new(1);

    private ExtendSubscriptionHandler CreateHandler()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _user.TenantId.Returns(1);
        return new ExtendSubscriptionHandler(
            new FakeEmptyDapperConnectionFactory(), _tenant, _user, _audit, _settings);
    }

    private static ExtendSubscriptionCommand Cmd(string? note = "Gia han theo yeu cau bệnh nhân")
        => new(Guid.NewGuid(), new ExtendSubscriptionRequest(note));

    // UTC-H14-01 — tinh nang tat (mac dinh 0) thi khong duoc gia han
    [Fact]
    public async Task Extend_KhiSettingBang0_TraVe_PACKAGE_EXTENSION_DISABLED()
    {
        _settings.GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        var result = await CreateHandler().Handle(Cmd(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PACKAGE_EXTENSION_DISABLED");
        result.ErrorMessage.Should().Contain("package_expiry_extension_days");
    }

    // UTC-H14-02 — bien am: setting bi cau hinh am van phai coi la tat, khong duoc lui ngay het han
    [Theory]
    [InlineData(-1)]
    [InlineData(-365)]
    public async Task Extend_KhiSettingAm_VanBiChan(int days)
    {
        _settings.GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(days));

        var result = await CreateHandler().Handle(Cmd(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PACKAGE_EXTENSION_DISABLED");
    }

    // UTC-H14-03 — bien duong nho nhat (1 ngay) da bat tinh nang -> di tiep den buoc tra cuu goi
    [Fact]
    public async Task Extend_KhiSettingBang1_VuotQuaGuardDisabled_VaTraVe_NOT_FOUND()
    {
        _settings.GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var result = await CreateHandler().Handle(Cmd(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PACKAGE_SUBSCRIPTION_NOT_FOUND");
    }

    // UTC-H14-04 — goi khong ton tai / khac tenant -> NOT_FOUND (khong lo thong tin tenant khac)
    [Fact]
    public async Task Extend_KhiGoiKhongTonTai_TraVe_PACKAGE_SUBSCRIPTION_NOT_FOUND()
    {
        _settings.GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(30));

        var result = await CreateHandler().Handle(Cmd(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PACKAGE_SUBSCRIPTION_NOT_FOUND");
        result.ErrorMessage.Should().Be("Không tìm thấy gói định mức đã mua");
    }

    // UTC-H14-05 — handler phai doc dung key setting voi default 0 (khong hardcode so ngay)
    [Fact]
    public async Task Extend_DocDungKeySetting_VaDefaultLa0()
    {
        _settings.GetIntAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        await CreateHandler().Handle(Cmd(), CancellationToken.None);

        await _settings.Received(1)
            .GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>());
    }

    // UTC-H14-06 — khong ghi audit khi guard chan (khong tao rac audit)
    [Fact]
    public async Task Extend_KhiBiChan_KhongGhiAudit()
    {
        _settings.GetIntAsync("package_expiry_extension_days", 0, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        await CreateHandler().Handle(Cmd(), CancellationToken.None);

        await _audit.DidNotReceive().LogAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
    }
}
