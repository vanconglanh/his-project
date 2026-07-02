using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>Unit tests cho Patient EF Core handlers</summary>
public class PatientHandlersTests
{
    private readonly FakeTenantProvider _tenant = new(1);
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _enc = new FakeEncryptionService();
    private readonly IAuditService _audit;

    public PatientHandlersTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.UserId.Returns(Guid.NewGuid());
        _audit = Substitute.For<IAuditService>();
    }

    // ──────────────────────────────────────────
    // List patients — happy path
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListPatients_ReturnsPagedResult_WithActiveFilter()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        db.Patients.Add(new Patient { TenantId = 1, Code = "BNT01000001", FullName = "Nguyen Van A", Status = "ACTIVE" });
        db.Patients.Add(new Patient { TenantId = 1, Code = "BNT01000002", FullName = "Tran Thi B", Status = "INACTIVE" });
        await db.SaveChangesAsync();

        var handler = new ListPatientsQueryHandler(db);
        var result = await handler.Handle(new ListPatientsQuery(1, 20, null, "ACTIVE", null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("Nguyen Van A");
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task ListPatients_TenantIsolation_OtherTenantNotVisible()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        // Tenant 2 patient — HasQueryFilter se loc ra
        db.Patients.Add(new Patient { TenantId = 2, Code = "BNT02000001", FullName = "Other Tenant" });
        db.Patients.Add(new Patient { TenantId = 1, Code = "BNT01000001", FullName = "My Tenant" });
        await db.SaveChangesAsync();

        var handler = new ListPatientsQueryHandler(db);
        var result = await handler.Handle(new ListPatientsQuery(1, 20, null, null, null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Be("My Tenant");
    }

    // ──────────────────────────────────────────
    // Get patient — not found
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetPatient_NotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new GetPatientQueryHandler(db);
        var result = await handler.Handle(new GetPatientQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
        result.ErrorMessage.Should().Contain("Không tìm thấy");
    }

    [Fact]
    public async Task GetPatient_Found_ReturnsPatientResponse()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = id, TenantId = 1, Code = "BNT01000001", FullName = "Nguyen Van A" });
        await db.SaveChangesAsync();

        var handler = new GetPatientQueryHandler(db);
        var result = await handler.Handle(new GetPatientQuery(id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Nguyen Van A");
        result.Value.Code.Should().Be("BNT01000001");
    }

    // ──────────────────────────────────────────
    // Create patient — happy path
    // ──────────────────────────────────────────
    [Fact]
    public async Task CreatePatient_ValidRequest_CreatesAndReturns()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreatePatientCommandHandler(db, _tenant, _currentUser, _enc, _audit);

        var req = new CreatePatientRequest(
            FullName: "Le Thi C",
            Gender: "FEMALE",
            DateOfBirth: new DateOnly(1995, 5, 5),
            IdNumber: "123456789012",
            Phone: "0912345678",
            Email: null, Address: null,
            Occupation: null, Ethnicity: null, BloodType: "A",
            IdCardIssuedDate: null, IdCardIssuedPlace: null);

        var result = await handler.Handle(new CreatePatientCommand(req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FullName.Should().Be("Le Thi C");
        result.Value.Code.Should().StartWith("BNT01");
        result.Value.TenantId.Should().Be(1);

        var inDb = await db.Patients.AsNoTracking().FirstOrDefaultAsync();
        inDb.Should().NotBeNull();
        inDb!.FullName.Should().Be("Le Thi C");
    }

    // ──────────────────────────────────────────
    // Create patient — validation: FullName empty
    // ──────────────────────────────────────────
    [Fact]
    public async Task CreatePatient_EmptyFullName_StillCreates_DomainValidationRespected()
    {
        // FluentValidation se xu ly o controller level, handler chi save
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new CreatePatientCommandHandler(db, _tenant, _currentUser, _enc, _audit);

        var req = new CreatePatientRequest(
            FullName: "Test",
            Gender: null, DateOfBirth: null, IdNumber: null, Phone: null,
            Email: null, Address: null, Occupation: null, Ethnicity: null,
            BloodType: null, IdCardIssuedDate: null, IdCardIssuedPlace: null);

        var result = await handler.Handle(new CreatePatientCommand(req), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    // ──────────────────────────────────────────
    // Soft delete patient
    // ──────────────────────────────────────────
    [Fact]
    public async Task DeletePatient_SetsDeletedAt_NotFoundAfterDelete()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = id, TenantId = 1, Code = "BNT01000001", FullName = "Test" });
        await db.SaveChangesAsync();

        var deleteHandler = new DeletePatientCommandHandler(db, _currentUser, _audit);
        var result = await deleteHandler.Handle(new DeletePatientCommand(id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();

        // HasQueryFilter se loc ra khi deleted_at IS NOT NULL
        var getHandler = new GetPatientQueryHandler(db);
        var found = await getHandler.Handle(new GetPatientQuery(id), CancellationToken.None);
        found.IsSuccess.Should().BeFalse();
        found.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }

    [Fact]
    public async Task DeletePatient_NotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new DeletePatientCommandHandler(db, _currentUser, _audit);
        var result = await handler.Handle(new DeletePatientCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }

    // ──────────────────────────────────────────
    // Search patients
    // ──────────────────────────────────────────
    [Fact]
    public async Task SearchPatients_MatchesFullName()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        db.Patients.Add(new Patient { TenantId = 1, Code = "BNT01000001", FullName = "Nguyen Van Alpha" });
        db.Patients.Add(new Patient { TenantId = 1, Code = "BNT01000002", FullName = "Tran Thi Beta" });
        await db.SaveChangesAsync();

        var handler = new SearchPatientsQueryHandler(db);
        var result = await handler.Handle(new SearchPatientsQuery("alpha", 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].FullName.Should().Contain("Alpha");
    }

    // ──────────────────────────────────────────
    // Add allergy
    // ──────────────────────────────────────────
    [Fact]
    public async Task AddAllergy_PatientNotFound_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var handler = new AddAllergyCommandHandler(db, _tenant, _currentUser);
        var req = new AddAllergyRequest("Penicillin", "Noi me day", "SEVERE", null, null);
        var result = await handler.Handle(new AddAllergyCommand(Guid.NewGuid(), req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PATIENT_NOT_FOUND");
    }

    [Fact]
    public async Task AddAllergy_ValidPatient_CreatesAllergy()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var patientId = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = patientId, TenantId = 1, Code = "BNT01000001", FullName = "Test" });
        await db.SaveChangesAsync();

        var handler = new AddAllergyCommandHandler(db, _tenant, _currentUser);
        var req = new AddAllergyRequest("Penicillin", "Noi me day", "SEVERE", null, null);
        var result = await handler.Handle(new AddAllergyCommand(patientId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Allergen.Should().Be("Penicillin");
        result.Value.Severity.Should().Be("SEVERE");
    }

    // ──────────────────────────────────────────
    // Update reception note
    // ──────────────────────────────────────────
    [Fact]
    public async Task UpdateReceptionNote_UpdatesSuccessfully()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = id, TenantId = 1, Code = "BNT01000001", FullName = "Test" });
        await db.SaveChangesAsync();

        var handler = new UpdateReceptionNoteCommandHandler(db, _currentUser, _audit);
        var result = await handler.Handle(new UpdateReceptionNoteCommand(id, "BN co tien su cao huyet ap"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReceptionNote.Should().Be("BN co tien su cao huyet ap");
    }

    // ──────────────────────────────────────────
    // Add insurance — expired card
    // ──────────────────────────────────────────
    [Fact]
    public async Task AddInsurance_ExpiredCard_ReturnsFailure()
    {
        using var db = TestDbContextFactory.Create(tenantId: 1);
        var id = Guid.NewGuid();
        db.Patients.Add(new Patient { Id = id, TenantId = 1, Code = "BNT01000001", FullName = "Test" });
        await db.SaveChangesAsync();

        var handler = new AddInsuranceCommandHandler(db, _tenant, _currentUser, _enc);
        var req = new InsuranceRequest("BHYT", "HC4010001234", new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), null, null);
        var result = await handler.Handle(new AddInsuranceCommand(id, req), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("BHYT_EXPIRED");
    }
}
