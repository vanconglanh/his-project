using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

public record ListPatientsQuery(
    int Page,
    int PageSize,
    string? Sort,
    string? Status,
    string? Gender,
    // FIX (2026-10-16): truoc day GET /patients BO QUA tu khoa -> tra ve toan bo 358 BN
    // ke ca chuoi vo nghia. Bo sung loc theo ten/ma BN (LIKE) + SDT/CMND (blind index exact).
    string? Search = null)
    : IRequest<PagedResult<PatientResponse>>;

public record SearchPatientsQuery(
    string Q,
    int Page,
    int PageSize)
    : IRequest<PagedResult<PatientResponse>>;

public record GetPatientQuery(Guid PatientId)
    : IRequest<Result<PatientResponse>>;

public record ListPatientEncountersQuery(
    Guid PatientId,
    int Page,
    int PageSize)
    : IRequest<PagedResult<EncounterSummaryDto>>;

public record ListAllergiesQuery(Guid PatientId)
    : IRequest<List<AllergyResponse>>;

public record ListInsuranceQuery(Guid PatientId)
    : IRequest<List<InsuranceResponse>>;

public record ListEmergencyContactsQuery(Guid PatientId)
    : IRequest<List<EmergencyContactResponse>>;

public record ListConsentsQuery(Guid PatientId)
    : IRequest<List<ConsentResponse>>;

public record ListGuardiansQuery(Guid PatientId)
    : IRequest<List<GuardianResponse>>;
