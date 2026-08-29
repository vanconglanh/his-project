using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H02-xx — H-2 (FR-203) / E-Dot2 (BR-25, BR-33): guard tim kiem benh nhan xuyen chi nhanh.
/// Quy tac: khong co quyen cross-branch va khong phai tim chinh xac (SDT/CCCD) thi CHI thay
/// benh nhan da tung co luot kham. Co quyen (patient.cross_branch_search / branch.group_view /
/// cross_branch_view / IgnoreBranchFilter) hoac tim chinh xac thi thay toan bo + ghi audit.
/// </summary>
public class CrossBranchPatientSearchGuardTests
{
    private readonly IPiiProtector _pii = new FakePiiProtector();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly IBranchProvider _branch = Substitute.For<IBranchProvider>();
    private readonly IPermissionChecker _perm = Substitute.For<IPermissionChecker>();

    private const string PatientDaKham = "Nguyễn Văn Đức";
    private const string PatientChuaKham = "Nguyễn Thị Hoa";

    /// <summary>Seed 2 benh nhan cung ho "Nguyễn": 1 nguoi da co encounter, 1 nguoi chua.</summary>
    private ProDiabHis.Infrastructure.Persistence.AppDbContext SeedDb()
    {
        var db = TestDbContextFactory.Create(tenantId: 1);
        var daKham = new Patient
        {
            TenantId = 1, Code = "BNT01000001", FullName = PatientDaKham, Status = "ACTIVE",
            PhoneBidx = _pii.BlindIndex("0901234567", PiiField.Phone)
        };
        var chuaKham = new Patient
        {
            TenantId = 1, Code = "BNT01000002", FullName = PatientChuaKham, Status = "ACTIVE",
            PhoneBidx = _pii.BlindIndex("0987654321", PiiField.Phone)
        };
        db.Patients.AddRange(daKham, chuaKham);
        db.Encounters.Add(new Encounter { TenantId = 1, PatientId = daKham.Id.ToString(), BranchId = 1 });
        db.SaveChanges();
        return db;
    }

    private SearchPatientsQueryHandler Handler(ProDiabHis.Infrastructure.Persistence.AppDbContext db)
        => new(db, _pii, _branch, _perm, _audit);

    // UTC-H02-01 — khong quyen + tim mo (theo ten) -> chi thay benh nhan da tung kham
    // BUG-01 (High) — DA FIX 2026-08-30: SearchPatientsQueryHandler truoc day tinh
    // isExactMatch = (phoneBidx != null || idBidx != null || ...). idBidx = _pii.BlindIndex(q, PiiField.IdNumber)
    // -> PiiNormalizer.NormalizeDigitsOrUpper giu lai MOI ky tu chu-hoac-so, nen voi q = "Nguyễn" van tra ve
    // chuoi hash != null => isExactMatch = true => nhanh han che "chi thay benh nhan da tung kham" KHONG BAO GIO
    // chay. Da sua: isExactMatch chi dung digitsOnly (thuan chu so, dung do dai 10 SDT / 12 CCCD), khong con
    // dung "idBidx != null" lam dieu kien. Verify: test nay PASS that sau fix (khong con Skip).
    [Fact]
    public async Task Search_KhongQuyen_TimTheoTen_ChiThayBenhNhanDaTungKham()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("Nguyễn", 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be(PatientDaKham);
        result.Total.Should().Be(1);
    }

    // UTC-H02-02..04 — co bat ky 1 trong 3 quyen cross-branch -> thay ca 2 benh nhan
    [Theory]
    [InlineData("patient.cross_branch_search")]
    [InlineData("branch.group_view")]
    [InlineData("cross_branch_view")]
    public async Task Search_CoQuyenCrossBranch_ThayTatCaBenhNhan(string permission)
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(ci => (string)ci[0] == permission);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("Nguyễn", 1, 20), CancellationToken.None);

        result.Total.Should().Be(2);
    }

    // UTC-H02-05 — tim chinh xac bang SDT 10 so -> mo khoa cross-branch du khong co quyen (BR-33)
    [Fact]
    public async Task Search_TimChinhXacSoDienThoai_MoKhoaCrossBranch_DuKhongCoQuyen()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("0987654321", 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items[0].FullName.Should().Be(PatientChuaKham);
    }

    // UTC-H02-06 — bien: 9 so (thieu 1 chu so) KHONG duoc coi la tim chinh xac -> van bi han che
    [Fact]
    public async Task Search_ChuoiSo9ChuSo_KhongDuocCoiLaTimChinhXac()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("098765432", 1, 20), CancellationToken.None);

        result.Total.Should().Be(0);
    }

    // UTC-H02-07 — admin (IgnoreBranchFilter) thay tat ca va KHONG ghi audit cross-branch
    [Fact]
    public async Task Search_AdminBoQuaFilterChiNhanh_ThayTatCa_VaKhongGhiAudit()
    {
        _branch.IgnoreBranchFilter.Returns(true);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("Nguyễn", 1, 20), CancellationToken.None);

        result.Total.Should().Be(2);
        await _audit.DidNotReceiveWithAnyArgs().LogAsync(
            default, default!, default, default, default, default, default, default);
    }

    // UTC-H02-08 — truy cap cross-branch cua user thuong PHAI de lai audit VIEW (dau vet phap ly)
    [Fact]
    public async Task Search_CrossBranchCuaUserThuong_PhaiGhiAudit()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission("patient.cross_branch_search").Returns(true);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("Nguyễn", 1, 20), CancellationToken.None);

        result.Total.Should().Be(2);
        await _audit.ReceivedWithAnyArgs(1).LogAsync(
            default, default!, default, default, default, default, default, default);
    }

    // UTC-H02-10 — NGUYEN NHAN GOC cua BUG-01, khang dinh doc lap voi handler:
    // blind index CCCD duoc sinh cho ca chuoi thuan chu tieng Viet, nen "idBidx != null"
    // KHONG the dung lam dau hieu "nguoi dung dang tim chinh xac theo so giay to".
    [Theory]
    [InlineData("Nguyễn")]
    [InlineData("a")]
    [InlineData("Trần Thị")]
    public void BlindIndexCccd_SinhCaChoChuoiThuanChu_NenKhongDungLamDauHieuTimChinhXac(string q)
    {
        var idBidx = _pii.BlindIndex(q, PiiField.IdNumber);

        idBidx.Should().NotBeNull(
            "day chinh la ly do isExactMatch luon true trong SearchPatientsQueryHandler (BUG-01)");
    }

    // UTC-H02-09 — tim khong ra thi khong ghi audit (tranh nhieu log)
    [Fact]
    public async Task Search_KhongCoKetQua_KhongGhiAudit()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(true);
        using var db = SeedDb();

        var result = await Handler(db).Handle(new SearchPatientsQuery("Trần Quốc Toản", 1, 20), CancellationToken.None);

        result.Total.Should().Be(0);
        await _audit.DidNotReceiveWithAnyArgs().LogAsync(
            default, default!, default, default, default, default, default, default);
    }
}
