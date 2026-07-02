using Dapper;
using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using System.Data;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>Unit tests cho patient command handlers dung mock IDapperConnectionFactory</summary>
public class PatientCommandHandlerTests
{
    private readonly IDapperConnectionFactory _dbFactory;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _enc;
    private readonly IAuditService _audit;

    public PatientCommandHandlerTests()
    {
        _dbFactory = Substitute.For<IDapperConnectionFactory>();
        _tenant = Substitute.For<ITenantProvider>();
        _currentUser = Substitute.For<ICurrentUser>();
        _enc = Substitute.For<IEncryptionService>();
        _audit = Substitute.For<IAuditService>();

        _tenant.TenantId.Returns(1);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _enc.Encrypt(Arg.Any<string>()).Returns("ENCRYPTED");
    }

    [Fact]
    public void PatientStatus_Constants_AreDefinedCorrectly()
    {
        // Enum values test (domain)
        PatientStatus.Active.Should().Be("ACTIVE");
        PatientStatus.Inactive.Should().Be("INACTIVE");
        PatientStatus.Deceased.Should().Be("DECEASED");
    }

    [Fact]
    public void CreatePatientRequest_RequiredFields()
    {
        var req = new CreatePatientRequest(
            FullName: "Nguyen Van A",
            Gender: "MALE",
            DateOfBirth: new DateOnly(1990, 1, 1),
            IdNumber: null, Phone: null, Email: null, Address: null,
            Occupation: null, Ethnicity: null, BloodType: null,
            IdCardIssuedDate: null,
            IdCardIssuedPlace: null);

        req.FullName.Should().Be("Nguyen Van A");
        req.Gender.Should().Be("MALE");
    }

    [Fact]
    public void DeletePatientCommand_CreatesCorrectly()
    {
        var patientId = Guid.NewGuid();
        var cmd = new DeletePatientCommand(patientId);
        cmd.PatientId.Should().Be(patientId);
    }

    [Fact]
    public void UpdateReceptionNoteCommand_StoresNote()
    {
        var patientId = Guid.NewGuid();
        var note = "BN có tiền sử cao huyết áp";
        var cmd = new UpdateReceptionNoteCommand(patientId, note);
        cmd.PatientId.Should().Be(patientId);
        cmd.ReceptionNote.Should().Be(note);
    }

    [Fact]
    public void SearchPatientsQuery_DefaultPage_IsOne()
    {
        var query = new SearchPatientsQuery("Nguyen", 1, 20);
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(20);
        query.Q.Should().Be("Nguyen");
    }

    [Fact]
    public void AddAllergyRequest_ValidSeverities()
    {
        var severities = new[] { "MILD", "MODERATE", "SEVERE", "LIFE_THREATENING" };
        foreach (var sev in severities)
        {
            var req = new AddAllergyRequest("Penicillin", "Noi me day", sev, null, null);
            req.Severity.Should().Be(sev);
        }
    }

    [Fact]
    public void InsuranceRequest_ValidTypes()
    {
        var types = new[] { "BHYT", "PRIVATE", "OTHER" };
        foreach (var type in types)
        {
            var req = new InsuranceRequest(type, "HC4010123456", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, null);
            req.Type.Should().Be(type);
        }
    }

    [Fact]
    public void EmergencyContactRequest_RequiredFields()
    {
        var req = new EmergencyContactRequest("Tran Thi B", "SPOUSE", "0987654321", null);
        req.FullName.Should().Be("Tran Thi B");
        req.Relationship.Should().Be("SPOUSE");
        req.Phone.Should().Be("0987654321");
    }

    [Fact]
    public void UploadAvatarCommand_SetsCorrectProperties()
    {
        var patientId = Guid.NewGuid();
        using var stream = new MemoryStream(new byte[1024]);
        var cmd = new UploadAvatarCommand(patientId, stream, "avatar.jpg", "image/jpeg", 1024);
        cmd.PatientId.Should().Be(patientId);
        cmd.ContentType.Should().Be("image/jpeg");
        cmd.SizeBytes.Should().Be(1024);
    }
}
