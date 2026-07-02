using Hl7.Fhir.Model;
using ProDiabHis.Application.Pharmacy.Prescriptions;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps PrescriptionResponse sang FHIR R4 MedicationRequest resource.
/// ATC system: http://www.whocc.no/atc
/// </summary>
public class MedicationRequestMapper : IFhirMapper<PrescriptionResponse, List<MedicationRequest>>
{
    public List<MedicationRequest> Map(PrescriptionResponse entity)
    {
        var result = new List<MedicationRequest>();

        foreach (var item in entity.Items ?? Array.Empty<PrescriptionItemResponse>())
        {
            var mr = new MedicationRequest
            {
                Id = item.Id.ToString(),
                Meta = new Meta
                {
                    Profile = new[] { "http://hl7.org/fhir/StructureDefinition/MedicationRequest" }
                },
                Status = entity.Status switch
                {
                    "SIGNED"      => MedicationRequest.MedicationrequestStatus.Active,
                    "DISPENSED"   => MedicationRequest.MedicationrequestStatus.Completed,
                    "CANCELLED"   => MedicationRequest.MedicationrequestStatus.Cancelled,
                    _             => MedicationRequest.MedicationrequestStatus.Draft
                },
                Intent = MedicationRequest.MedicationRequestIntent.Order,
                Medication = new CodeableConcept
                {
                    Text = item.DrugName,
                    Coding = new List<Coding>
                    {
                        new Coding("http://www.whocc.no/atc", item.DrugId.ToString(), item.DrugName)
                    }
                },
                Subject = new ResourceReference($"Patient/{entity.PatientId}"),
                Encounter = new ResourceReference($"Encounter/{entity.EncounterId}"),
                AuthoredOn = entity.PrescribedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            mr.Identifier.Add(new Identifier("https://prodiab.vn/fhir/medication-request-id", item.Id.ToString()));

            // Prescriber
            if (entity.DoctorId.HasValue)
                mr.Requester = new ResourceReference($"Practitioner/{entity.DoctorId}");

            // DosageInstruction
            var dosage = new Dosage
            {
                Text = $"{item.Dosage} {item.Frequency} x {item.DurationDays} ngày",
                Route = new CodeableConcept { Text = item.Route },
                DoseAndRate = new List<Dosage.DoseAndRateComponent>
                {
                    new Dosage.DoseAndRateComponent
                    {
                        Dose = new Quantity
                        {
                            Value = item.Quantity,
                            Unit = item.Unit ?? "tablet",
                            System = "http://terminology.hl7.org/CodeSystem/v3-orderableDrugForm"
                        }
                    }
                }
            };
            if (!string.IsNullOrEmpty(item.Instructions))
                dosage.PatientInstruction = item.Instructions;
            mr.DosageInstruction.Add(dosage);

            // DispenseRequest
            mr.DispenseRequest = new MedicationRequest.DispenseRequestComponent
            {
                Quantity = new Quantity
                {
                    Value = item.Quantity,
                    Unit = item.Unit ?? "tablet"
                },
                ExpectedSupplyDuration = new Duration
                {
                    Value = item.DurationDays,
                    Unit = "days",
                    System = "http://unitsofmeasure.org",
                    Code = "d"
                }
            };

            if (!string.IsNullOrEmpty(entity.Note))
                mr.Note.Add(new Annotation { Text = new Markdown(entity.Note) });

            result.Add(mr);
        }

        return result;
    }
}
