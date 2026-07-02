using DomainPatient = ProDiabHis.Domain.Entities.Patient;
using DomainEncounter = ProDiabHis.Domain.Entities.Encounter;
using DomainVitalSigns = ProDiabHis.Domain.Entities.VitalSigns;
using Hl7.Fhir.Model;
using ProDiabHis.Application.Fhir.Mappers;
using ProDiabHis.Application.LabResults;
using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Fhir;

/// <summary>
/// Service build FHIR Bundle chua nhieu resource lien quan den mot encounter.
/// </summary>
public class FhirBundleService
{
    private readonly PatientMapper _patientMapper;
    private readonly EncounterMapper _encounterMapper;
    private readonly ConditionMapper _conditionMapper;
    private readonly ObservationMapper _observationMapper;
    private readonly MedicationRequestMapper _medicationRequestMapper;
    private readonly ProcedureMapper _procedureMapper;
    private readonly AllergyIntoleranceMapper _allergyMapper;
    private readonly DiagnosticReportMapper _diagnosticReportMapper;

    public FhirBundleService(
        PatientMapper patientMapper,
        EncounterMapper encounterMapper,
        ConditionMapper conditionMapper,
        ObservationMapper observationMapper,
        MedicationRequestMapper medicationRequestMapper,
        ProcedureMapper procedureMapper,
        AllergyIntoleranceMapper allergyMapper,
        DiagnosticReportMapper diagnosticReportMapper)
    {
        _patientMapper = patientMapper;
        _encounterMapper = encounterMapper;
        _conditionMapper = conditionMapper;
        _observationMapper = observationMapper;
        _medicationRequestMapper = medicationRequestMapper;
        _procedureMapper = procedureMapper;
        _allergyMapper = allergyMapper;
        _diagnosticReportMapper = diagnosticReportMapper;
    }

    /// <summary>Build searchset Bundle tu patient + encounters.</summary>
    public Bundle BuildPatientBundle(DomainPatient patient, IReadOnlyList<DomainEncounter>? encounters = null)
    {
        var bundle = CreateBundle(Bundle.BundleType.Searchset);

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            Resource = _patientMapper.Map(patient),
            FullUrl = $"urn:uuid:{patient.Id}"
        });

        foreach (var enc in encounters ?? Array.Empty<DomainEncounter>())
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                Resource = _encounterMapper.Map(enc),
                FullUrl = $"urn:uuid:{enc.Id}"
            });
        }

        bundle.Total = bundle.Entry.Count;
        return bundle;
    }

    /// <summary>Build collection Bundle day du cho mot encounter (clinical document export).</summary>
    public Bundle BuildEncounterBundle(
        DomainPatient patient,
        DomainEncounter encounter,
        IReadOnlyList<EncounterDiagnosis>? diagnoses = null,
        IReadOnlyList<DomainVitalSigns>? vitalSigns = null,
        IReadOnlyList<PrescriptionResponse>? prescriptions = null,
        IReadOnlyList<LabOrder>? labOrders = null,
        IReadOnlyList<LabResultResponse>? labResults = null,
        IReadOnlyList<Allergy>? allergies = null)
    {
        var bundle = CreateBundle(Bundle.BundleType.Collection);

        bundle.Entry.Add(MakeEntry(_patientMapper.Map(patient), patient.Id.ToString()));
        bundle.Entry.Add(MakeEntry(_encounterMapper.Map(encounter), encounter.Id.ToString()));

        foreach (var dx in diagnoses ?? Array.Empty<EncounterDiagnosis>())
            bundle.Entry.Add(MakeEntry(_conditionMapper.Map(dx), dx.Id.ToString()));

        foreach (var vs in vitalSigns ?? Array.Empty<DomainVitalSigns>())
        {
            foreach (var obs in _observationMapper.Map(vs))
                bundle.Entry.Add(MakeEntry(obs, obs.Id));
        }

        foreach (var rx in prescriptions ?? Array.Empty<PrescriptionResponse>())
        {
            foreach (var mr in _medicationRequestMapper.Map(rx))
                bundle.Entry.Add(MakeEntry(mr, mr.Id));
        }

        foreach (var lo in labOrders ?? Array.Empty<LabOrder>())
            bundle.Entry.Add(MakeEntry(_procedureMapper.Map(lo), lo.Id.ToString()));

        if (labResults?.Count > 0)
        {
            var report = _diagnosticReportMapper.Map((encounter.Id.ToString(), patient.Id.ToString(), labResults));
            bundle.Entry.Add(MakeEntry(report, report.Id));
        }

        foreach (var al in allergies ?? Array.Empty<Allergy>())
            bundle.Entry.Add(MakeEntry(_allergyMapper.Map(al), al.Id.ToString()));

        bundle.Total = bundle.Entry.Count;
        return bundle;
    }

    private static Bundle CreateBundle(Bundle.BundleType type)
        => new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Meta = new Meta { LastUpdated = DateTimeOffset.UtcNow },
            Type = type,
            Timestamp = DateTimeOffset.UtcNow
        };

    private static Bundle.EntryComponent MakeEntry(Resource resource, string id)
        => new Bundle.EntryComponent
        {
            Resource = resource,
            FullUrl = $"urn:uuid:{id}"
        };
}
