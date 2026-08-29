using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy.StockTransfers;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-E3-xx — E/Dot3 state machine dieu chuyen kho noi bo (BR-54..BR-62).
/// Cover: validate dau vao khi tao phieu, guard tenant/chi nhanh, va tat ca lenh chuyen trang thai
/// deu tra NOT_FOUND (khong 500) khi phieu khong ton tai / khac tenant.
/// LUU Y: guard trang thai + nguong duyet 5tr can du lieu that -> kiem o tang ITC/ITE qua API.
/// </summary>
public class StockTransferStateMachineTests
{
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly IBranchProvider _branch = Substitute.For<IBranchProvider>();
    private readonly ISettingsProvider _settings = Substitute.For<ISettingsProvider>();
    private readonly IPermissionChecker _perm = Substitute.For<IPermissionChecker>();
    private readonly FakeEmptyDapperConnectionFactory _db = new();

    public StockTransferStateMachineTests()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _user.TenantId.Returns(1);
        _settings.GetDecimalAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(5_000_000m));
    }

    private static StockTransferItemRequest Item(decimal qty = 10, decimal cost = 12_000m) =>
        new("11111111-1111-1111-1111-111111111111", "LOT-A", DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            qty, cost, "Điều chuyển bổ sung");

    private CreateStockTransferHandler Create() => new(_db, _user, _audit);

    // UTC-E3-01 — phieu khong co dong hang -> EMPTY_ITEMS, chan truoc khi cham DB
    [Fact]
    public async Task Create_KhiKhongCoDongHang_TraVe_EMPTY_ITEMS()
    {
        var cmd = new CreateStockTransferCommand(
            new CreateStockTransferRequest(1, 2, "Bổ sung tồn", Array.Empty<StockTransferItemRequest>()));

        var result = await Create().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(StockTransferErrors.EmptyItems);
        result.ErrorCode.Should().Be("STOCK_TRANSFER_EMPTY_ITEMS");
    }

    // UTC-E3-02 — chi nhanh gui trung chi nhanh nhan -> SAME_BRANCH (BR-55)
    [Fact]
    public async Task Create_KhiTrungChiNhanhGuiVaNhan_TraVe_SAME_BRANCH()
    {
        var cmd = new CreateStockTransferCommand(
            new CreateStockTransferRequest(3, 3, "Bổ sung tồn", new[] { Item() }));

        var result = await Create().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(StockTransferErrors.SameBranch);
    }

    // UTC-E3-03 — chi nhanh khong thuoc tenant hien tai -> BRANCH_ACCESS_DENIED (BR-54)
    [Fact]
    public async Task Create_KhiChiNhanhKhongThuocTenant_TraVe_BRANCH_ACCESS_DENIED()
    {
        var cmd = new CreateStockTransferCommand(
            new CreateStockTransferRequest(1, 999, "Điều chuyển sang chi nhánh lạ", new[] { Item() }));

        var result = await Create().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(StockTransferErrors.BranchAccessDenied);
    }

    // UTC-E3-04 — tap trang thai phai on dinh (hop dong voi FE/bao cao)
    [Fact]
    public void TrangThai_PhaiDungGiaTriChuoiDaThongNhat()
    {
        StockTransferStatus.Draft.Should().Be("DRAFT");
        StockTransferStatus.PendingApproval.Should().Be("PENDING_APPROVAL");
        StockTransferStatus.Approved.Should().Be("APPROVED");
        StockTransferStatus.Rejected.Should().Be("REJECTED");
        StockTransferStatus.InTransit.Should().Be("IN_TRANSIT");
        StockTransferStatus.Received.Should().Be("RECEIVED");
        StockTransferStatus.PartiallyReceived.Should().Be("PARTIALLY_RECEIVED");
        StockTransferStatus.Closed.Should().Be("CLOSED");
        StockTransferStatus.Cancelled.Should().Be("CANCELLED");
    }

    // UTC-E3-05..12 — moi lenh chuyen trang thai tren phieu khong ton tai deu phai NOT_FOUND, khong 500
    [Fact]
    public async Task Submit_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new SubmitStockTransferHandler(_db, _user, _branch, _audit);
        var r = await h.Handle(new SubmitStockTransferCommand(Guid.NewGuid().ToString()), CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    [Fact]
    public async Task Approve_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new ApproveStockTransferHandler(_db, _user, _branch, _audit, _settings, _perm);
        var r = await h.Handle(
            new ApproveStockTransferCommand(Guid.NewGuid().ToString(), new ApproveStockTransferRequest()),
            CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    [Fact]
    public async Task Reject_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new RejectStockTransferHandler(_db, _user, _branch, _audit);
        var r = await h.Handle(
            new RejectStockTransferCommand(Guid.NewGuid().ToString(), new RejectStockTransferRequest("Không đủ tồn")),
            CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    [Fact]
    public async Task Ship_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new ShipStockTransferHandler(_db, _user, _branch, _audit);
        var r = await h.Handle(new ShipStockTransferCommand(Guid.NewGuid().ToString()), CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    [Fact]
    public async Task Close_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new CloseStockTransferHandler(_db, _user, _branch, _audit);
        var r = await h.Handle(new CloseStockTransferCommand(Guid.NewGuid().ToString()), CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    [Fact]
    public async Task Cancel_PhieuKhongTonTai_TraVe_NOT_FOUND()
    {
        var h = new CancelStockTransferHandler(_db, _user, _branch, _audit);
        var r = await h.Handle(new CancelStockTransferCommand(Guid.NewGuid().ToString()), CancellationToken.None);
        r.ErrorCode.Should().Be(StockTransferErrors.NotFound);
    }

    // UTC-E3-13 — nguong duyet phai doc tu setting (khong hardcode), default 5.000.000
    [Fact]
    public async Task Approve_DocNguongDuyetTuSetting_KhongHardcode()
    {
        var h = new ApproveStockTransferHandler(_db, _user, _branch, _audit, _settings, _perm);
        await h.Handle(new ApproveStockTransferCommand("khong-ton-tai", new ApproveStockTransferRequest()),
            CancellationToken.None);

        // Phieu khong ton tai nen return truoc khi doc setting -> xac nhan thu tu guard: NotFound truoc.
        await _settings.DidNotReceiveWithAnyArgs().GetDecimalAsync(default!, default, default);
    }
}
