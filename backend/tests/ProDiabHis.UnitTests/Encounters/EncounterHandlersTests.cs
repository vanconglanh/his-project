using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;
using ProDiabHis.Application.Encounters;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Encounters;

/// <summary>Unit tests cho Encounter EF Core handlers</summary>
public class EncounterHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IDapperConnectionFactory _dapper;
    private readonly IBranchProvider _branch;
    private readonly IPackageEntitlementService _packageEntitlement;
    private readonly ILogger<StartEncounterCommandHandler> _startLogger;

    public EncounterHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
        // Dong bo hang doi tiep don la side-effect non-fatal; tra icd10 tu dict/ref cung qua Dapper.
        // PHAI dung DbConnection that (khong phai mock IDbConnection) de Dapper chay duoc nhanh async
        // that (QueryFirstOrDefaultAsync/ExecuteAsync) — xem FakeEmptyDapperConnectionFactory.
        _dapper = new FakeEmptyDapperConnectionFactory();
        _branch = Substitute.For<IBranchProvider>();
        _packageEntitlement = Substitute.For<IPackageEntitlementService>();
        _startLogger = Substitute.For<ILogger<StartEncounterCommandHandler>>();
    }

    // ──────────────────────────────────────────
    // Create encounter — happy path
    // ──────────────────────────────────────────
    [Fact]
    public async Task CreateEncounter_ValidPatient_CreatesSuccessfully()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patientId = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = patientId, TenantId = 1, Code = "BNT01000001", FullName = "Nguyen Van A" });
        await db.SaveChangesAsync();

        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit, _dapper);
        var req = new CreateEncounterRequest(
            PatientId: patientId.ToString(),
            RoomId: null, DoctorId: null,
            EncounterType: "FIRST_VISIT",
            ReasonForVisit: "Kham dinh ky",
            ChiefComplaint: null);

        var result = await handler.Handle(new CreateEncounterCommand(req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PatientId.Should().Be(patientId.ToString());
        result.Value.Status.Should().Be("WAITING");
        result.Value.EncounterType.Should().Be("FIRST_VISIT");

        var enc = await db.Encounters.AsNoTracking().FirstOrDefaultAsync();
        enc.Should().NotBeNull();
        enc!.TenantId.Should().Be(1);
    }

    // ──────────────────────────────────────────
    // Create encounter — patient not found
    // ──────────────────────────────────────────
    [Fact]
    public async Task CreateEncounter_PatientNotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit, _dapper);
        var req = new CreateEncounterRequest(
            PatientId: Guid.NewGuid().ToString(),
            RoomId: null, DoctorId: null,
            EncounterType: "FIRST_VISIT",
            ReasonForVisit: "Test", ChiefComplaint: null);

        var result = await handler.Handle(new CreateEncounterCommand(req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Create encounter — cross-tenant patient blocked
    // ──────────────────────────────────────────
    [Fact]
    public async Task CreateEncounter_CrossTenantPatient_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        // Tenant 2 patient
        var otherPatientId = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = otherPatientId, TenantId = 2, Code = "BNT02000001", FullName = "Other" });
        await db.SaveChangesAsync();

        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit, _dapper);
        var req = new CreateEncounterRequest(
            PatientId: otherPatientId.ToString(),
            RoomId: null, DoctorId: null,
            EncounterType: "FIRST_VISIT",
            ReasonForVisit: "Test", ChiefComplaint: null);

        var result = await handler.Handle(new CreateEncounterCommand(req), CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Add diagnosis
    // ──────────────────────────────────────────
    [Fact]
    public async Task AddDiagnosis_ValidEncounter_CreatesDiagnosis()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            EncounterType = "FIRST_VISIT", Status = "IN_PROGRESS"
        });
        await db.SaveChangesAsync();

        var handler = new AddDiagnosisCommandHandler(db, _tenant, _user, _audit, _dapper);
        var req = new DiagnosisRequest("E11", "PRIMARY", null);
        var result = await handler.Handle(new AddDiagnosisCommand(encId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Icd10Code.Should().Be("E11");
        result.Value.Type.Should().Be("PRIMARY");

        var diagInDb = await db.EncounterDiagnoses.AsNoTracking().FirstOrDefaultAsync();
        diagInDb.Should().NotBeNull();
        diagInDb!.EncounterId.Should().Be(encId.ToString());
    }

    [Fact]
    public async Task AddDiagnosis_EncounterNotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new AddDiagnosisCommandHandler(db, _tenant, _user, _audit, _dapper);
        var req = new DiagnosisRequest("E11", "PRIMARY", null);
        var result = await handler.Handle(new AddDiagnosisCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Status transition: WAITING -> IN_PROGRESS
    // ──────────────────────────────────────────
    [Fact]
    public async Task StartEncounter_TransitionsToInProgress()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            EncounterType = "FIRST_VISIT", Status = "WAITING"
        });
        await db.SaveChangesAsync();

        var handler = new StartEncounterCommandHandler(db, _tenant, _user, _audit, _dapper, _branch, _packageEntitlement, _startLogger);
        var result = await handler.Handle(new StartEncounterCommand(encId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("IN_PROGRESS");

        var enc = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encId);
        enc!.Status.Should().Be("IN_PROGRESS");
        enc.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartEncounter_AlreadyDone_InvalidTransition()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            EncounterType = "FIRST_VISIT", Status = "DONE"
        });
        await db.SaveChangesAsync();

        var handler = new StartEncounterCommandHandler(db, _tenant, _user, _audit, _dapper, _branch, _packageEntitlement, _startLogger);
        var result = await handler.Handle(new StartEncounterCommand(encId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_INVALID_TRANSITION");
    }

    // ──────────────────────────────────────────
    // Update chief complaint
    // ──────────────────────────────────────────
    [Fact]
    public async Task UpdateChiefComplaint_UpdatesSuccessfully()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            EncounterType = "FIRST_VISIT", Status = "IN_PROGRESS"
        });
        await db.SaveChangesAsync();

        var handler = new UpdateChiefComplaintCommandHandler(db, _user);
        var result = await handler.Handle(new UpdateChiefComplaintCommand(encId, "Dau dau, sot cao"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var enc = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encId);
        enc!.ChiefComplaint.Should().Be("Dau dau, sot cao");
    }

    // ──────────────────────────────────────────
    // List encounters — paging + filter
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListEncounters_FilterByStatus_ReturnsFiltered()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patientId = Guid.NewGuid().ToString();
        db.Encounters.Add(new Encounter { TenantId = 1, PatientId = patientId, EncounterType = "FIRST_VISIT", Status = "WAITING" });
        db.Encounters.Add(new Encounter { TenantId = 1, PatientId = patientId, EncounterType = "FIRST_VISIT", Status = "DONE" });
        db.Encounters.Add(new Encounter { TenantId = 1, PatientId = patientId, EncounterType = "FIRST_VISIT", Status = "DONE" });
        await db.SaveChangesAsync();

        var handler = new ListEncountersQueryHandler(db, _tenant, _user, _audit, _dapper);
        var query = new ListEncountersQuery(null, null, null, "DONE", null, null, null, 1, 20);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(e => e.Status.Should().Be("DONE"));
    }

    // BM-07 (tiep, Lark 2026-09-16): DateFrom/DateTo la ngay lich VN (FE gui theo gio dia
    // phuong khi bam "Dang kham hom nay") nhung CreatedAt luu UTC. Benh nhan check-in luc
    // 21h VN (= 14h UTC cung ngay) van phai duoc tinh la "hom nay" theo VN. Truoc fix,
    // so sanh truc tiep se dung dung boundary UTC nen van PASS o gio nay trong ngay -
    // case that su bi loi la sau 17h00 UTC (tuc sau 00h00 VN hom sau): benh nhan check-in
    // luc 23h VN (=16h UTC cung ngay) van la "hom nay" theo VN nhung neu server so sanh
    // CreatedAt (UTC) voi bien [DateFrom 00:00, DateTo 23:59:59] theo UTC thi van dung boundary
    // sai vi CreatedAt (16h UTC) < DateTo 23:59:59 UTC nen van qua - that ra bug chi lo khi
    // check-in SAU 17h00 UTC (tuc sau 00h00 VN NGAY HOM SAU): CreatedAt roi sang ngay UTC ke
    // tiep trong khi FE (VN) van con dang gui date_from/date_to la ngay HOM QUA (VN). Mo phong
    // dung case nay: CreatedAt = 2026-09-19 17:30:00 UTC (= 2026-09-20 00:30 VN, tuc DA SANG
    // ngay 20 theo VN) nhung FE (dang o VN 2026-09-20 sau 00h) se gui date_from=date_to=
    // 2026-09-20. Truoc fix: so sanh CreatedAt >= 2026-09-20 00:00 (UTC) se FAIL vi CreatedAt
    // (2026-09-19 17:30 UTC) < 2026-09-20 00:00 UTC -> bi loc mat oan du dung la "hom nay" theo
    // VN. Sau fix (tru VnOffset +7h khoi bien truoc khi so sanh): from = 2026-09-20 00:00 VN -
    // 7h = 2026-09-19 17:00 UTC -> CreatedAt (17:30 UTC) >= 17:00 UTC -> PASS dung.
    [Fact]
    public async Task ListEncounters_FilterTheoNgayVn_KhongLocMatEncounterSauNuaDemUtc()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patientId = Guid.NewGuid().ToString();
        // "Hom nay" theo VN la 2026-09-20, nhung check-in luc 00:30 VN (= 17:30 UTC ngay 19)
        // nen CreatedAt (UTC) van con ghi ngay 19.
        var createdAtUtc = new DateTime(2026, 9, 19, 17, 30, 0, DateTimeKind.Utc);
        db.Encounters.Add(new Encounter
        {
            TenantId = 1, PatientId = patientId, EncounterType = "FIRST_VISIT",
            Status = "IN_PROGRESS", CreatedAt = createdAtUtc,
        });
        // Doi chung: benh nhan kham "hom qua" that su (VN) - phai bi loai.
        db.Encounters.Add(new Encounter
        {
            TenantId = 1, PatientId = patientId, EncounterType = "FIRST_VISIT",
            Status = "IN_PROGRESS", CreatedAt = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc),
        });
        // Dung SaveChanges() (KHONG phai SaveChangesAsync) - AppDbContext.SaveChangesAsync co
        // override tu dong ghi de CreatedAt = DateTime.UtcNow cho entity moi (IAuditTimestamps),
        // se lam mat gia tri CreatedAt gia lap o tren. SaveChanges() dong bo khong bi override nay.
        db.SaveChanges();

        var handler = new ListEncountersQueryHandler(db, _tenant, _user, _audit, _dapper);
        var today = new DateOnly(2026, 9, 20);
        var query = new ListEncountersQuery(null, null, null, null, null, today, today, 1, 20);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1, "encounter check-in 00:30 VN ngay 20/09 (du la 17:30 UTC ngay 19/09) van phai tinh la 'hom nay' theo VN");
    }

    // ──────────────────────────────────────────
    // Close encounter — missing chief complaint
    // ──────────────────────────────────────────
    [Fact]
    public async Task CloseEncounter_MissingChiefComplaint_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var encId = Guid.NewGuid();
        db.Encounters.Add(new Encounter
        {
            Id = encId, TenantId = 1,
            PatientId = Guid.NewGuid().ToString(),
            EncounterType = "FIRST_VISIT", Status = "IN_PROGRESS",
            ChiefComplaint = null
        });
        await db.SaveChangesAsync();

        var handler = new CloseEncounterCommandHandler(db, _user, _audit, _dapper);
        var result = await handler.Handle(new CloseEncounterCommand(encId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ENCOUNTER_MISSING_CHIEF_COMPLAINT");
    }
}
