using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H09-xx — H-9 (FR-911) sinh QR VietQR DONG theo so tien con phai thu cua hoa don.
/// Trong tam: so tien tren QR phai bang so tien con thieu (khong phai tong hoa don),
/// va cac guard hoa don huy / da thu du / chua cau hinh tai khoan ngan hang.
/// </summary>
public class DynamicBillingQrHandlerTests
{
    private readonly ISettingsProvider _settings = Substitute.For<ISettingsProvider>();
    private readonly IVietQrBuilder _qr = Substitute.For<IVietQrBuilder>();
    private readonly FakeTenantProvider _tenant = new(1);

    public DynamicBillingQrHandlerTests()
    {
        _settings.GetRawAsync("bil.qr_bank_bin", Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>("970436"));
        _settings.GetRawAsync("bil.qr_bank_account_no", Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>("1234567890"));
        _settings.GetRawAsync("bil.qr_bank_account_name", Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>("PHONG KHAM PRO DIAB"));
        _qr.Build(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new VietQrBuildResult("BASE64PAYLOAD", "00020101021238...", "https://img.vietqr.io/x.png"));
    }

    private static ProDiabHis.Domain.Entities.Billing NewBilling(decimal payable, decimal paid, decimal balance,
        string status = BillingStatus.Finalized, string? billNo = "HD-0001")
        => new()
        {
            Id = Guid.NewGuid(), TenantId = 1, BillNo = billNo, Status = status,
            PatientPayable = payable, PaidAmount = paid, Balance = balance
        };

    // UTC-H09-01 — hoa don khong ton tai -> BILLING_NOT_FOUND
    [Fact]
    public async Task Qr_HoaDonKhongTonTai_TraVe_BILLING_NOT_FOUND()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(Guid.NewGuid()), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("BILLING_NOT_FOUND");
    }

    // UTC-H09-02 — hoa don da huy -> BILLING_VOID (khong cho quet QL tra tien cho hoa don huy)
    [Fact]
    public async Task Qr_HoaDonDaHuy_TraVe_BILLING_VOID()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(500_000m, 0m, 500_000m, BillingStatus.Void);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.ErrorCode.Should().Be("BILLING_VOID");
    }

    // UTC-H09-03 — bien: da thu du (balance=0, payable=paid) -> BILLING_NO_AMOUNT_DUE
    [Fact]
    public async Task Qr_HoaDonDaThuDu_TraVe_BILLING_NO_AMOUNT_DUE()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(500_000m, 500_000m, 0m, BillingStatus.Paid);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.ErrorCode.Should().Be("BILLING_NO_AMOUNT_DUE");
    }

    // UTC-H09-04 — chua cau hinh tai khoan nhan tien -> BANK_ACCOUNT_NOT_CONFIGURED (tieng Viet)
    [Fact]
    public async Task Qr_ChuaCauHinhTaiKhoan_TraVe_BANK_ACCOUNT_NOT_CONFIGURED()
    {
        _settings.GetRawAsync("bil.qr_bank_bin", Arg.Any<CancellationToken>()).Returns(Task.FromResult<string?>(null));
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(500_000m, 0m, 500_000m);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.ErrorCode.Should().Be("BANK_ACCOUNT_NOT_CONFIGURED");
        r.ErrorMessage.Should().Be("Chưa cấu hình tài khoản nhận thanh toán");
    }

    // UTC-H09-05 — HAPPY: hoa don thu mot phan -> QR phai mang so tien CON LAI (khong phai tong)
    [Fact]
    public async Task Qr_ThuMotPhan_SoTienTrenQrLaSoConLai()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(payable: 1_000_000m, paid: 400_000m, balance: 600_000m, status: BillingStatus.PartialPaid);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Amount.Should().Be(600_000m);
        r.Value.BillingId.Should().Be(b.Id);
        r.Value.QrPayload.Should().NotBeNullOrWhiteSpace();
        r.Value.TransactionRef.Should().StartWith("PD");
        _qr.Received(1).Build(600_000m, Arg.Is<string>(s => s.Contains("HD-0001")), "970436", "1234567890",
            "PHONG KHAM PRO DIAB");
    }

    // UTC-H09-06 — fallback khi Balance chua duoc tinh (=0) nhung van con no theo payable - paid
    [Fact]
    public async Task Qr_KhiBalanceChuaTinh_TinhLaiTuPayableTruPaid()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(payable: 300_000m, paid: 100_000m, balance: 0m);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Amount.Should().Be(200_000m);
    }

    // UTC-H09-07 — chua dat ten tai khoan -> fallback "PHONG KHAM", khong nem loi
    [Fact]
    public async Task Qr_ChuaDatTenTaiKhoan_FallbackPhongKham()
    {
        _settings.GetRawAsync("bil.qr_bank_account_name", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("  "));
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(500_000m, 0m, 500_000m);
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        _qr.Received(1).Build(500_000m, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "PHONG KHAM");
    }

    // UTC-H09-08 — cach ly tenant: hoa don tenant khac -> NOT_FOUND (khong lo du lieu phong kham khac)
    [Fact]
    public async Task Qr_HoaDonTenantKhac_TraVe_BILLING_NOT_FOUND()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var b = NewBilling(500_000m, 0m, 500_000m);
        b.TenantId = 2;
        db.Billings.Add(b); await db.SaveChangesAsync();
        var sut = new GenerateDynamicBillingQrHandler(db, _tenant, _settings, _qr);

        var r = await sut.Handle(new GenerateDynamicBillingQrCommand(b.Id), CancellationToken.None);

        r.ErrorCode.Should().Be("BILLING_NOT_FOUND");
    }
}
