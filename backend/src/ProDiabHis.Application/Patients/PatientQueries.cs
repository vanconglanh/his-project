using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

public record ListPatientsQuery(
    int Page,
    int PageSize,
    string? Sort,
    string? Status,
    string? Gender)
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
