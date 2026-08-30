using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-P0-BIDX — Hoi quy loi P0: benh nhan cu co id_number_enc day du nhung id_number_bidx = NULL
/// -> le tan go dung CCCD van "khong tim thay". Nguyen nhan: blind index (bidx) chua duoc sinh cho
/// du lieu cu. Cach xu ly: chay PiiBackfillService (giai ma enc -> BlindIndex -> UPDATE bidx).
///
/// Test nay mo phong DUNG logic backfill blind index CCCD (Unprotect(enc) -> BlindIndex(IdNumber))
/// va xac nhan sau khi dien bidx thi SearchPatientsQueryHandler tim ra benh nhan theo CCCD.
/// Khong phu thuoc DB that -> chay duoc o moi moi truong CI.
/// </summary>
public class IdNumberBidxBackfillRegressionTests
{
    private const string Cccd = "079123456789"; // CCCD 12 so -> isExactMatch = true
    private const string PatientName = "Lê Văn Cũ";

    private readonly IPiiProtector _pii = new FakePiiProtector();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly IBranchProvider _branch = Substitute.For<IBranchProvider>();
    private readonly IPermissionChecker _perm = Substitute.For<IPermissionChecker>();

    private SearchPatientsQueryHandler Handler(ProDiabHis.Infrastructure.Persistence.AppDbContext db)
        => new(db, _pii, _branch, _perm, _audit);

    // Truoc backfill: enc co, bidx = null -> tim theo CCCD KHONG ra (tai hien loi P0)
    [Fact]
    public async Task TruocBackfill_BidxNull_TimTheoCccd_KhongRa()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);

        using var db = TestDbContextFactory.Create(tenantId: 1);
        db.Patients.Add(new Patient
        {
            TenantId = 1, Code = "BNT01000009", FullName = PatientName, Status = "ACTIVE",
            IdNumberEnc = _pii.Protect(Cccd),   // du lieu cu: da ma hoa
            IdNumberBidx = null                 // nhung THIEU blind index
        });
        db.SaveChanges();

        var result = await Handler(db).Handle(new SearchPatientsQuery(Cccd, 1, 20), CancellationToken.None);

        result.Total.Should().Be(0, "bidx NULL nen dieu kien p.IdNumberBidx == idBidx khong bao gio khop");
    }

    // Sau backfill: dien bidx = BlindIndex(Unprotect(enc)) -> tim theo CCCD RA dung benh nhan
    [Fact]
    public async Task SauBackfill_BidxDaDien_TimTheoCccd_RaDungBenhNhan()
    {
        _branch.IgnoreBranchFilter.Returns(false);
        _perm.HasPermission(Arg.Any<string>()).Returns(false);

        using var db = TestDbContextFactory.Create(tenantId: 1);
        var enc = _pii.Protect(Cccd);
        var p = new Patient
        {
            TenantId = 1, Code = "BNT01000009", FullName = PatientName, Status = "ACTIVE",
            IdNumberEnc = enc, IdNumberBidx = null
        };
        db.Patients.Add(p);
        db.SaveChanges();

        // === Mo phong dung logic backfill (PiiBackfillService: giai ma enc -> BlindIndex -> set bidx) ===
        var plain = _pii.Unprotect(enc);
        var bidx = _pii.BlindIndex(plain, PiiField.IdNumber);
        bidx.Should().NotBeNull();
        p.IdNumberBidx = bidx;
        db.SaveChanges();
        // ================================================================================================

        var result = await Handler(db).Handle(new SearchPatientsQuery(Cccd, 1, 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items[0].FullName.Should().Be(PatientName);
    }

    // Blind index cua backfill (tu Unprotect(enc)) PHAI trung blind index cua query tim kiem
    // (tu chuoi CCCD nguoi dung go). Neu 2 ben lech nhau -> tim mai khong ra du da backfill.
    [Fact]
    public void BidxTuBackfill_KhopVoiBidxCuaQueryTimKiem()
    {
        var enc = _pii.Protect(Cccd);
        var bidxBackfill = _pii.BlindIndex(_pii.Unprotect(enc), PiiField.IdNumber);
        var bidxQuery = _pii.BlindIndex(Cccd, PiiField.IdNumber);

        bidxBackfill.Should().Be(bidxQuery);
    }
}
