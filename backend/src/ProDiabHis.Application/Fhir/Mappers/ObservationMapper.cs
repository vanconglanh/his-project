using DomainVitalSigns = ProDiabHis.Domain.Entities.VitalSigns;
using Hl7.Fhir.Model;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps VitalSigns sang FHIR R4 Observation resource (vital signs panel).
/// LOINC codes from http://loinc.org.
/// </summary>
public class ObservationMapper : IFhirMapper<DomainVitalSigns, List<Observation>>
{
    private static readonly Dictionary<string, (string Code, string Display, string Unit, string UcumUnit)> LoincVital = new()
    {
        ["temperature"]      = ("8310-5",  "Body temperature",         "°C",    "Cel"),
        ["heart_rate"]       = ("8867-4",  "Heart rate",               "bpm",   "/min"),
        ["respiratory_rate"] = ("9279-1",  "Respiratory rate",         "/min",  "/min"),
        ["bp_systolic"]      = ("8480-6",  "Systolic blood pressure",  "mmHg",  "mm[Hg]"),
        ["bp_diastolic"]     = ("8462-4",  "Diastolic blood pressure", "mmHg",  "mm[Hg]"),
        ["spo2"]             = ("59408-5", "Oxygen saturation",        "%",     "%"),
        ["weight"]           = ("29463-7", "Body weight",              "kg",    "kg"),
        ["height"]           = ("8302-2",  "Body height",              "cm",    "cm"),
        ["pain_scale"]       = ("72514-3", "Pain severity",            "/10",   "{score}"),
        ["glucose"]          = ("15074-8", "Glucose",                  "mg/dL", "mg/dL")
    };

    public List<Observation> Map(DomainVitalSigns entity)
    {
        var result = new List<Observation>();

        void AddObs(string key, decimal? value)
        {
            if (!value.HasValue) return;
            if (!LoincVital.TryGetValue(key, out var loinc)) return;

            var obs = new Observation
            {
                Id = $"{entity.Id}-{key}",
                Meta = new Meta
                {
                    Profile = new[] { "http://hl7.org/fhir/StructureDefinition/vitalsigns" }
                },
                Status = ObservationStatus.Final,
                Category =
                {
                    new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/observation-category",
                        "vital-signs", "Vital Signs")
                },
                Code = new CodeableConcept("http://loinc.org", loinc.Code, loinc.Display),
                Subject = new ResourceReference($"Patient/{entity.PatientId}"),
                Encounter = new ResourceReference($"Encounter/{entity.EncounterId}"),
                Effective = new FhirDateTime(entity.RecordedAt),
                Value = new Quantity
                {
                    Value = value,
                    Unit = loinc.Unit,
                    System = "http://unitsofmeasure.org",
                    Code = loinc.UcumUnit
                }
            };
            obs.Identifier.Add(new Identifier("https://prodiab.vn/fhir/observation-id", obs.Id));
            result.Add(obs);
        }

        AddObs("temperature",      entity.TemperatureC);
        AddObs("heart_rate",       entity.HeartRateBpm.HasValue ? (decimal)entity.HeartRateBpm.Value : null);
        AddObs("respiratory_rate", entity.RespiratoryRate.HasValue ? (decimal)entity.RespiratoryRate.Value : null);
        AddObs("bp_systolic",      entity.BpSystolic.HasValue ? (decimal)entity.BpSystolic.Value : null);
        AddObs("bp_diastolic",     entity.BpDiastolic.HasValue ? (decimal)entity.BpDiastolic.Value : null);
        AddObs("spo2",             entity.Spo2Percent.HasValue ? (decimal)entity.Spo2Percent.Value : null);
        AddObs("weight",           entity.WeightKg);
        AddObs("height",           entity.HeightCm);
        AddObs("pain_scale",       entity.PainScale.HasValue ? (decimal)entity.PainScale.Value : null);
        AddObs("glucose",          entity.GlucoseMgDl);

        return result;
    }
}
