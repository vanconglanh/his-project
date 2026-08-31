using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.IntegrationTests.Patients;

/// <summary>
/// Tiep don + quet QR CCCD (muc I cua PRD quet-qr-cccd-20260830) — kiem tra 3 case trung
/// tren MySQL THAT: NONE (chua co ho so) / EXACT_MATCH (khop hoan toan) / FIELD_MISMATCH
/// (cung CCCD nhung co truong lech, tra ve dung danh sach truong lech de le tan doi chieu).
///
/// Day la diem VAO cua toan bo hanh trinh kham — sai o day thi tao ho so trung hoac ghi de
/// nham benh nhan, nen phai co test hoi quy.
/// </summary>
[Collection("MySql")]
public class CccdDuplicateIntegrationTests : IClassFixture<MySqlTestFixture>, IAsyncLifetime
{
    private const int TenantId = 1;
    private const string Cccd = "079085001234";

    private readonly MySqlTestFixture _fixture;
    private AppDbContext _db = null!;
    private CheckCccdDuplicateQueryHandler _handler = null!;
    private Guid _patientId;

    public CccdDuplicateIntegrationTests(MySqlTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        // DbContext rieng voi tenant = 1 (fixture mac dinh dung tenant 0)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(_fixture.ConnectionString, ServerVersion.AutoDetect(_fixture.ConnectionString))
            .Options;
        _db = new AppDbContext(options, new FixedTenantProvider(TenantId), new AllBranchProvider());
        await _db.Database.EnsureCreatedAsync();

        _patientId = Guid.NewGuid();
        _db.Patients.Add(new Patient
        {
            Id = _patientId,
            TenantId = TenantId,
            Code = "BN-CCCD-IT-001",
            FullName = "Nguyễn Thị Bích Hạnh",
            Gender = "FEMALE",
            DateOfBirth = new DateOnly(1985, 3, 15),
            IdNumberHash = PatientMappingHelper.ComputeIdNumberHash(Cccd),
            Street = "12 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM",
            Status = "ACTIVE",
        });
        await _db.SaveChangesAsync();

        _handler = new CheckCccdDuplicateQueryHandler(_db, new FixedTenantProvider(TenantId));
    }

    public async Task DisposeAsync()
    {
        var p = await _db.Patients.FirstOrDefaultAsync(x => x.Id == _patientId);
        if (p is not null) { _db.Patients.Remove(p); await _db.SaveChangesAsync(); }
        await _db.DisposeAsync();
    }

