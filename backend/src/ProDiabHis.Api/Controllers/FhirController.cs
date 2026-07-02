using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.Fhir;
using ProDiabHis.Application.Fhir.Mappers;
using ProDiabHis.Application.Patients;
using MediatR;
using ProDiabHis.Application.Encounters;

namespace ProDiabHis.Api.Controllers;

/// <summary>
/// FHIR R4 endpoint — cung cap Patient, Encounter, Condition, Observation,
/// MedicationRequest, Procedure, AllergyIntolerance, DiagnosticReport, Bundle.
/// Content-Type: application/fhir+json
/// Permission yeu cau: fhir.read (ADMIN + BACSI mac dinh)
/// </summary>
[ApiController]
[Route("api/fhir/r4")]
[Authorize]
[RequirePermission("fhir.read")]
public class FhirController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly FhirBundleService _bundleService;

    private static readonly FhirJsonSerializer FhirSerializer = new(new SerializerSettings { Pretty = false });

    public FhirController(IMediator mediator, FhirBundleService bundleService)
    {
        _mediator = mediator;
        _bundleService = bundleService;
    }

    // ── CapabilityStatement ──────────────────────────────────────────────────

    /// <summary>GET /api/fhir/r4/metadata — CapabilityStatement, khong can auth</summary>
    [HttpGet("metadata")]
    [AllowAnonymous]
    public IActionResult Metadata()
    {
        var cs = new CapabilityStatement
        {
            Id = "prodiab-his-capability",
            Status = PublicationStatus.Active,
            Date = "2026-05-23",
            Software = new CapabilityStatement.SoftwareComponent
            {
                Name = "Pro-Diab HIS",
                Version = "1.0.0"
            },
            FhirVersion = FHIRVersion.N4_0_1,
            Format = new[] { "application/fhir+json", "application/json" },
            Rest = new List<CapabilityStatement.RestComponent>
            {
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Resource = new List<CapabilityStatement.ResourceComponent>
                    {
                        MakeCap("Patient",           new[]{"read","search-type"}),
                        MakeCap("Encounter",         new[]{"read","search-type"}),
                        MakeCap("Condition",         new[]{"read","search-type"}),
                        MakeCap("Observation",       new[]{"read","search-type"}),
                        MakeCap("MedicationRequest", new[]{"read","search-type"}),
                        MakeCap("Procedure",         new[]{"read","search-type"}),
                        MakeCap("AllergyIntolerance",new[]{"read","search-type"}),
                        MakeCap("DiagnosticReport",  new[]{"read","search-type"}),
                        MakeCap("Bundle",            new[]{"read"})
                    }
                }
            }
        };

        return FhirJson(cs);
    }

    // ── Patient ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/fhir/r4/Patient/{id}</summary>
    [HttpGet("Patient/{id:guid}")]
    public async Task<IActionResult> GetPatient(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPatientQuery(id), ct);
        if (!result.IsSuccess)
            return FhirNotFound("Patient", id.ToString());

        return FhirJson(MapPatientResponse(result.Value!));
    }

    /// <summary>GET /api/fhir/r4/Patient?identifier=...&name=...</summary>
    [HttpGet("Patient")]
    public async Task<IActionResult> SearchPatient(
        [FromQuery] string? identifier,
        [FromQuery] string? name,
        [FromQuery] int _count = 20,
        CancellationToken ct = default)
    {
        var q = identifier ?? name ?? string.Empty;
        var result = await _mediator.Send(new SearchPatientsQuery(q, 1, _count), ct);

        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Type = Bundle.BundleType.Searchset,
            Total = result.Total,
            Timestamp = DateTimeOffset.UtcNow
        };

        foreach (var dto in result.Items)
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                Resource = MapPatientResponse(dto),
                FullUrl = $"urn:uuid:{dto.Id}"
            });

        return FhirJson(bundle);
    }

    // ── Encounter ────────────────────────────────────────────────────────────

    /// <summary>GET /api/fhir/r4/Encounter/{id}</summary>
    [HttpGet("Encounter/{id:guid}")]
    public async Task<IActionResult> GetEncounter(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEncounterDetailQuery(id), ct);
        if (!result.IsSuccess)
            return FhirNotFound("Encounter", id.ToString());

        return FhirJson(MapEncounterDetail(result.Value!));
    }

    /// <summary>GET /api/fhir/r4/Encounter?patient={patientId}</summary>
    [HttpGet("Encounter")]
    public async Task<IActionResult> SearchEncounter(
        [FromQuery] Guid? patient,
        [FromQuery] int _count = 20,
        CancellationToken ct = default)
    {
        if (!patient.HasValue)
            return FhirBadRequest("patient parameter required");

        var result = await _mediator.Send(new ListPatientEncountersQuery(patient.Value, 1, _count), ct);

        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Type = Bundle.BundleType.Searchset,
            Total = result.Total,
            Timestamp = DateTimeOffset.UtcNow
        };

        foreach (var dto in result.Items)
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                Resource = MapEncounterSummary(dto, patient.Value),
                FullUrl = $"urn:uuid:{dto.Id}"
            });

        return FhirJson(bundle);
    }

    // ── Bundle ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/fhir/r4/Bundle?patient={id} — searchset bundle cho patient</summary>
    [HttpGet("Bundle")]
    public async Task<IActionResult> GetBundle(
        [FromQuery] string? type,
        [FromQuery] Guid? patient,
        CancellationToken ct = default)
    {
        if (!patient.HasValue)
            return FhirBadRequest("patient parameter required");

        var patResult = await _mediator.Send(new GetPatientQuery(patient.Value), ct);
        if (!patResult.IsSuccess)
            return FhirNotFound("Patient", patient.Value.ToString());

        var bundleType = type == "collection" ? Bundle.BundleType.Collection : Bundle.BundleType.Searchset;

        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Type = bundleType,
            Timestamp = DateTimeOffset.UtcNow
        };

        bundle.Entry.Add(new Bundle.EntryComponent
        {
            Resource = MapPatientResponse(patResult.Value!),
            FullUrl = $"urn:uuid:{patient}"
        });

        bundle.Total = bundle.Entry.Count;
        return FhirJson(bundle);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ContentResult FhirJson(Resource resource)
    {
        var json = FhirSerializer.SerializeToString(resource);
        return Content(json, "application/fhir+json");
    }

    private ContentResult FhirNotFound(string resourceType, string id)
    {
        var oo = new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.NotFound,
                    Diagnostics = $"{resourceType}/{id} not found"
                }
            }
        };
        Response.StatusCode = 404;
        return Content(FhirSerializer.SerializeToString(oo), "application/fhir+json");
    }

    private ContentResult FhirBadRequest(string message)
    {
        var oo = new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = OperationOutcome.IssueType.Required,
                    Diagnostics = message
                }
            }
        };
        Response.StatusCode = 400;
        return Content(FhirSerializer.SerializeToString(oo), "application/fhir+json");
    }

    private static CapabilityStatement.ResourceComponent MakeCap(string type, string[] interactions)
        => new CapabilityStatement.ResourceComponent
        {
            TypeElement = new Code(type),
            Interaction = interactions.Select(i => new CapabilityStatement.ResourceInteractionComponent
            {
                Code = i switch
                {
                    "read"        => CapabilityStatement.TypeRestfulInteraction.Read,
                    "search-type" => CapabilityStatement.TypeRestfulInteraction.SearchType,
                    _             => CapabilityStatement.TypeRestfulInteraction.Read
                }
            }).ToList()
        };

    private static Hl7.Fhir.Model.Patient MapPatientResponse(PatientResponse dto)
    {
        var fhir = new Hl7.Fhir.Model.Patient
        {
            Id = dto.Id.ToString(),
            Meta = new Meta { Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Patient" } }
        };
        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/patient-id", dto.Id.ToString()));
        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/patient-code", dto.Code));
        fhir.Name.Add(new HumanName { Text = dto.FullName, Use = HumanName.NameUse.Official });
        fhir.Gender = dto.Gender switch
        {
            "MALE"   => AdministrativeGender.Male,
            "FEMALE" => AdministrativeGender.Female,
            _        => AdministrativeGender.Other
        };
        if (dto.DateOfBirth.HasValue)
            fhir.BirthDate = dto.DateOfBirth.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(dto.Phone))
            fhir.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = dto.Phone
            });
        if (!string.IsNullOrEmpty(dto.Email))
            fhir.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = dto.Email
            });
        fhir.Active = dto.Status == "ACTIVE";
        return fhir;
    }

    private static Hl7.Fhir.Model.Encounter MapEncounterDetail(EncounterDetailResponse dto)
    {
        var fhir = new Hl7.Fhir.Model.Encounter
        {
            Id = dto.Id.ToString(),
            Meta = new Meta { Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Encounter" } },
            Status = dto.Status switch
            {
                "WAITING"     => Hl7.Fhir.Model.Encounter.EncounterStatus.Planned,
                "IN_PROGRESS" => Hl7.Fhir.Model.Encounter.EncounterStatus.InProgress,
                "DONE"        => Hl7.Fhir.Model.Encounter.EncounterStatus.Finished,
                "CANCELLED"   => Hl7.Fhir.Model.Encounter.EncounterStatus.Cancelled,
                _             => Hl7.Fhir.Model.Encounter.EncounterStatus.Unknown
            },
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB"),
            Subject = new ResourceReference($"Patient/{dto.PatientId}")
        };
        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/encounter-id", dto.Id.ToString()));
        if (!string.IsNullOrEmpty(dto.ReasonForVisit))
            fhir.ReasonCode.Add(new CodeableConcept { Text = dto.ReasonForVisit });
        return fhir;
    }

    private static Hl7.Fhir.Model.Encounter MapEncounterSummary(EncounterSummaryDto dto, Guid patientId)
    {
        var fhir = new Hl7.Fhir.Model.Encounter
        {
            Id = dto.Id.ToString(),
            Meta = new Meta { Profile = new[] { "http://hl7.org/fhir/StructureDefinition/Encounter" } },
            Status = Hl7.Fhir.Model.Encounter.EncounterStatus.Unknown,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB"),
            Subject = new ResourceReference($"Patient/{patientId}")
        };
        fhir.Identifier.Add(new Identifier("https://prodiab.vn/fhir/encounter-id", dto.Id.ToString()));
        if (!string.IsNullOrEmpty(dto.ChiefComplaint))
            fhir.ReasonCode.Add(new CodeableConcept { Text = dto.ChiefComplaint });
        return fhir;
    }
}
