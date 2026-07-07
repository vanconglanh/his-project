using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Patients;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _enc;
    private readonly IAuditService _audit;

    public CreatePatientCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser currentUser, IEncryptionService enc, IAuditService audit)
    {
        _db = db; _tenant = tenant; _currentUser = currentUser; _enc = enc; _audit = audit;
    }

    public async Task<Result<PatientResponse>> Handle(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var prefix = $"BNT{_tenant.TenantId:D2}";
        // IgnoreQueryFilters: phai tinh seq tren CA benh nhan da soft-delete —
        // unique key uq_patients_code_tenant van tinh dong deleted_at != null nen
        // KHONG duoc tai su dung ma cua benh nhan da xoa (gay Duplicate entry 500).
        var existingCodes = await _db.Patients.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.TenantId == _tenant.TenantId && p.Code.StartsWith(prefix))
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var seq = existingCodes.Count > 0
            ? existingCodes
                .Select(c => { long.TryParse(c[prefix.Length..], out var n); return n; })
                .DefaultIfEmpty(0).Max() + 1
            : 1;
        var code = $"{prefix}{seq:D6}";

        var codeExists = await _db.Patients.IgnoreQueryFilters()
            .AnyAsync(p => p.TenantId == _tenant.TenantId && p.Code == code, cancellationToken);
        if (codeExists)
            return Result<PatientResponse>.Failure("PATIENT_CODE_EXISTS", "Mã bệnh nhân đã tồn tại");

        string? idNumEnc = null, idNumMasked = null;
        if (!string.IsNullOrEmpty(req.IdNumber))
        {
            idNumEnc = _enc.Encrypt(req.IdNumber);
            idNumMasked = PatientMappingHelper.MaskIdNumber(req.IdNumber);
        }

        var now = DateTime.UtcNow;
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            Code = code,
            FullName = req.FullName,
            Gender = req.Gender,
            DateOfBirth = req.DateOfBirth,
            IdNumberEnc = idNumEnc,
            IdNumberMasked = idNumMasked,
            Phone = req.Phone,
            Email = req.Email,
            ProvinceCode = req.Address?.ProvinceCode,
            DistrictCode = req.Address?.DistrictCode,
            WardCode = req.Address?.WardCode,
            Street = req.Address?.Street,
            Occupation = req.Occupation,
            Ethnicity = req.Ethnicity,
            BloodType = req.BloodType,
            Status = PatientStatus.Active,
            IdCardIssuedDate = req.IdCardIssuedDate,
            IdCardIssuedPlace = req.IdCardIssuedPlace,
            Nationality = req.Nationality,
            PatientType = req.PatientType,
            MaritalStatus = req.MaritalStatus,
            VisitType = req.VisitType,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
            UpdatedAt = now
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("CREATE", "Patient", patient.Id.ToString(), new { code }, cancellationToken);

        return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));
    }
}

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result<PatientResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _enc;
    private readonly IAuditService _audit;

    public UpdatePatientCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser currentUser, IEncryptionService enc, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _enc = enc; _audit = audit;
    }

    public async Task<Result<PatientResponse>> Handle(UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

        if (patient is null)
            return Result<PatientResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var req = command.Request;
        if (!string.IsNullOrEmpty(req.IdNumber))
        {
            patient.IdNumberEnc = _enc.Encrypt(req.IdNumber);
            patient.IdNumberMasked = PatientMappingHelper.MaskIdNumber(req.IdNumber);
        }

        patient.FullName = req.FullName;
        patient.Gender = req.Gender;
        patient.DateOfBirth = req.DateOfBirth;
        patient.Phone = req.Phone;
        patient.Email = req.Email;
        patient.ProvinceCode = req.Address?.ProvinceCode;
        patient.DistrictCode = req.Address?.DistrictCode;
        patient.WardCode = req.Address?.WardCode;
        patient.Street = req.Address?.Street;
        patient.Occupation = req.Occupation;
        patient.Ethnicity = req.Ethnicity;
        patient.BloodType = req.BloodType;
        if (!string.IsNullOrEmpty(req.Status)) patient.Status = req.Status;
        if (req.IdCardIssuedDate.HasValue) patient.IdCardIssuedDate = req.IdCardIssuedDate;
        if (!string.IsNullOrEmpty(req.IdCardIssuedPlace)) patient.IdCardIssuedPlace = req.IdCardIssuedPlace;
        if (!string.IsNullOrEmpty(req.Nationality)) patient.Nationality = req.Nationality;
        if (!string.IsNullOrEmpty(req.PatientType)) patient.PatientType = req.PatientType;
        if (req.MaritalStatus != null) patient.MaritalStatus = req.MaritalStatus;
        if (req.VisitType != null) patient.VisitType = req.VisitType;
        patient.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("UPDATE", "Patient", patient.Id.ToString(), null, cancellationToken);

        return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));
    }
}

