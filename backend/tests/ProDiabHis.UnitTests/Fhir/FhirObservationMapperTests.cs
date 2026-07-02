using DomainVitalSigns = ProDiabHis.Domain.Entities.VitalSigns;
using FluentAssertions;
using Hl7.Fhir.Model;
using ProDiabHis.Application.Fhir.Mappers;
using Xunit;

namespace ProDiabHis.UnitTests.Fhir;

public class FhirObservationMapperTests
{
    private readonly ObservationMapper _mapper = new();

    private static DomainVitalSigns MakeVitalSigns(Action<DomainVitalSigns>? configure = null)
    {
        var vs = new DomainVitalSigns
        {
            Id = Guid.NewGuid(),
            EncounterId = Guid.NewGuid().ToString(),
            PatientId = Guid.NewGuid().ToString(),
            RecordedAt = DateTime.UtcNow,
            TenantId = 1
        };
        configure?.Invoke(vs);
        return vs;
    }

    [Fact]
    public void Map_FullVitalSigns_ReturnsAllObservations()
    {
        var vs = MakeVitalSigns(v =>
        {
            v.TemperatureC = 37.2m;
            v.HeartRateBpm = 80;
            v.RespiratoryRate = 16;
            v.BpSystolic = 120;
            v.BpDiastolic = 80;
            v.Spo2Percent = 98;
            v.WeightKg = 65m;
            v.HeightCm = 170m;
            v.PainScale = 2;
            v.GlucoseMgDl = 126m;
        });

        var observations = _mapper.Map(vs);

        observations.Should().HaveCount(10);
        observations.Should().AllSatisfy(o =>
        {
            o.Status.Should().Be(ObservationStatus.Final);
            o.Category.Should().HaveCount(1);
            o.Category[0].Coding[0].Code.Should().Be("vital-signs");
        });
    }

    [Fact]
    public void Map_TemperatureOnly_ReturnsSingleObservation()
    {
        var vs = MakeVitalSigns(v => v.TemperatureC = 38.5m);

        var observations = _mapper.Map(vs);

        observations.Should().HaveCount(1);
        var obs = observations[0];
        obs.Code.Coding[0].System.Should().Be("http://loinc.org");
        obs.Code.Coding[0].Code.Should().Be("8310-5");
        var qty = obs.Value as Quantity;
        qty.Should().NotBeNull();
        qty!.Value.Should().Be(38.5m);
        qty.Unit.Should().Be("°C");
        qty.System.Should().Be("http://unitsofmeasure.org");
    }

    [Fact]
    public void Map_GlucoseObs_LoincCodeCorrect()
    {
        var vs = MakeVitalSigns(v => v.GlucoseMgDl = 200m);

        var observations = _mapper.Map(vs);

        var glucoseObs = observations.Single();
        glucoseObs.Code.Coding[0].Code.Should().Be("15074-8");
    }

    [Fact]
    public void Map_BloodPressure_SystolicDiastolicSeparate()
    {
        var vs = MakeVitalSigns(v =>
        {
            v.BpSystolic = 140;
            v.BpDiastolic = 90;
        });

        var observations = _mapper.Map(vs);

        observations.Should().HaveCount(2);
        observations.Should().Contain(o => o.Code.Coding[0].Code == "8480-6"); // systolic
        observations.Should().Contain(o => o.Code.Coding[0].Code == "8462-4"); // diastolic
    }

    [Fact]
    public void Map_EmptyVitalSigns_ReturnsEmpty()
    {
        var vs = MakeVitalSigns(); // all null

        var observations = _mapper.Map(vs);

        observations.Should().BeEmpty();
    }

    [Fact]
    public void Map_VitalSigns_SubjectAndEncounterReferenceCorrect()
    {
        var patId = Guid.NewGuid().ToString();
        var encId = Guid.NewGuid().ToString();
        var vs = MakeVitalSigns(v =>
        {
            v.PatientId = patId;
            v.EncounterId = encId;
            v.WeightKg = 60m;
        });

        var observations = _mapper.Map(vs);

        var obs = observations.Single();
        obs.Subject!.Reference.Should().Be($"Patient/{patId}");
        obs.Encounter!.Reference.Should().Be($"Encounter/{encId}");
    }
}
