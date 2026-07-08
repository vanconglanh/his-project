using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
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

    public EncounterHandlersTests()
    {
        _user = Substitute.For<ICurrentUser>();
        _user.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
        // Dong bo hang doi tiep don la side-effect non-fatal -> mock rong, loi bi nuot trong QueueTicketSync.
        _dapper = Substitute.For<IDapperConnectionFactory>();
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

        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit);
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
        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit);
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

        var handler = new CreateEncounterCommandHandler(db, _tenant, _user, _audit);
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

        var handler = new AddDiagnosisCommandHandler(db, _tenant, _user, _audit);
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
        var handler = new AddDiagnosisCommandHandler(db, _tenant, _user, _audit);
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

        var handler = new StartEncounterCommandHandler(db, _tenant, _user, _audit, _dapper);
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

        var handler = new StartEncounterCommandHandler(db, _tenant, _user, _audit, _dapper);
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

        var handler = new ListEncountersQueryHandler(db, _tenant, _user, _audit);
        var query = new ListEncountersQuery(null, null, null, "DONE", null, null, null, 1, 20);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().AllSatisfy(e => e.Status.Should().Be("DONE"));
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
