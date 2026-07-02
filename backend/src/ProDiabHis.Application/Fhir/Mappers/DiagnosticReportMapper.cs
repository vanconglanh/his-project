using Hl7.Fhir.Model;
using ProDiabHis.Application.LabResults;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps LabResultResponse list sang FHIR R4 DiagnosticReport resource.
/// DiagnosticReport bao gom nhieu Observation result (lab panel).
/// HbA1c LOINC: 4548-4
/// </summary>
public class DiagnosticReportMapper : IFhirMapper<(string EncounterId, string PatientId, IReadOnlyList<LabResultResponse> Results), DiagnosticReport>
{
    // Common lab LOINC mapping (chi map cho common tests)
    private static readonly Dictionary<string, string> CommonLoincMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HBA1C"]    = "4548-4",
        ["GLUCOSE"]  = "15074-8",
        ["WBC"]      = "6690-2",
        ["RBC"]      = "789-8",
        ["HGB"]      = "718-7",
        ["PLT"]      = "777-3",
        ["CREATININE"] = "2160-0",
        ["UREA"]     = "3094-0",
        ["ALT"]      = "1742-6",
        ["AST"]      = "1920-8",
        ["CHOL"]     = "2093-3",
        ["TRIG"]     = "2571-8",
        ["HDL"]      = "2085-9",
        ["LDL"]      = "2089-1"
    };

    public DiagnosticReport Map((string EncounterId, string PatientId, IReadOnlyList<LabResultResponse> Results) input)
    {
        var (encounterId, patientId, results) = input;
        var reportId = results.Count > 0 ? results[0].Id.ToString() : Guid.NewGuid().ToString();

        var fhir = new DiagnosticReport
        {
            Id = reportId,
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/DiagnosticReport" }
            },
            Status = results.All(r => r.Status == LabResultStatus.Verified)
                ? DiagnosticReport.DiagnosticReportStatus.Final
                : DiagnosticReport.DiagnosticReportStatus.Partial,
            Category = new List<CodeableConcept>
            {
                new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0074", "LAB", "Laboratory")
            },
            Code = new CodeableConcept
            {
                Text = "Laboratory Panel"
            },
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}")
        };

        if (results.Count > 0)
            fhir.Effective = new FhirDateTime(results[0].PerformedAt);

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/diagnostic-report-id", encounterId));

        // Map each lab result as Observation reference + contained
        foreach (var r in results)
        {
            var obs = BuildObservation(r, patientId, encounterId);
            fhir.Contained.Add(obs);
            fhir.Result.Add(new ResourceReference($"#{obs.Id}"));
        }

        return fhir;
    }

    private static Observation BuildObservation(LabResultResponse r, string patientId, string encounterId)
    {
        var loincCode = CommonLoincMap.TryGetValue(r.TestCode.ToUpper(), out var loinc)
            ? loinc
            : r.TestCode;

        var obs = new Observation
        {
            Id = r.Id.ToString(),
            Status = r.Status switch
            {
                LabResultStatus.Verified => ObservationStatus.Final,
                LabResultStatus.Amended  => ObservationStatus.Amended,
                _                        => ObservationStatus.Preliminary
            },
            Category =
            {
                new CodeableConcept(
                    "http://terminology.hl7.org/CodeSystem/observation-category",
                    "laboratory", "Laboratory")
            },
            Code = new CodeableConcept("http://loinc.org", loincCode, r.TestName),
            Subject = new ResourceReference($"Patient/{patientId}"),
            Encounter = new ResourceReference($"Encounter/{encounterId}"),
            Effective = new FhirDateTime(r.PerformedAt)
        };

        // Value
        if (r.ValueNumeric.HasValue)
        {
            obs.Value = new Quantity
            {
                Value = r.ValueNumeric,
                Unit = r.Unit ?? string.Empty,
                System = "http://unitsofmeasure.org",
                Code = r.Unit ?? string.Empty
            };
        }
        else
        {
            obs.Value = new FhirString(r.Value);
        }

        // Reference range
        if (r.ReferenceRangeLow.HasValue || r.ReferenceRangeHigh.HasValue)
        {
            var refRange = new Observation.ReferenceRangeComponent();
            if (r.ReferenceRangeLow.HasValue)
                refRange.Low = new Quantity { Value = r.ReferenceRangeLow, Unit = r.Unit };
            if (r.ReferenceRangeHigh.HasValue)
                refRange.High = new Quantity { Value = r.ReferenceRangeHigh, Unit = r.Unit };
            obs.ReferenceRange.Add(refRange);
        }

        // Interpretation (flag)
        var interp = r.Flag switch
        {
            LabResultFlag.H or LabResultFlag.HH => "H",
            LabResultFlag.L or LabResultFlag.LL => "L",
            LabResultFlag.Critical              => "AA",
            _                                   => "N"
        };
        obs.Interpretation.Add(new CodeableConcept(
            "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
            interp));

        if (!string.IsNullOrEmpty(r.Note))
            obs.Note.Add(new Annotation { Text = new Markdown(r.Note) });

        return obs;
    }
}
