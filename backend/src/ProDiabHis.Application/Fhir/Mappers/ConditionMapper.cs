using Hl7.Fhir.Model;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps EncounterDiagnosis (ICD-10) sang FHIR R4 Condition resource.
/// ICD-10 system: http://hl7.org/fhir/sid/icd-10
/// </summary>
public class ConditionMapper : IFhirMapper<EncounterDiagnosis, Condition>
{
    public Condition Map(EncounterDiagnosis entity)
    {
        var fhir = new Condition
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Condition" }
            },
            // Clinical status: active
            ClinicalStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-clinical",
                "active", "Active"),
            // Verification status
            VerificationStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/condition-ver-status",
                "confirmed", "Confirmed"),
            // ICD-10 code
            Code = new CodeableConcept(
                "http://hl7.org/fhir/sid/icd-10",
                entity.Icd10Code,
                entity.Name),
            // Subject = encounter's patient (caller should populate if needed)
            Subject = new ResourceReference($"Encounter/{entity.EncounterId}"),
            // Encounter context
            Encounter = new ResourceReference($"Encounter/{entity.EncounterId}")
        };

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/condition-id", entity.Id.ToString()));

        // Category based on diagnosis type
        var category = entity.Type == DiagnosisType.Primary ? "encounter-diagnosis" : "secondary-diagnosis";
        fhir.Category.Add(new CodeableConcept(
            "http://terminology.hl7.org/CodeSystem/condition-category",
            category));

        if (!string.IsNullOrEmpty(entity.Note))
            fhir.Note.Add(new Annotation { Text = new Markdown(entity.Note) });

        return fhir;
    }
}
