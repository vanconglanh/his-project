using FhirEncounter = Hl7.Fhir.Model.Encounter;
using DomainEncounter = ProDiabHis.Domain.Entities.Encounter;
using Hl7.Fhir.Model;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps internal Encounter entity sang FHIR R4 Encounter resource.
/// Profile: http://hl7.org/fhir/StructureDefinition/Encounter
/// </summary>
public class EncounterMapper : IFhirMapper<DomainEncounter, FhirEncounter>
{
    public FhirEncounter Map(DomainEncounter entity)
    {
        var fhir = new FhirEncounter
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Encounter" }
            },
            Status = entity.Status switch
            {
                Domain.Entities.EncounterStatus.Waiting    => FhirEncounter.EncounterStatus.Planned,
                Domain.Entities.EncounterStatus.InProgress => FhirEncounter.EncounterStatus.InProgress,
                Domain.Entities.EncounterStatus.Done       => FhirEncounter.EncounterStatus.Finished,
                Domain.Entities.EncounterStatus.Cancelled  => FhirEncounter.EncounterStatus.Cancelled,
                _                                          => FhirEncounter.EncounterStatus.Unknown
            },
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Subject = new ResourceReference($"Patient/{entity.PatientId}")
        };

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/encounter-id", entity.Id.ToString()));

        var typeCode = entity.EncounterType switch
        {
            Domain.Entities.EncounterTypes.FirstVisit  => "11429006",
            Domain.Entities.EncounterTypes.FollowUp    => "185389009",
            Domain.Entities.EncounterTypes.Emergency   => "50849002",
            _                                          => "11429006"
        };
        fhir.Type.Add(new CodeableConcept("http://snomed.info/sct", typeCode, entity.EncounterType));

        if (entity.StartedAt.HasValue || entity.FinishedAt.HasValue)
        {
            fhir.Period = new Period();
            if (entity.StartedAt.HasValue)
                fhir.Period.StartElement = new FhirDateTime(entity.StartedAt.Value);
            if (entity.FinishedAt.HasValue)
                fhir.Period.EndElement = new FhirDateTime(entity.FinishedAt.Value);
        }

        if (!string.IsNullOrEmpty(entity.ReasonForVisit))
            fhir.ReasonCode.Add(new CodeableConcept { Text = entity.ReasonForVisit });

        if (!string.IsNullOrEmpty(entity.DoctorId))
            fhir.Participant.Add(new FhirEncounter.ParticipantComponent
            {
                Individual = new ResourceReference($"Practitioner/{entity.DoctorId}")
            });

        return fhir;
    }
}
