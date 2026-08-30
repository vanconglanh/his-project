using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Patients;

/// <summary>
/// BR-DUP-001..005: check trung theo blind-index/hash CCCD, phan biet 3 case va tra ve
/// danh sach truong lech cu the (Case 3) de FE hien thi dialog so sanh.
/// </summary>
public class CheckCccdDuplicateQueryHandler : IRequestHandler<CheckCccdDuplicateQuery, Result<CccdDuplicateCheckResult>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;

    public CheckCccdDuplicateQueryHandler(IApplicationDbContext db, ITenantProvider tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<CccdDuplicateCheckResult>> Handle(CheckCccdDuplicateQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.IdNumber))
            return Result<CccdDuplicateCheckResult>.Failure("CCCD_REQUIRED", "Thiếu số CCCD để kiểm tra trùng");

        var idHash = PatientMappingHelper.ComputeIdNumberHash(query.IdNumber);
        var patient = await _db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.IdNumberHash == idHash, cancellationToken);

        if (patient is null)
            return Result<CccdDuplicateCheckResult>.Success(
                new CccdDuplicateCheckResult(CccdDuplicateCase.None, null, null, null, null, new List<CccdFieldDiff>()));

        var diffs = BuildDiffs(patient, query);
        var resultCase = diffs.Count == 0 ? CccdDuplicateCase.ExactMatch : CccdDuplicateCase.FieldMismatch;

        return Result<CccdDuplicateCheckResult>.Success(new CccdDuplicateCheckResult(
            resultCase, patient.Id, patient.Code, patient.FullName, patient.DateOfBirth, diffs));
    }

    /// <summary>BR-DUP-005: normalize truoc khi so sanh (trim + lowercase), giu nguyen dinh dang goc de hien thi</summary>
    public static List<CccdFieldDiff> BuildDiffs(Patient patient, CheckCccdDuplicateQuery query)
    {
        var diffs = new List<CccdFieldDiff>();

        void Compare(string field, string? oldVal, string? newVal)
        {
            if (!NormalizeEqual(oldVal, newVal))
                diffs.Add(new CccdFieldDiff(field, oldVal, newVal));
        }

        Compare(CccdComparableField.FullName, patient.FullName, query.FullName);
        Compare(CccdComparableField.Gender, patient.Gender, query.Gender);
        Compare(CccdComparableField.DateOfBirth,
            patient.DateOfBirth?.ToString("dd/MM/yyyy"), query.DateOfBirth?.ToString("dd/MM/yyyy"));
        Compare(CccdComparableField.Address, patient.Street, query.Address);

        return diffs;
    }

    private static bool NormalizeEqual(string? a, string? b) => Normalize(a) == Normalize(b);

    private static string Normalize(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? string.Empty
            : string.Join(' ', s.Trim().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>
/// BR-DUP-004, BR-AUDIT-001..004: cap nhat CHỈ cac truong lien tan da tich chon,
/// ghi audit log rieng cho tung truong voi source = "CCCD_QR_SCAN". Patient update
/// va audit log entries duoc them vao cung 1 DbContext va luu trong 1 SaveChangesAsync
/// duy nhat -> neu insert audit that bai, toan bo transaction rollback (BR-AUDIT-003).
/// </summary>
public class ApplyCccdFieldUpdatesCommandHandler : IRequestHandler<ApplyCccdFieldUpdatesCommand, Result<PatientResponse>>
{
    public const string AuditSource = "CCCD_QR_SCAN";

    private static readonly HashSet<string> AllowedFields = new()
    {
        CccdComparableField.FullName,
        CccdComparableField.Gender,
        CccdComparableField.DateOfBirth,
        CccdComparableField.Address
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantProvider _tenant;

    public ApplyCccdFieldUpdatesCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, ITenantProvider tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<Result<PatientResponse>> Handle(ApplyCccdFieldUpdatesCommand command, CancellationToken cancellationToken)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
            return Result<PatientResponse>.Failure("PATIENT_NOT_FOUND", "Không tìm thấy bệnh nhân");

        var validItems = (command.Fields ?? new List<CccdFieldUpdateItem>())
            .Where(f => AllowedFields.Contains(f.Field))
            .ToList();

        // US-QR-005: khong tich gi -> khong thay doi DB, khong ghi audit log
        if (validItems.Count == 0)
            return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));

        var now = DateTime.UtcNow;
        foreach (var item in validItems)
        {
            string? oldValue = ApplyField(patient, item);

            _db.AuditLogs.Add(new AuditLog
            {
                TenantId = _tenant.TenantId,
                UserId = _currentUser.UserId,
                Action = "UPDATE",
                ResourceType = "Patient",
                ResourceId = patient.Id.ToString(),
                DetailsJson = JsonSerializer.Serialize(new
                {
                    field = item.Field,
                    oldValue,
                    newValue = item.NewValue,
                    source = AuditSource
                }),
                Severity = "INFO",
                CreatedAt = now
            });
        }

        patient.UpdatedBy = _currentUser.UserId;
        patient.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<PatientResponse>.Success(PatientEntityMapper.ToResponse(patient));
    }

    private static string? ApplyField(Patient patient, CccdFieldUpdateItem item)
    {
        switch (item.Field)
        {
            case CccdComparableField.FullName:
                var oldName = patient.FullName;
                patient.FullName = item.NewValue;
                return oldName;

            case CccdComparableField.Gender:
                var oldGender = patient.Gender;
                patient.Gender = item.NewValue;
                return oldGender;

            case CccdComparableField.DateOfBirth:
                var oldDob = patient.DateOfBirth?.ToString("dd/MM/yyyy");
                if (DateOnly.TryParseExact(item.NewValue, "dd/MM/yyyy", out var dob))
                    patient.DateOfBirth = dob;
                return oldDob;

            case CccdComparableField.Address:
                var oldAddress = patient.Street;
                patient.Street = item.NewValue;
                return oldAddress;

            default:
                return null;
        }
    }
}
