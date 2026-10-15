using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>
/// FIX 2026-10-16 (A1): GET /api/v1/patients?search= truoc day BO QUA tu khoa -> tra ve toan bo
/// benh nhan ke ca chuoi vo nghia. Sau fix: loc theo ten/ma BN (LIKE) + SDT/CMND (blind index exact),
/// van giu tenant filter (EF Core Global Query Filter).
/// </summary>
public class ListPatientsSearchTests
{
    private readonly IPiiProtector _pii = new FakePiiProtector();

    private ProDiabHis.Infrastructure.Persistence.AppDbContext SeedDb()
    {
        var db = TestDbContextFactory.Create(tenantId: 1);
        db.Patients.AddRange(
            new Patient
            {
                TenantId = 1, Code = "BNT01000001", FullName = "NGUYỄN VĂN AN", Status = "ACTIVE",
                PhoneBidx = _pii.BlindIndex("0901234567", PiiField.Phone)
            },
            new Patient
            {
                TenantId = 1, Code = "BNT01000002", FullName = "TRẦN THỊ BÌNH", Status = "ACTIVE",
                PhoneBidx = _pii.BlindIndex("0987654321", PiiField.Phone)
            },
            new Patient
            {
                TenantId = 1, Code = "BNT01000003", FullName = "LÊ VĂN CƯỜNG", Status = "ACTIVE"
            });
        db.SaveChanges();
        return db;
    }

    private ListPatientsQueryHandler Handler(ProDiabHis.Infrastructure.Persistence.AppDbContext db)
        => new(db, _pii);

    private static ListPatientsQuery Q(string? search)
        => new(Page: 1, PageSize: 20, Sort: null, Status: null, Gender: null, Search: search);

    [Fact]
    public async Task KhongCoTuKhoa_TraVeToanBo()
    {
        using var db = SeedDb();
        var result = await Handler(db).Handle(Q(null), CancellationToken.None);
        result.Total.Should().Be(3);
    }

    [Fact]
    public async Task TimTheoTen_LocDungBenhNhan()
    {
        using var db = SeedDb();
        var result = await Handler(db).Handle(Q("trần"), CancellationToken.None);
        result.Total.Should().Be(1);
        result.Items[0].FullName.Should().Be("TRẦN THỊ BÌNH");
    }

    [Fact]
    public async Task TimTheoMaBenhNhan_LocDung()
    {
        using var db = SeedDb();
        var result = await Handler(db).Handle(Q("BNT01000003"), CancellationToken.None);
        result.Total.Should().Be(1);
        result.Items[0].FullName.Should().Be("LÊ VĂN CƯỜNG");
    }

    [Fact]
    public async Task TimTheoSoDienThoai_QuaBlindIndex_LocDung()
    {
        using var db = SeedDb();
        var result = await Handler(db).Handle(Q("0987654321"), CancellationToken.None);
        result.Total.Should().Be(1);
        result.Items[0].FullName.Should().Be("TRẦN THỊ BÌNH");
    }

    [Fact]
    public async Task ChuoiVoNghia_TraVe0()
    {
        using var db = SeedDb();
        var result = await Handler(db).Handle(Q("ZZZZZZZZZZ999"), CancellationToken.None);
        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