public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public DeletePatientCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<bool>> Handle(DeletePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

        if (patient is null)
            return Result<bool>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        patient.DeletedAt = DateTime.UtcNow;
        patient.DeletedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("DELETE", "Patient", patient.Id.ToString(), null, cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<string>>
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public UploadAvatarCommandHandler(IApplicationDbContext db, IFileStorage storage)
    {
        _db = db; _storage = storage;
    }

    public async Task<Result<string>> Handle(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        if (command.SizeBytes > 2 * 1024 * 1024)
            return Result<string>.Failure("AVATAR_FILE_TOO_LARGE", "Ảnh đại diện vượt quá 2MB");

        var allowed = new[] { "image/jpeg", "image/png" };
        if (!allowed.Contains(command.ContentType))
            return Result<string>.Failure("AVATAR_INVALID_FORMAT", "Chỉ chấp nhận ảnh PNG hoặc JPEG");

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<string>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var ext = command.ContentType == "image/png" ? ".png" : ".jpg";
        var objectKey = $"avatars/{patient.TenantId}/{command.PatientId}{ext}";

        await _storage.UploadAsync(FileBuckets.Avatars, objectKey, command.FileStream, command.ContentType, cancellationToken);
        var signedUrl = await _storage.GetSignedUrlAsync(FileBuckets.Avatars, objectKey, 900, cancellationToken);

        patient.AvatarUrl = objectKey;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(signedUrl);
    }
}

public class UpdateReceptionNoteCommandHandler : IRequestHandler<UpdateReceptionNoteCommand, Result<PatientResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public UpdateReceptionNoteCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<PatientResponse>> Handle(UpdateReceptionNoteCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

        if (patient is null)
            return Result<PatientResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        patient.ReceptionNote = command.ReceptionNote;
        patient.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("UPDATE_RECEPTION_NOTE", "Patient", patient.Id.ToString(), null, cancellationToken);
        return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));
    }
}

public class AddAllergyCommandHandler : IRequestHandler<AddAllergyCommand, Result<AllergyResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public AddAllergyCommandHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _db = db; _tenant = tenant; _currentUser = currentUser;
    }

    public async Task<Result<AllergyResponse>> Handle(AddAllergyCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<AllergyResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var allergy = new Allergy
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            PatientId = 0,
            Allergen = command.Request.Allergen,
            Reaction = command.Request.Reaction,
            Severity = command.Request.Severity,
            OnsetDate = command.Request.OnsetDate,
            Note = command.Request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Allergies.Add(allergy);
        patient.AllergiesSummary = command.Request.Allergen;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AllergyResponse>.Success(new AllergyResponse(
            allergy.Id, allergy.PatientId, allergy.Allergen, allergy.Reaction,
            allergy.Severity, allergy.OnsetDate, allergy.Note, allergy.CreatedAt));
    }
}

public class DeleteAllergyCommandHandler : IRequestHandler<DeleteAllergyCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;

    public DeleteAllergyCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteAllergyCommand command, CancellationToken cancellationToken)
    {
        var allergy = await _db.Allergies
            .FirstOrDefaultAsync(a => a.Id == command.AllergyId, cancellationToken);

        if (allergy is null)
            return Result<bool>.Failure("ALLERGY_NOT_FOUND", "Không tìm thấy dị ứng");

        allergy.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class AddInsuranceCommandHandler : IRequestHandler<AddInsuranceCommand, Result<InsuranceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _enc;

    public AddInsuranceCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser currentUser, IEncryptionService enc)
    {
        _db = db; _tenant = tenant; _currentUser = currentUser; _enc = enc;
    }

    public async Task<Result<InsuranceResponse>> Handle(AddInsuranceCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        if (req.ValidTo < DateOnly.FromDateTime(DateTime.Today))
            return Result<InsuranceResponse>.Failure("BHYT_EXPIRED", "Thẻ BHYT đã hết hạn");

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<InsuranceResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var enc = _enc.Encrypt(req.CardNo);
        var masked = PatientMappingHelper.MaskCardNo(req.CardNo);
        var now = DateTime.UtcNow;

        var insurance = new Insurance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            PatientId = 0,
            Type = req.Type,
            CardNoEnc = enc,
            CardNoMasked = masked,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            HospitalCode = req.HospitalCode,
            CoveragePercent = req.CoveragePercent,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
            UpdatedAt = now
        };

        _db.Insurances.Add(insurance);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<InsuranceResponse>.Success(new InsuranceResponse(
            insurance.Id, insurance.PatientId, insurance.Type, masked,
            req.ValidFrom, req.ValidTo, req.HospitalCode, req.CoveragePercent, now));
    }
}

