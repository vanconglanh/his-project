using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Behaviors;
using ProDiabHis.Application.Encounters;
using ProDiabHis.Application.Encounters.Addenda;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Clinical;
using ProDiabHis.Infrastructure.Persistence;
using Xunit;

namespace ProDiabHis.UnitTests.Encounters;

/// <summary>
/// [G03] Unit test quy tac KHOA BENH AN (Luat KCB 2023 / TT 32-2023):
/// encounter DONE/CANCELLED => du lieu lam sang READ-ONLY, chi sua qua ban dinh chinh co ly do.
/// </summary>
public class EncounterLockTests
{
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IDapperConnectionFactory _dapper;
    private readonly IPermissionChecker _permissions;
    private readonly FakeTenantProvider _tenant = new(1);

    public EncounterLockTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
        // Canh bao BHYT doc qua Dapper — mock tra null connection, guard nuot loi va bo qua canh bao.
        _dapper = Substitute.For<IDapperConnectionFactory>();
        _permissions = Substitute.For<IPermissionChecker>();
        _permissions.HasPermission(Arg.Any<string>()).Returns(true);
    }

    private EncounterLockGuard Guard(AppDbContext db) =>
        new(db, _tenant, _dapper, NullLogger<EncounterLockGuard>.Instance);

    private static async Task<Guid> SeedEncounterAsync(AppDbContext db, string status)
    {
        var id = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = id,
            TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            Status = status,
            ChiefComplaint = "Met moi",
            FinishedAt = status == EncounterStatus.Done ? DateTime.UtcNow : null,
            LockedAt   = status == EncounterStatus.Done ? DateTime.UtcNow : null
        });
        await db.SaveChangesAsync();
        return id;
    }

    // ──────────────────────────────────────────
    // Guard — ma tran khoa
    // ──────────────────────────────────────────
    [Theory]
    [InlineData(EncounterStatus.Waiting)]
    [InlineData(EncounterStatus.InProgress)]
    public async Task EnsureEditable_EncounterMo_ChoPhepSua(string status)
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, status);

        var result = await Guard(db).EnsureEditableAsync(id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(EncounterStatus.Done)]
    [InlineData(EncounterStatus.Cancelled)]
    public async Task EnsureEditable_EncounterDaKhoa_TraVeEncounterLocked(string status)
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, status);

        var result = await Guard(db).EnsureEditableAsync(id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_LOCKED");
        result.ErrorMessage.Should().Be("Bệnh án đã khoá — chỉ xem");
        result.ErrorDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureEditable_KhongTonTai_TraVeEncounterNotFound()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);

        var result = await Guard(db).EnsureEditableAsync(Guid.NewGuid(), CancellationToken.None);

        result.ErrorCode.Should().Be("ENCOUNTER_NOT_FOUND");
    }

    [Fact]
    public async Task EnsureEditable_KhacTenant_KhongDocDuoc()
    {
        using var db = TestDbContextFactory.Create(dbName: "lock-cross-tenant", tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.InProgress);

        using var otherTenantDb = TestDbContextFactory.Create(dbName: "lock-cross-tenant", tenantId: 99);
        var result = await Guard(otherTenantDb).EnsureEditableAsync(id, CancellationToken.None);

        result.ErrorCode.Should().Be("ENCOUNTER_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Pipeline behavior — chan command ghi len benh an da khoa
    // ──────────────────────────────────────────
    [Fact]
    public async Task Behavior_EncounterDaKhoa_ChanCommandVaKhongGoiHandler()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var behavior = new EncounterLockBehavior<AddDiagnosisCommand, Result<DiagnosisResponse>>(Guard(db));
        var handlerCalled = false;

        var response = await behavior.Handle(
            new AddDiagnosisCommand(id, new DiagnosisRequest("E11.9", DiagnosisType.Primary, null)),
            () => { handlerCalled = true; return Task.FromResult(Result<DiagnosisResponse>.Success(null!)); },
            CancellationToken.None);

        handlerCalled.Should().BeFalse();
        response.IsSuccess.Should().BeFalse();
        response.ErrorCode.Should().Be("ENCOUNTER_LOCKED");
    }

    [Fact]
    public async Task Behavior_EncounterDangKham_ChoHandlerChay()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.InProgress);

        var behavior = new EncounterLockBehavior<AddDiagnosisCommand, Result<DiagnosisResponse>>(Guard(db));
        var handlerCalled = false;

        var response = await behavior.Handle(
            new AddDiagnosisCommand(id, new DiagnosisRequest("E11.9", DiagnosisType.Primary, null)),
            () => { handlerCalled = true; return Task.FromResult(Result<DiagnosisResponse>.Success(null!)); },
            CancellationToken.None);

        handlerCalled.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Behavior_CommandTaoDinhChinh_KhongBiChan()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var behavior = new EncounterLockBehavior<CreateEncounterAddendumCommand, Result<EncounterAddendumResponse>>(Guard(db));
        var handlerCalled = false;

        await behavior.Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                AddendumSection.Diagnosis, null, null, null, null, "Ly do dinh chinh hop le")),
            () => { handlerCalled = true; return Task.FromResult(Result<EncounterAddendumResponse>.Success(null!)); },
            CancellationToken.None);

        handlerCalled.Should().BeTrue();
    }

    // ──────────────────────────────────────────
    // Addendum — validate ly do dinh chinh
    // ──────────────────────────────────────────
    private CreateEncounterAddendumCommandHandler AddendumHandler(AppDbContext db) =>
        new(db, _tenant, _user, _audit, Guard(db), _permissions);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task TaoDinhChinh_ThieuLyDo_TraVeAmendmentReasonRequired(string? reason)
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var result = await AddendumHandler(db).Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                AddendumSection.ClinicalNote, AddendumOperation.Add, null, null,
                new { note = "Bo sung" }, reason!)),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("AMENDMENT_REASON_REQUIRED");
        result.ErrorMessage.Should().Be("Phải nhập lý do đính chính");
        (await db.EncounterAddenda.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TaoDinhChinh_ThieuQuyenAmend_TraVeForbidden()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var noPerm = Substitute.For<IPermissionChecker>();
        noPerm.HasPermission(Arg.Any<string>()).Returns(false);
        var handler = new CreateEncounterAddendumCommandHandler(db, _tenant, _user, _audit, Guard(db), noPerm);

        var result = await handler.Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                AddendumSection.ClinicalNote, AddendumOperation.Add, null, null,
                new { note = "x" }, "Ly do dinh chinh day du")),
            CancellationToken.None);

        result.ErrorCode.Should().Be("FORBIDDEN");
        (await db.EncounterAddenda.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task TaoDinhChinh_BenhAnChuaKhoa_TraVeAddendumNotApplicable()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.InProgress);

        var result = await AddendumHandler(db).Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                AddendumSection.ClinicalNote, AddendumOperation.Add, null, null,
                new { note = "x" }, "Ly do dinh chinh day du")),
            CancellationToken.None);

        result.ErrorCode.Should().Be("ADDENDUM_NOT_APPLICABLE");
    }

    [Fact]
    public async Task TaoDinhChinh_SectionKhongHopLe_TraVeInvalidSection()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var result = await AddendumHandler(db).Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                "KHONG_TON_TAI", AddendumOperation.Add, null, null, new { }, "Ly do dinh chinh day du")),
            CancellationToken.None);

        result.ErrorCode.Should().Be("ADDENDUM_INVALID_SECTION");
    }

    [Fact]
    public async Task TaoDinhChinh_TargetKhongThuocEncounter_TraVeTargetNotFound()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = await SeedEncounterAsync(db, EncounterStatus.Done);

        var result = await AddendumHandler(db).Handle(
            new CreateEncounterAddendumCommand(id, new CreateAddendumRequest(
                AddendumSection.Diagnosis, AddendumOperation.Update,
                "diab_his_enc_diagnoses", Guid.NewGuid().ToString(),
                new { icd10Code = "E11.9" }, "Chan doan chinh ghi nham, dinh chinh theo ket qua CLS")),
            CancellationToken.None);

        result.ErrorCode.Should().Be("ADDENDUM_TARGET_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Addendum — happy path: khong ghi de ban goc + co vet audit
    // ──────────────────────────────────────────
    [Fact]
    public async Task TaoDinhChinh_HopLe_GhiBanDinhChinhVaGiuNguyenBanGoc()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = await SeedEncounterAsync(db, EncounterStatus.Done);

        var diagId = Guid.NewGuid();
        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            Id = diagId,
            TenantId = 1,
            EncounterId = encId.ToString(),
            Icd10Code = "E10.9",
            Name = "DTD typ 1",
            Type = DiagnosisType.Primary
        });
        await db.SaveChangesAsync();

        var result = await AddendumHandler(db).Handle(
            new CreateEncounterAddendumCommand(encId, new CreateAddendumRequest(
                AddendumSection.Diagnosis, AddendumOperation.Update,
                "diab_his_enc_diagnoses", diagId.ToString(),
                new { icd10Code = "E11.9", name = "DTD typ 2" },
                "Chan doan chinh ghi nham E10.9, dinh chinh theo ket qua C-peptide")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var addendum = await db.EncounterAddenda.AsNoTracking().SingleAsync();
        addendum.TenantId.Should().Be(1);
        addendum.EncounterId.Should().Be(encId.ToString());
        addendum.Reason.Should().Contain("dinh chinh");
        addendum.ContentBefore.Should().Contain("E10.9");   // snapshot ban goc do SERVER chup
        addendum.ContentAfter.Should().Contain("E11.9");

        // Ban goc KHONG bi ghi de (bat bien theo TT 32/2023)
        var original = await db.EncounterDiagnoses.AsNoTracking().SingleAsync(d => d.Id == diagId);
        original.Icd10Code.Should().Be("E10.9");

        // amendment_count tang
        var enc = await db.Encounters.AsNoTracking().SingleAsync(e => e.Id == encId);
        enc.AmendmentCount.Should().Be(1);

        // Audit AMEND co vet truoc/sau
        await _audit.Received(1).LogAsync(
            "AMEND", "Encounter", encId.ToString(),
            AuditSeverity.WARN, Arg.Any<bool>(), Arg.Any<string?>(),
            Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DongCaKham_SetLockedAtVaLockedBy()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1, PatientId = Guid.NewGuid().ToString(),
            Status = EncounterStatus.InProgress, ChiefComplaint = "Met moi"
        });
        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            Id = Guid.NewGuid(), TenantId = 1, EncounterId = encId.ToString(),
            Icd10Code = "E11.9", Name = "DTD typ 2", Type = DiagnosisType.Primary
        });
        db.EmrContents.Add(new EmrContent
        {
            Id = Guid.NewGuid(), TenantId = 1, EncounterId = encId.ToString(),
            ContentJson = "{}", SignedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new CloseEncounterCommandHandler(db, _user, _audit, _dapper);
        var result = await handler.Handle(new CloseEncounterCommand(encId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var enc = await db.Encounters.AsNoTracking().SingleAsync(e => e.Id == encId);
        enc.Status.Should().Be(EncounterStatus.Done);
        enc.LockedAt.Should().NotBeNull();
        enc.LockedBy.Should().Be(_user.UserId);
    }
}