    /// <summary>UTC-REC-02 (Case 1): CCCD chua co trong he thong -&gt; NONE, khong tra ho so nao.</summary>
    [DockerAvailableFact]
    public async Task Case1_CccdChuaTonTai_TraVeNone()
    {
        var r = await _handler.Handle(
            new CheckCccdDuplicateQuery("999999999999", "Người Lạ", new DateOnly(1990, 1, 1), "MALE", "Hà Nội"),
            CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Case.Should().Be(CccdDuplicateCase.None);
        r.Value.PatientId.Should().BeNull();
        r.Value.FieldDiffs.Should().BeEmpty();
    }

    /// <summary>UTC-REC-04 (Case 2): quet lai the cu, moi truong y het -&gt; EXACT_MATCH, khong co truong lech.</summary>
    [DockerAvailableFact]
    public async Task Case2_DuLieuYHet_TraVeExactMatch()
    {
        var r = await _handler.Handle(
            new CheckCccdDuplicateQuery(Cccd, "Nguyễn Thị Bích Hạnh", new DateOnly(1985, 3, 15), "FEMALE",
                "12 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM"),
            CancellationToken.None);

        r.Value!.Case.Should().Be(CccdDuplicateCase.ExactMatch);
        r.Value.PatientId.Should().Be(_patientId);
        r.Value.PatientCode.Should().Be("BN-CCCD-IT-001");
        r.Value.FieldDiffs.Should().BeEmpty();
    }

    /// <summary>
    /// UTC-REC-05 (Case 3): cung CCCD nhung ho ten + dia chi lech (BN doi ten sau ket hon,
    /// chuyen nha) -&gt; FIELD_MISMATCH kem dung 2 truong lech, gia tri cu va moi.
    /// </summary>
    [DockerAvailableFact]
    public async Task Case3_HoTenVaDiaChiLech_TraVeFieldMismatch_DungTungTruong()
    {
        var r = await _handler.Handle(
            new CheckCccdDuplicateQuery(Cccd, "Nguyễn Thị Bích Hằng", new DateOnly(1985, 3, 15), "FEMALE",
                "99 Nguyễn Huệ, Quận 1, TP.HCM"),
            CancellationToken.None);

        r.Value!.Case.Should().Be(CccdDuplicateCase.FieldMismatch);
        r.Value.PatientId.Should().Be(_patientId);

        r.Value.FieldDiffs.Should().HaveCount(2);
        r.Value.FieldDiffs.Select(d => d.Field)
            .Should().BeEquivalentTo(new[] { CccdComparableField.FullName, CccdComparableField.Address });

        var name = r.Value.FieldDiffs.Single(d => d.Field == CccdComparableField.FullName);
        name.OldValue.Should().Be("Nguyễn Thị Bích Hạnh");
        name.NewValue.Should().Be("Nguyễn Thị Bích Hằng");
    }

    /// <summary>
    /// BR-DUP-005: so sanh phai chuan hoa (trim + lowercase + gom khoang trang) — khac hoa/thuong
    /// hay thua khoang trang KHONG duoc coi la lech, tranh bat le tan xac nhan vo ich.
    /// </summary>
    [DockerAvailableFact]
    public async Task ChuanHoaTruocKhiSoSanh_KhacHoaThuongVaKhoangTrang_VanLaExactMatch()
    {
        var r = await _handler.Handle(
            new CheckCccdDuplicateQuery(Cccd, "  nguyễn thị   bích hạnh ", new DateOnly(1985, 3, 15), "FEMALE",
                "12 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM"),
            CancellationToken.None);

        r.Value!.Case.Should().Be(CccdDuplicateCase.ExactMatch);
        r.Value.FieldDiffs.Should().BeEmpty();
    }

    /// <summary>Thieu so CCCD -&gt; tra loi ro rang, KHONG nem exception.</summary>
    [DockerAvailableFact]
    public async Task ThieuSoCccd_TraLoiCccdRequired()
    {
        var r = await _handler.Handle(
            new CheckCccdDuplicateQuery("   ", null, null, null, null), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.ErrorCode.Should().Be("CCCD_REQUIRED");
    }

    /// <summary>Cach ly tenant: cung CCCD nhung tenant khac -&gt; khong duoc thay ho so.</summary>
    [DockerAvailableFact]
    public async Task CachLyTenant_TenantKhac_KhongThayHoSo()
    {
        var handlerTenant99 = new CheckCccdDuplicateQueryHandler(_db, new FixedTenantProvider(99));

        var r = await handlerTenant99.Handle(
            new CheckCccdDuplicateQuery(Cccd, "Nguyễn Thị Bích Hạnh", new DateOnly(1985, 3, 15), "FEMALE", null),
            CancellationToken.None);

        r.Value!.Case.Should().Be(CccdDuplicateCase.None);
    }

    private sealed class FixedTenantProvider : ITenantProvider
    {
        public FixedTenantProvider(int tenantId) => TenantId = tenantId;
        public int TenantId { get; private set; }
        public void SetTenantId(int tenantId) => TenantId = tenantId;
    }

    private sealed class AllBranchProvider : IBranchProvider
    {
        public int BranchId => 0;
        public bool IgnoreBranchFilter => true;
        public IReadOnlyList<int> AllowedBranchIds => Array.Empty<int>();
        public void SetContext(int branchId, bool ignoreFilter, IReadOnlyList<int> allowedBranchIds) { }
    }
}
