using DomainPatient = ProDiabHis.Domain.Entities.Patient;
using DomainEncounter = ProDiabHis.Domain.Entities.Encounter;
using DomainVitalSigns = ProDiabHis.Domain.Entities.VitalSigns;
using FluentAssertions;
using Hl7.Fhir.Model;
using ProDiabHis.Application.Fhir;
using ProDiabHis.Application.Fhir.Mappers;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Fhir;

public class FhirBundleServiceTests
{
    private static FhirBundleService CreateService() => new(
        new PatientMapper(),
        new EncounterMapper(),
        new ConditionMapper(),
        new ObservationMapper(),
        new MedicationRequestMapper(),
        new ProcedureMapper(),
        new AllergyIntoleranceMapper(),
        new DiagnosticReportMapper());

    private static DomainPatient MakePatient() => new DomainPatient
    {
        Id = Guid.NewGuid(),
        Code = "PAT-001",
        FullName = "Test Patient",
        Gender = Gender.Male,
        Status = PatientStatus.Active,
        TenantId = 1
    };

    private static DomainEncounter MakeEncounter(Guid patientId) => new DomainEncounter
    {
        Id = Guid.NewGuid(),
        PatientId = patientId.ToString(),
        Status = EncounterStatus.Done,
        EncounterType = EncounterTypes.FirstVisit,
        TenantId = 1
    };

    [Fact]
    public void BuildPatientBundle_PatientOnly_HasOneEntry()
    {
        var service = CreateService();
        var patient = MakePatient();

        var bundle = service.BuildPatientBundle(patient);

        bundle.Type.Should().Be(Bundle.BundleType.Searchset);
        bundle.Entry.Should().HaveCount(1);
        bundle.Total.Should().Be(1);
        bundle.Entry[0].Resource.Should().BeOfType<Hl7.Fhir.Model.Patient>();
    }

    [Fact]
    public void BuildPatientBundle_WithEncounters_HasCorrectCount()
    {
        var service = CreateService();
        var patient = MakePatient();
        var encounters = new[] { MakeEncounter(patient.Id), MakeEncounter(patient.Id) };

        var bundle = service.BuildPatientBundle(patient, encounters);

        bundle.Entry.Should().HaveCount(3); // 1 patient + 2 encounters
        bundle.Total.Should().Be(3);
    }

    [Fact]
    public void BuildEncounterBundle_WithDiagnosis_ContainsCondition()
    {
        var service = CreateService();
        var patient = MakePatient();
        var encounter = MakeEncounter(patient.Id);
        var diagnosis = new EncounterDiagnosis
        {
            Id = Guid.NewGuid(),
            EncounterId = encounter.Id.ToString(),
            Icd10Code = "E11",
            Name = "Dai thao duong type 2",
            Type = DiagnosisType.Primary,
            TenantId = 1
        };

        var bundle = service.BuildEncounterBundle(
            patient, encounter,
            diagnoses: new[] { diagnosis });

        bundle.Type.Should().Be(Bundle.BundleType.Collection);
        var types = bundle.Entry.Select(e => e.Resource?.TypeName).ToList();
        types.Should().Contain("Patient");
        types.Should().Contain("Encounter");
        types.Should().Contain("Condition");
    }

    [Fact]
    public void BuildEncounterBundle_WithVitalSigns_ContainsObservations()
    {
        var service = CreateService();
        var patient = MakePatient();
        var encounter = MakeEncounter(patient.Id);
        var vs = new DomainVitalSigns
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id.ToString(),
            EncounterId = encounter.Id.ToString(),
            TemperatureC = 37.0m,
            HeartRateBpm = 75,
            WeightKg = 70m,
            TenantId = 1,
            RecordedAt = DateTime.UtcNow
        };

        var bundle = service.BuildEncounterBundle(
            patient, encounter,
            vitalSigns: new[] { vs });

        var types = bundle.Entry.Select(e => e.Resource?.TypeName).ToList();
        types.Should().Contain("Observation");
        bundle.Entry.Count(e => e.Resource?.TypeName == "Observation").Should().Be(3); // temp + hr + weight
    }

    [Fact]
    public void BuildEncounterBundle_BundleId_IsGuid()
    {
        var service = CreateService();
        var patient = MakePatient();
        var encounter = MakeEncounter(patient.Id);

        var bundle = service.BuildEncounterBundle(patient, encounter);

        Guid.TryParse(bundle.Id, out _).Should().BeTrue();
        bundle.Timestamp.Should().NotBeNull();
    }
}