public class UpdateInsuranceCommandHandler : IRequestHandler<UpdateInsuranceCommand, Result<InsuranceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEncryptionService _enc;

    public UpdateInsuranceCommandHandler(IApplicationDbContext db, IEncryptionService enc)
    {
        _db = db; _enc = enc;
    }

    public async Task<Result<InsuranceResponse>> Handle(UpdateInsuranceCommand command, CancellationToken cancellationToken)
    {
        var insurance = await _db.Insurances
            .FirstOrDefaultAsync(i => i.Id == command.InsuranceId, cancellationToken);

        if (insurance is null)
            return Result<InsuranceResponse>.Failure("INSURANCE_NOT_FOUND", "Không tìm thấy thẻ bảo hiểm");

        var req = command.Request;
        insurance.CardNoEnc = _enc.Encrypt(req.CardNo);
        insurance.CardNoMasked = PatientMappingHelper.MaskCardNo(req.CardNo);
        insurance.Type = req.Type;
        insurance.ValidFrom = req.ValidFrom;
        insurance.ValidTo = req.ValidTo;
        insurance.HospitalCode = req.HospitalCode;
        insurance.CoveragePercent = req.CoveragePercent;
        insurance.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<InsuranceResponse>.Success(new InsuranceResponse(
            insurance.Id, insurance.PatientId, req.Type, insurance.CardNoMasked,
            req.ValidFrom, req.ValidTo, req.HospitalCode, req.CoveragePercent, DateTime.UtcNow));
    }
}

public class DeleteInsuranceCommandHandler : IRequestHandler<DeleteInsuranceCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;

    public DeleteInsuranceCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteInsuranceCommand command, CancellationToken cancellationToken)
    {
        var insurance = await _db.Insurances
            .FirstOrDefaultAsync(i => i.Id == command.InsuranceId, cancellationToken);

        if (insurance is null)
            return Result<bool>.Failure("INSURANCE_NOT_FOUND", "Không tìm thấy thẻ bảo hiểm");

        insurance.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class AddEmergencyContactCommandHandler : IRequestHandler<AddEmergencyContactCommand, Result<EmergencyContactResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public AddEmergencyContactCommandHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _db = db; _tenant = tenant; _currentUser = currentUser;
    }

    public async Task<Result<EmergencyContactResponse>> Handle(AddEmergencyContactCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<EmergencyContactResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var req = command.Request;
        var contact = new EmergencyContact
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            PatientId = 0,
            FullName = req.FullName,
            Relationship = req.Relationship,
            Phone = req.Phone,
            Address = req.Address,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        _db.EmergencyContacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<EmergencyContactResponse>.Success(new EmergencyContactResponse(
            contact.Id, contact.PatientId, req.FullName, req.Relationship, req.Phone, req.Address));
    }
}

public class UpdateEmergencyContactCommandHandler : IRequestHandler<UpdateEmergencyContactCommand, Result<EmergencyContactResponse>>
{
    private readonly IApplicationDbContext _db;

    public UpdateEmergencyContactCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<EmergencyContactResponse>> Handle(UpdateEmergencyContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await _db.EmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == command.ContactId, cancellationToken);

        if (contact is null)
            return Result<EmergencyContactResponse>.Failure("CONTACT_NOT_FOUND", "Không tìm thấy liên hệ khẩn cấp");

        var req = command.Request;
        contact.FullName = req.FullName;
        contact.Relationship = req.Relationship;
        contact.Phone = req.Phone;
        contact.Address = req.Address;
        contact.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<EmergencyContactResponse>.Success(new EmergencyContactResponse(
            contact.Id, contact.PatientId, req.FullName, req.Relationship, req.Phone, req.Address));
    }
}

public class DeleteEmergencyContactCommandHandler : IRequestHandler<DeleteEmergencyContactCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;

    public DeleteEmergencyContactCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteEmergencyContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await _db.EmergencyContacts
            .FirstOrDefaultAsync(c => c.Id == command.ContactId, cancellationToken);

        if (contact is null)
            return Result<bool>.Failure("CONTACT_NOT_FOUND", "Không tìm thấy liên hệ khẩn cấp");

        contact.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class AddConsentCommandHandler : IRequestHandler<AddConsentCommand, Result<ConsentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _currentUser;

    public AddConsentCommandHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser currentUser)
    {
        _db = db; _tenant = tenant; _currentUser = currentUser;
    }

    public async Task<Result<ConsentResponse>> Handle(AddConsentCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<ConsentResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var req = command.Request;
        var now = DateTime.UtcNow;
        var consent = new Consent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenant.TenantId,
            PatientId = 0,
            ConsentType = req.ConsentType,
            SignedAt = now,
            SignedBy = req.SignedBy,
            DocumentFileId = req.DocumentFileId,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId,
            UpdatedAt = now
        };

        _db.Consents.Add(consent);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ConsentResponse>.Success(new ConsentResponse(
            consent.Id, consent.PatientId, consent.ConsentType, now, req.SignedBy, null, null));
    }
}
