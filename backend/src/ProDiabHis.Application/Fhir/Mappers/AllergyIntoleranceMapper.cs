using Hl7.Fhir.Model;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps Allergy entity sang FHIR R4 AllergyIntolerance resource.
/// </summary>
public class AllergyIntoleranceMapper : IFhirMapper<Allergy, AllergyIntolerance>
{
    public AllergyIntolerance Map(Allergy entity)
    {
        var fhir = new AllergyIntolerance
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/AllergyIntolerance" }
            },
            ClinicalStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical",
                entity.DeletedAt == null ? "active" : "inactive"),
            VerificationStatus = new CodeableConcept(
                "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification",
                "confirmed"),
            Code = new CodeableConcept { Text = entity.Allergen },
            Patient = new ResourceReference($"Patient/{entity.PatientId}")
        };

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/allergy-id", entity.Id.ToString()));

        // Criticality from severity
        fhir.Criticality = entity.Severity?.ToUpper() switch
        {
            "HIGH" or "SEVERE" => AllergyIntolerance.AllergyIntoleranceCriticality.High,
            "LOW" or "MILD"    => AllergyIntolerance.AllergyIntoleranceCriticality.Low,
            _                  => AllergyIntolerance.AllergyIntoleranceCriticality.UnableToAssess
        };

        // Onset
        if (entity.OnsetDate.HasValue)
            fhir.Onset = new FhirDateTime(entity.OnsetDate.Value.ToString("yyyy-MM-dd"));

        // Reaction
        if (!string.IsNullOrEmpty(entity.Reaction))
        {
            fhir.Reaction.Add(new AllergyIntolerance.ReactionComponent
            {
                Manifestation = new List<CodeableConcept>
                {
                    new CodeableConcept { Text = entity.Reaction }
                },
                Severity = entity.Severity?.ToUpper() switch
                {
                    "HIGH" or "SEVERE" => AllergyIntolerance.AllergyIntoleranceSeverity.Severe,
                    "MODERATE"         => AllergyIntolerance.AllergyIntoleranceSeverity.Moderate,
                    _                  => AllergyIntolerance.AllergyIntoleranceSeverity.Mild
                }
            });
        }

        if (!string.IsNullOrEmpty(entity.Note))
            fhir.Note.Add(new Annotation { Text = new Markdown(entity.Note) });

        return fhir;
    }
}
