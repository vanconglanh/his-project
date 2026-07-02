using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Patients;

public class ListPatientsQueryHandler : IRequestHandler<ListPatientsQuery, PagedResult<PatientResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListPatientsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<PatientResponse>> Handle(ListPatientsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Patients.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (!string.IsNullOrEmpty(request.Gender))
            query = query.Where(p => p.Gender == request.Gender);

        query = request.Sort switch
        {
            "full_name"  => query.OrderBy(p => p.FullName),
            "-full_name" => query.OrderByDescending(p => p.FullName),
            _            => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var offset = (request.Page - 1) * request.PageSize;
        var patients = await query.Skip(offset).Take(request.PageSize).ToListAsync(cancellationToken);

        var items = patients.Select(PatientEntityMapper.ToResponse).ToList();
        return new PagedResult<PatientResponse>(items, request.Page, request.PageSize, total);
    }
}

public class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, PagedResult<PatientResponse>>
{
    private readonly IApplicationDbContext _db;

    public SearchPatientsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<PatientResponse>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var q = request.Q.ToLower();
        var query = _db.Patients.AsNoTracking()
            .Where(p =>
                p.FullName.ToLower().Contains(q) ||
                p.Code.ToLower().Contains(q) ||
                (p.Phone != null && p.Phone.Contains(q)) ||
                (p.IdNumberMasked != null && p.IdNumberMasked.Contains(q)));

        var total = await query.CountAsync(cancellationToken);
        var offset = (request.Page - 1) * request.PageSize;
        var patients = await query.OrderBy(p => p.FullName)
            .Skip(offset).Take(request.PageSize).ToListAsync(cancellationToken);

        var items = patients.Select(PatientEntityMapper.ToResponse).ToList();
        return new PagedResult<PatientResponse>(items, request.Page, request.PageSize, total);
    }
}

public class GetPatientQueryHandler : IRequestHandler<GetPatientQuery, Result<PatientResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetPatientQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PatientResponse>> Handle(GetPatientQuery request, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient is null)
            return Result<PatientResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));
    }
}

public class ListPatientEncountersQueryHandler : IRequestHandler<ListPatientEncountersQuery, PagedResult<EncounterSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public ListPatientEncountersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<EncounterSummaryDto>> Handle(ListPatientEncountersQuery request, CancellationToken cancellationToken)
    {
        var patientIdStr = request.PatientId.ToString();
        var query = _db.Encounters.AsNoTracking().Where(e => e.PatientId == patientIdStr);

        var total = await query.CountAsync(cancellationToken);
        var offset = (request.Page - 1) * request.PageSize;
        var encounters = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(offset).Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var encounterIds = encounters.Select(e => e.Id.ToString()).ToList();
        var diagnoses = await _db.EncounterDiagnoses.AsNoTracking()
            .Where(d => encounterIds.Contains(d.EncounterId))
            .ToListAsync(cancellationToken);

        var items = encounters.Select(e => new EncounterSummaryDto(
            Id: e.Id,
            EncounterNo: "",
            EncounterDate: e.StartedAt ?? e.CreatedAt,
            DoctorName: null,
            RoomName: null,
            ChiefComplaint: e.ChiefComplaint,
            DiagnosisIcd10: diagnoses.Where(d => d.EncounterId == e.Id.ToString()).Select(d => d.Icd10Code).ToList(),
            Status: e.Status)).ToList();

        return new PagedResult<EncounterSummaryDto>(items, request.Page, request.PageSize, total);
    }
}

public class ListAllergiesQueryHandler : IRequestHandler<ListAllergiesQuery, List<AllergyResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListAllergiesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AllergyResponse>> Handle(ListAllergiesQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists) return new List<AllergyResponse>();

        var allergies = await _db.Allergies.AsNoTracking()
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return allergies.Select(a => new AllergyResponse(
            a.Id, a.PatientId, a.Allergen, a.Reaction, a.Severity, a.OnsetDate, a.Note, a.CreatedAt)).ToList();
    }
}

public class ListInsuranceQueryHandler : IRequestHandler<ListInsuranceQuery, List<InsuranceResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListInsuranceQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InsuranceResponse>> Handle(ListInsuranceQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists) return new List<InsuranceResponse>();

        var insurances = await _db.Insurances.AsNoTracking()
            .OrderByDescending(i => i.ValidTo)
            .ToListAsync(cancellationToken);

        return insurances.Select(i => new InsuranceResponse(
            i.Id, i.PatientId, i.Type, i.CardNoMasked,
            i.ValidFrom, i.ValidTo, i.HospitalCode, i.CoveragePercent, i.CreatedAt)).ToList();
    }
}

public class ListEmergencyContactsQueryHandler : IRequestHandler<ListEmergencyContactsQuery, List<EmergencyContactResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListEmergencyContactsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<EmergencyContactResponse>> Handle(ListEmergencyContactsQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists) return new List<EmergencyContactResponse>();

        var contacts = await _db.EmergencyContacts.AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return contacts.Select(c => new EmergencyContactResponse(
            c.Id, c.PatientId, c.FullName, c.Relationship, c.Phone, c.Address)).ToList();
    }
}

public class ListConsentsQueryHandler : IRequestHandler<ListConsentsQuery, List<ConsentResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListConsentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ConsentResponse>> Handle(ListConsentsQuery request, CancellationToken cancellationToken)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists) return new List<ConsentResponse>();

        var consents = await _db.Consents.AsNoTracking()
            .OrderByDescending(c => c.SignedAt)
            .ToListAsync(cancellationToken);

        return consents.Select(c => new ConsentResponse(
            c.Id, c.PatientId, c.ConsentType, c.SignedAt, c.SignedBy, null, c.RevokedAt)).ToList();
    }
}
