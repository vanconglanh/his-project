using Hl7.Fhir.Model;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Fhir.Mappers;

/// <summary>
/// Maps LabOrder / RadOrder sang FHIR R4 Procedure resource.
/// CLS orders (lab + rad) duoc bieu dien la Procedure.
/// </summary>
public class ProcedureMapper : IFhirMapper<LabOrder, Procedure>
{
    public Procedure Map(LabOrder entity)
    {
        var fhir = new Procedure
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Procedure" }
            },
            Status = entity.Status switch
            {
                LabOrderStatus.Done      => EventStatus.Completed,
                LabOrderStatus.Cancelled => EventStatus.Stopped,
                LabOrderStatus.Processing or LabOrderStatus.SampleTaken => EventStatus.InProgress,
                _                        => EventStatus.Preparation
            },
            Code = new CodeableConcept
            {
                Text = entity.TestName,
                Coding = new List<Coding>
                {
                    new Coding("http://loinc.org", entity.TestCode, entity.TestName)
                }
            },
            Encounter = new ResourceReference($"Encounter/{entity.EncounterId}"),
            Performed = new FhirDateTime(entity.OrderedAt)
        };

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/procedure-id", entity.Id.ToString()));

        if (!string.IsNullOrEmpty(entity.Note))
            fhir.Note.Add(new Annotation { Text = new Markdown(entity.Note) });

        return fhir;
    }
}

/// <summary>Maps RadOrder sang FHIR Procedure (imaging).</summary>
public class RadProcedureMapper : IFhirMapper<RadOrder, Procedure>
{
    public Procedure Map(RadOrder entity)
    {
        var fhir = new Procedure
        {
            Id = entity.Id.ToString(),
            Meta = new Meta
            {
                Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Procedure" }
            },
            Status = entity.Status switch
            {
                RadOrderStatus.Done       => EventStatus.Completed,
                RadOrderStatus.Cancelled  => EventStatus.Stopped,
                RadOrderStatus.InProgress => EventStatus.InProgress,
                _                         => EventStatus.Preparation
            },
            Code = new CodeableConcept
            {
                Text = entity.ProcedureName,
                Coding = new List<Coding>
                {
                    new Coding("http://snomed.info/sct", entity.ProcedureCode, entity.ProcedureName)
                }
            },
            Category = new CodeableConcept("http://snomed.info/sct", "363679005", "Imaging"),
            Encounter = new ResourceReference($"Encounter/{entity.EncounterId}"),
            Performed = new FhirDateTime(entity.OrderedAt)
        };

        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/procedure-rad-id", entity.Id.ToString()));

        if (!string.IsNullOrEmpty(entity.BodyPart))
            fhir.BodySite.Add(new CodeableConcept { Text = entity.BodyPart });

        if (!string.IsNullOrEmpty(entity.Note))
            fhir.Note.Add(new Annotation { Text = new Markdown(entity.Note) });

        return fhir;
    }
}
