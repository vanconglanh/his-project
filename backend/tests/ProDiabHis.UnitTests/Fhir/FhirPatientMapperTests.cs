using DomainPatient = ProDiabHis.Domain.Entities.Patient;
using FluentAssertions;
using Hl7.Fhir.Model;
using ProDiabHis.Application.Fhir.Mappers;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Fhir;

public class FhirPatientMapperTests
{
    private readonly PatientMapper _mapper = new();

    [Fact]
    public void Map_ActiveMalePatient_ReturnsCorrectFhirShape()
    {
        var patient = new DomainPatient
        {
            Id = Guid.NewGuid(),
            Code = "PAT-001",
            FullName = "Nguyen Van A",
            Gender = Gender.Male,
            DateOfBirth = new DateOnly(1985, 3, 15),
            Phone = "0901234567",
            Email = "a@example.com",
            Street = "123 Nguyen Hue",
            ProvinceCode = "79",
            Status = PatientStatus.Active,
            TenantId = 1
        };

        var fhir = _mapper.Map(patient);

        fhir.Should().NotBeNull();
        fhir.Id.Should().Be(patient.Id.ToString());
        fhir.Active.Should().BeTrue();
        fhir.Gender.Should().Be(AdministrativeGender.Male);
        fhir.BirthDate.Should().Be("1985-03-15");
        fhir.Name.Should().HaveCount(1);
        fhir.Name[0].Text.Should().Be("Nguyen Van A");
        fhir.Telecom.Should().HaveCountGreaterOrEqualTo(2);
        fhir.Telecom.Should().Contain(t => t.System == ContactPoint.ContactPointSystem.Phone && t.Value == "0901234567");
        fhir.Telecom.Should().Contain(t => t.System == ContactPoint.ContactPointSystem.Email && t.Value == "a@example.com");
        fhir.Address.Should().HaveCount(1);
        fhir.Address[0].Country.Should().Be("VN");
        fhir.Identifier.Should().Contain(i => i.Value == "PAT-001");
        fhir.Meta!.Profile.Should().Contain("http://hl7.org/fhir/StructureDefinition/Patient");
    }

    [Fact]
    public void Map_FemalePatient_GenderMappedCorrectly()
    {
        var patient = new DomainPatient
        {
            Id = Guid.NewGuid(),
            Code = "PAT-002",
            FullName = "Tran Thi B",
            Gender = Gender.Female,
            Status = PatientStatus.Active,
            TenantId = 1
        };

        var fhir = _mapper.Map(patient);

        fhir.Gender.Should().Be(AdministrativeGender.Female);
    }

    [Fact]
    public void Map_DeceasedPatient_ActiveFalse()
    {
        var patient = new DomainPatient
        {
            Id = Guid.NewGuid(),
            Code = "PAT-003",
            FullName = "Le Van C",
            Status = PatientStatus.Deceased,
            TenantId = 1
        };

        var fhir = _mapper.Map(patient);

        fhir.Active.Should().BeFalse();
    }

    [Fact]
    public void Map_SoftDeletedPatient_ActiveFalse()
    {
        var patient = new DomainPatient
        {
            Id = Guid.NewGuid(),
            Code = "PAT-004",
            FullName = "Pham Thi D",
            Status = PatientStatus.Active,
            DeletedAt = DateTime.UtcNow,
            TenantId = 1
        };

        var fhir = _mapper.Map(patient);

        fhir.Active.Should().BeFalse();
    }

    [Fact]
    public void Map_PatientWithNoBirthDate_BirthDateNull()
    {
        var patient = new DomainPatient
        {
            Id = Guid.NewGuid(),
            Code = "PAT-005",
            FullName = "Unknown",
            DateOfBirth = null,
            Status = PatientStatus.Active,
            TenantId = 1
        };

        var fhir = _mapper.Map(patient);

        fhir.BirthDate.Should().BeNull();
    }
}
