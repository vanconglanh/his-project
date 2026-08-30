using System.Text;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.EMR;

// ────────────────────────────────────────────────
// Get EMR
// ────────────────────────────────────────────────
public class GetEmrQueryHandler : IRequestHandler<GetEmrQuery, Result<EmrContentResponse?>>
{
    private readonly IApplicationDbContext _db;

    public GetEmrQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<EmrContentResponse?>> Handle(GetEmrQuery q, CancellationToken ct)
    {
        var encIdStr = q.EncounterId.ToString();
        var emr = await _db.EmrContents
            .FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);

        if (emr is null) return Result<EmrContentResponse?>.Success(null);

        var sig = await _db.EmrSignatures
            .FirstOrDefaultAsync(s => s.EmrId == emr.Id.ToString(), ct);

        string? signerName = null;
        if (emr.SignedBy is not null)
        {
            var signer = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id.ToString() == emr.SignedBy, ct);
            signerName = signer?.FullName;
        }

        // §5.8.2 — schema snapshot cua ban ghi lay tu phien ban moi nhat; FE render theo cot nay.
        var emrIdStr = emr.Id.ToString();
        var latestVersion = await _db.EmrVersions
            .Where(v => v.EmrId == emrIdStr)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(ct);

        return Result<EmrContentResponse?>.Success(
            MapEmrEntity(emr, sig, signerName, latestVersion?.SchemaSnapshotJson));
    }

    internal static EmrContentResponse MapEmrEntity(EmrContent e, EmrSignature? sig, string? signerName,
        string? schemaSnapshotJson = null)
    {
        SignatureCertDto? cert = sig is null ? null
            : new SignatureCertDto(sig.CertificateSerial, sig.CertificateSubject, sig.SignatureAlgorithm);

        var contentObj = TryParseJson(e.ContentJson) ?? (object)e.ContentJson;

        return new EmrContentResponse(
            e.Id,
            Guid.TryParse(e.EncounterId, out var eid) ? eid : Guid.Empty,
            contentObj,
            e.ContentHtml,
            Guid.TryParse(e.TemplateId, out var tid) ? tid : (Guid?)null,
            e.SignedAt,
            Guid.TryParse(e.SignedBy, out var sbid) ? sbid : (Guid?)null,
            signerName,
            cert,
            e.Version,
            e.UpdatedAt,
            e.UpdatedBy,
            StructuredValues: TryParseJson(e.StructuredValuesJson),
            SchemaSnapshot: TryParseJson(schemaSnapshotJson));
    }

    private static object? TryParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<object>(json); } catch { return null; }
    }
}

// ────────────────────────────────────────────────
// Save Draft
// ────────────────────────────────────────────────
public class SaveEmrDraftCommandHandler : IRequestHandler<SaveEmrDraftCommand, Result<EmrContentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public SaveEmrDraftCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; }

    public async Task<Result<EmrContentResponse>> Handle(SaveEmrDraftCommand cmd, CancellationToken ct)
    {
        var encIdStr = cmd.EncounterId.ToString();

        // Validate encounter exists
        var enc = await _db.Encounters.FirstOrDefaultAsync(e => e.Id.ToString() == encIdStr, ct);
        if (enc is null)
            return Result<EmrContentResponse>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        // Check existing EMR
        var existing = await _db.EmrContents
            .FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);

        if (existing is not null && existing.SignedAt is not null)
            return Result<EmrContentResponse>.Failure("EMR_ALREADY_SIGNED", "Bệnh án đã được ký số, không thể chỉnh sửa");

        var contentJsonStr = JsonSerializer.Serialize(cmd.Request.ContentJson);
        // §5.8.1 (QD4) — gia tri form: giu nguyen chuoi JSON da serialize de hash ky so on dinh (§5.8.3 muc 2).
        var structuredValuesStr = cmd.Request.StructuredValues is null
            ? null
            : JsonSerializer.Serialize(cmd.Request.StructuredValues);

        int newVersion;
        EmrContent emrEntity;

        if (existing is null)
        {
            newVersion = 1;
            emrEntity = new EmrContent
            {
                TenantId             = _tenant.TenantId,
                EncounterId          = encIdStr,
                ContentJson          = contentJsonStr,
                ContentHtml          = cmd.Request.ContentHtml,
                TemplateId           = cmd.Request.TemplateId?.ToString(),
                StructuredValuesJson = structuredValuesStr,
                Version              = 1,
                CreatedBy            = _user.UserId,
            };
            _db.EmrContents.Add(emrEntity);
        }
        else
        {
            newVersion = existing.Version + 1;
            existing.ContentJson = contentJsonStr;
            existing.ContentHtml = cmd.Request.ContentHtml;
            if (cmd.Request.TemplateId.HasValue)
                existing.TemplateId = cmd.Request.TemplateId.Value.ToString();
            if (cmd.Request.StructuredValues is not null)
                existing.StructuredValuesJson = structuredValuesStr;
            existing.Version  = newVersion;
            existing.UpdatedBy = _user.UserId;
            emrEntity = existing;
        }

        await _db.SaveChangesAsync(ct);

        // §5.8.2 (QD5) — CHUP structured_json cua template dang chon vao schema_snapshot_json NGAY khi tao
        // version (khong doi ky). Render benh an LUON theo snapshot nay, khong doc lai template hien tai.
        var effectiveTemplateId = emrEntity.TemplateId;
        string? schemaSnapshotStr = null;
        if (!string.IsNullOrEmpty(effectiveTemplateId) && Guid.TryParse(effectiveTemplateId, out var tplGuid))
        {
            var tpl = await _db.EmrTemplates.FirstOrDefaultAsync(t => t.Id == tplGuid, ct);
            schemaSnapshotStr = tpl?.StructuredJson;
        }

        // Snapshot version — chup content + gia tri form + schema tai thoi diem nay
        var versionEntry = new EmrVersion
        {
            Id                   = Guid.NewGuid(),
            EmrId                = emrEntity.Id.ToString(),
            TenantId             = _tenant.TenantId,
            Version              = newVersion,
            ContentJson          = contentJsonStr,
            TemplateId           = effectiveTemplateId,
            StructuredValuesJson = emrEntity.StructuredValuesJson,
            SchemaSnapshotJson   = schemaSnapshotStr,
            BytesSize            = Encoding.UTF8.GetByteCount(contentJsonStr),
            SavedAt              = DateTime.UtcNow,
            SavedBy              = _user.UserId?.ToString(),
            IsSigned             = false,
        };
        _db.EmrVersions.Add(versionEntry);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("SAVE_DRAFT", "EMR", emrEntity.Id.ToString(), new { version = newVersion }, ct);

        return Result<EmrContentResponse>.Success(
            GetEmrQueryHandler.MapEmrEntity(emrEntity, null, null, schemaSnapshotStr));
    }
}

// ────────────────────────────────────────────────
// Sign
// ────────────────────────────────────────────────
public class SignEmrCommandHandler : IRequestHandler<SignEmrCommand, Result<EmrContentResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;
    private readonly IEmrSignatureVerifier _verifier;

    public SignEmrCommandHandler(IApplicationDbContext db, ITenantProvider tenant,
        ICurrentUser user, IAuditService audit, IEmrSignatureVerifier verifier)
    { _db = db; _tenant = tenant; _user = user; _audit = audit; _verifier = verifier; }

    public async Task<Result<EmrContentResponse>> Handle(SignEmrCommand cmd, CancellationToken ct)
    {
        var encIdStr = cmd.EncounterId.ToString();
        var emr = await _db.EmrContents.FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);
        if (emr is null)
            return Result<EmrContentResponse>.Failure("EMR_NOT_FOUND", "Chưa có bệnh án để ký");
        if (emr.SignedAt is not null)
            return Result<EmrContentResponse>.Failure("EMR_ALREADY_SIGNED", "Bệnh án đã được ký số");

        // §5.8.2/§5.8.3 — lay phien ban moi nhat de dung dung content/gia tri/schema da chup.
        var latestVersion = await _db.EmrVersions
            .Where(v => v.EmrId == emr.Id.ToString())
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(ct);

        var sigBytes = Convert.FromBase64String(cmd.Request.SignatureData);

        // §5.8.3 — hash payload gop 3 phan (v2) cho ban ghi moi; ban ghi cu (ca 2 cot NULL) tu roi ve v1.
        // LUON hash dung chuoi JSON DA LUU (khong serialize lai). Uu tien lay tu version snapshot;
        // fallback ve EmrContent neu chua co version (bao toan luong cu).
        var contentJson         = latestVersion?.ContentJson ?? emr.ContentJson;
        var structuredValues    = latestVersion?.StructuredValuesJson ?? emr.StructuredValuesJson;
        var schemaSnapshot      = latestVersion?.SchemaSnapshotJson;
        var contentBytes = EmrSignPayload.Build(contentJson, structuredValues, schemaSnapshot);

        var verifyResult = await _verifier.VerifyAsync(contentBytes, sigBytes, ct);

        if (!verifyResult.IsValid)
            return Result<EmrContentResponse>.Failure("EMR_SIGNATURE_INVALID",
                verifyResult.ErrorMessage ?? "Chữ ký số không hợp lệ");

        var now    = DateTime.UtcNow;
        var userId = _user.UserId?.ToString() ?? string.Empty;

        emr.SignedAt   = now;
        emr.SignedBy   = userId;
        emr.UpdatedBy  = _user.UserId;

        // Insert signature record
        var sigEntity = new EmrSignature
        {
            Id                 = Guid.NewGuid(),
            TenantId           = _tenant.TenantId,
            EmrId              = emr.Id.ToString(),
            EncounterId        = encIdStr,
            SignedAt           = now,
            SignedBy           = userId,
            CertificateSerial  = verifyResult.CertificateSerial,
            CertificateSubject = verifyResult.CertificateSubject,
            SignatureAlgorithm = cmd.Request.SignatureAlgorithm,
            SignatureData      = sigBytes,
            CreatedAt          = now,
        };
        _db.EmrSignatures.Add(sigEntity);

        // Update latest version snapshot as signed (dung lai latestVersion da lay o tren)
        if (latestVersion is not null)
            latestVersion.IsSigned = true;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("SIGN", "EMR", emr.Id.ToString(), new { encounterId = cmd.EncounterId }, ct);

        string? signerName = null;
        if (_user.UserId.HasValue)
        {
            var signer = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == _user.UserId.Value, ct);
            signerName = signer?.FullName;
        }

        return Result<EmrContentResponse>.Success(GetEmrQueryHandler.MapEmrEntity(emr, sigEntity, signerName));
    }
}

// ────────────────────────────────────────────────
// Unsign
// ────────────────────────────────────────────────
public class UnsignEmrCommandHandler : IRequestHandler<UnsignEmrCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditService _audit;

    public UnsignEmrCommandHandler(IApplicationDbContext db, ICurrentUser user, IAuditService audit)
    { _db = db; _user = user; _audit = audit; }

    public async Task<Result<bool>> Handle(UnsignEmrCommand cmd, CancellationToken ct)
    {
        var encIdStr = cmd.EncounterId.ToString();
        var emr = await _db.EmrContents.FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);

        if (emr is null || emr.SignedAt is null)
            return Result<bool>.Failure("EMR_NOT_SIGNED", "Bệnh án chưa được ký số");

        if ((DateTime.UtcNow - emr.SignedAt.Value).TotalHours > 24)
            return Result<bool>.Failure("EMR_UNSIGN_TIMEOUT", "Chỉ được hủy ký trong vòng 24h sau khi ký");

        emr.SignedAt  = null;
        emr.SignedBy  = null;
        emr.UpdatedBy = _user.UserId;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("UNSIGN", "EMR", emr.Id.ToString(),
            new { reason = cmd.Reason, encounterId = cmd.EncounterId }, ct);

        return Result<bool>.Success(true);
    }
}

// ────────────────────────────────────────────────
// Export PDF
// ────────────────────────────────────────────────
public class ExportEmrPdfCommandHandler : IRequestHandler<ExportEmrPdfCommand, Result<byte[]>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmrPdfExporter _exporter;
    private readonly IDapperConnectionFactory _dapper;

    public ExportEmrPdfCommandHandler(IApplicationDbContext db, IEmrPdfExporter exporter, IDapperConnectionFactory dapper)
    { _db = db; _exporter = exporter; _dapper = dapper; }

    public async Task<Result<byte[]>> Handle(ExportEmrPdfCommand cmd, CancellationToken ct)
    {
        var encIdStr = cmd.EncounterId.ToString();
        var emr = await _db.EmrContents.FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);
        if (emr is null)
            return Result<byte[]>.Failure("EMR_NOT_FOUND", "Không tìm thấy bệnh án");

        var encounter = await _db.Encounters.FirstOrDefaultAsync(e => e.Id.ToString() == encIdStr, ct);
        var sig       = await _db.EmrSignatures.FirstOrDefaultAsync(s => s.EmrId == emr.Id.ToString(), ct);

        string? patientName = null;
        string? doctorName  = null;
        string? signerName  = null;
        Domain.Entities.Patient? patient = null;

        if (encounter is not null)
        {
            patient = await _db.Patients.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id.ToString() == encounter.PatientId, ct);
            patientName = patient?.FullName;

            if (encounter.DoctorId is not null)
            {
                var doctor = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == encounter.DoctorId, ct);
                doctorName = doctor?.FullName;
            }
        }

        if (emr.SignedBy is not null)
        {
            var signer = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id.ToString() == emr.SignedBy, ct);
            signerName = signer?.FullName;
        }

        // Nang cap phieu kham: letterhead + chan doan ICD-10 + sinh hieu (Sprint bieu mau in)
        Reports.LetterheadDto? letterhead = null;
        EmrDiagnosisLine? primaryDx = null;
        List<EmrDiagnosisLine> secondaryDx = new();
        EmrVitalsDto? vitals = null;

        if (encounter is not null)
        {
            using var conn = (System.Data.IDbConnection)_dapper.CreateConnection();
            letterhead = await conn.QueryFirstOrDefaultAsync<Reports.LetterheadDto>(
                @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                         phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                         slogan AS Slogan, website AS Website
                  FROM diab_his_sys_tenants
                  WHERE id = @tenantId",
                new { tenantId = encounter.TenantId });
            letterhead ??= new Reports.LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

            var diagnoses = await _db.EncounterDiagnoses.IgnoreQueryFilters()
                .Where(d => d.EncounterId == encIdStr && d.DeletedAt == null)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync(ct);
            var primary = diagnoses.FirstOrDefault(d => d.Type == DiagnosisType.Primary);
            if (primary is not null)
                primaryDx = new EmrDiagnosisLine(primary.Icd10Code, primary.Name);
            secondaryDx = diagnoses.Where(d => d.Type != DiagnosisType.Primary)
                .Select(d => new EmrDiagnosisLine(d.Icd10Code, d.Name)).ToList();

            var lastVitals = await _db.VitalSigns.IgnoreQueryFilters()
                .Where(v => v.EncounterId == encIdStr && v.DeletedAt == null)
                .OrderByDescending(v => v.RecordedAt).ThenByDescending(v => v.RecordSequence)
                .FirstOrDefaultAsync(ct);
            if (lastVitals is not null)
            {
                vitals = new EmrVitalsDto(
                    lastVitals.TemperatureC, lastVitals.HeartRateBpm, lastVitals.RespiratoryRate,
                    lastVitals.BpSystolic, lastVitals.BpDiastolic, lastVitals.Spo2Percent,
                    lastVitals.WeightKg, lastVitals.HeightCm, lastVitals.GlucoseMgDl);
            }
        }

        var ctx = new EmrPdfContext(
            cmd.EncounterId,
            patientName ?? "",
            doctorName,
            encounter?.CreatedAt ?? emr.CreatedAt,
            emr.ContentHtml ?? emr.ContentJson,
            emr.SignedAt is not null,
            emr.SignedAt,
            signerName,
            sig?.CertificateSerial,
            Letterhead: letterhead,
            PatientCode: patient?.Code,
            PatientGender: patient?.Gender,
            PatientDob: patient?.DateOfBirth,
            PatientAddress: patient?.Street,
            ChiefComplaint: encounter?.ChiefComplaint,
            ReasonForVisit: encounter?.ReasonForVisit,
            PrimaryDiagnosis: primaryDx,
            SecondaryDiagnoses: secondaryDx,
            Vitals: vitals);

        var pdfBytes = await _exporter.ExportAsync(ctx, ct);
        return Result<byte[]>.Success(pdfBytes);
    }
}

// ────────────────────────────────────────────────
// List Versions
// ────────────────────────────────────────────────
public class GetEmrVersionsQueryHandler : IRequestHandler<GetEmrVersionsQuery, Result<IReadOnlyList<EmrVersionMetaDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetEmrVersionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<EmrVersionMetaDto>>> Handle(GetEmrVersionsQuery q, CancellationToken ct)
    {
        var encIdStr = q.EncounterId.ToString();
        var emr = await _db.EmrContents.FirstOrDefaultAsync(e => e.EncounterId == encIdStr, ct);
        if (emr is null)
            return Result<IReadOnlyList<EmrVersionMetaDto>>.Success(Array.Empty<EmrVersionMetaDto>());

        var emrIdStr = emr.Id.ToString();
        var versions = await _db.EmrVersions
            .Where(v => v.EmrId == emrIdStr)
            .OrderByDescending(v => v.Version)
            .ToListAsync(ct);

        var result = new List<EmrVersionMetaDto>();
        foreach (var v in versions)
        {
            string? savedByName = null;
            if (!string.IsNullOrEmpty(v.SavedBy))
            {
                var u = await _db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == v.SavedBy, ct);
                savedByName = u?.FullName;
            }
            result.Add(new EmrVersionMetaDto(
                v.Id,
                v.Version,
                v.SavedAt,
                Guid.TryParse(v.SavedBy, out var sbid) ? sbid : (Guid?)null,
                savedByName,
                v.IsSigned,
                v.BytesSize));
        }

        return Result<IReadOnlyList<EmrVersionMetaDto>>.Success(result.AsReadOnly());
    }
}

// ────────────────────────────────────────────────
// Version Diff
// ────────────────────────────────────────────────
public class GetEmrVersionDiffQueryHandler : IRequestHandler<GetEmrVersionDiffQuery, Result<EmrVersionDiffDto>>
{
    private readonly IApplicationDbContext _db;

    public GetEmrVersionDiffQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<EmrVersionDiffDto>> Handle(GetEmrVersionDiffQuery q, CancellationToken ct)
    {
        var v1 = await _db.EmrVersions.FirstOrDefaultAsync(v => v.Id == q.VersionId, ct);
        if (v1 is null)
            return Result<EmrVersionDiffDto>.Failure("EMR_VERSION_NOT_FOUND", "Không tìm thấy phiên bản");

        var ops = new List<object> { new { op = "replace", path = "/", from = v1.ContentJson } };

        if (q.CompareTo.HasValue)
        {
            var v2 = await _db.EmrVersions.FirstOrDefaultAsync(v => v.Id == q.CompareTo.Value, ct);
            if (v2 is not null)
                ops.Add(new { op = "replace", path = "/", value = v2.ContentJson });
        }

        return Result<EmrVersionDiffDto>.Success(new EmrVersionDiffDto(ops.AsReadOnly()));
    }
}

// ────────────────────────────────────────────────
// Template CRUD
// ────────────────────────────────────────────────
public class ListEmrTemplatesQueryHandler : IRequestHandler<ListEmrTemplatesQuery, Result<IReadOnlyList<EmrTemplateResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDapperConnectionFactory _dapper;

    public ListEmrTemplatesQueryHandler(IApplicationDbContext db, IDapperConnectionFactory dapper)
    { _db = db; _dapper = dapper; }

    public async Task<Result<IReadOnlyList<EmrTemplateResponse>>> Handle(ListEmrTemplatesQuery q, CancellationToken ct)
    {
        var query = _db.EmrTemplates.AsQueryable();

        if (!string.IsNullOrEmpty(q.Speciality)) query = query.Where(e => e.Speciality == q.Speciality);
        if (q.IsSystem.HasValue)                 query = query.Where(e => e.IsSystem   == q.IsSystem.Value);

        var templates = await query
            .OrderByDescending(e => e.IsSystem)
            .ThenBy(e => e.Name)
            .ToListAsync(ct);

        // §5.7.3 — neu loc theo goi benh nhan: mau gan voi goi (bang noi) hien len DAU.
        HashSet<string> packageTemplateIds = new();
        if (q.PackageId.HasValue)
        {
            using var conn = (System.Data.IDbConnection)_dapper.CreateConnection();
            var ids = await Dapper.SqlMapper.QueryAsync<string>(conn,
                @"SELECT template_id FROM diab_his_cli_emr_template_package_map
                  WHERE package_id = @packageId AND deleted_at IS NULL",
                new { packageId = q.PackageId.Value.ToString() });
            packageTemplateIds = ids.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var result = templates
            .OrderByDescending(t => packageTemplateIds.Contains(t.Id.ToString()))
            .ThenByDescending(t => t.IsSystem)
            .ThenBy(t => t.Name)
            .Select(t => MapTemplate(t))
            .ToList();

        return Result<IReadOnlyList<EmrTemplateResponse>>.Success(result.AsReadOnly());
    }

    internal static EmrTemplateResponse MapTemplate(EmrTemplate t) => new(
        t.Id,
        t.TenantId,
        t.Name,
        TryParseJson(t.ContentJson) ?? (object)t.ContentJson,
        t.Speciality,
        t.IsSystem,
        t.CreatedBy,
        t.CreatedAt,
        StructuredJson: TryParseJson(t.StructuredJson),
        IsDefault: t.IsDefault);

    internal static object? TryParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<object>(json); } catch { return null; }
    }

    /// <summary>§5.7.2 — serialize structured_json; null -> null (rong, hop le); loi serialize -> false.</summary>
    internal static bool TrySerializeStructured(object? structured, out string? json)
    {
        json = null;
        if (structured is null) return true;
        try { json = JsonSerializer.Serialize(structured); return true; }
        catch { return false; }
    }
}

// §5.7.3 — GET /api/v1/emr-templates/{id} — tra du structuredJson de FE nap form
public class GetEmrTemplateQueryHandler : IRequestHandler<GetEmrTemplateQuery, Result<EmrTemplateResponse?>>
{
    private readonly IApplicationDbContext _db;

    public GetEmrTemplateQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<EmrTemplateResponse?>> Handle(GetEmrTemplateQuery q, CancellationToken ct)
    {
        var t = await _db.EmrTemplates.FirstOrDefaultAsync(e => e.Id == q.TemplateId, ct);
        return Result<EmrTemplateResponse?>.Success(
            t is null ? null : ListEmrTemplatesQueryHandler.MapTemplate(t));
    }
}

public class CreateEmrTemplateCommandHandler : IRequestHandler<CreateEmrTemplateCommand, Result<EmrTemplateResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly ICurrentUser _user;

    public CreateEmrTemplateCommandHandler(IApplicationDbContext db, ITenantProvider tenant, ICurrentUser user)
    { _db = db; _tenant = tenant; _user = user; }

    public async Task<Result<EmrTemplateResponse>> Handle(CreateEmrTemplateCommand cmd, CancellationToken ct)
    {
        var req    = cmd.Request;
        // §5.7.2 — structured_json (dinh nghia form) — validate la JSON hop le neu khong rong
        if (!ListEmrTemplatesQueryHandler.TrySerializeStructured(req.StructuredJson, out var structuredJsonStr))
            return Result<EmrTemplateResponse>.Failure("EMR_TEMPLATE_STRUCTURED_INVALID",
                "Cấu hình biểu mẫu (structuredJson) không phải JSON hợp lệ");

        var entity = new EmrTemplate
        {
            TenantId       = _tenant.TenantId,
            Name           = req.Name,
            ContentJson    = JsonSerializer.Serialize(req.ContentJson),
            StructuredJson = structuredJsonStr,
            Speciality     = req.Speciality,
            IsSystem       = false,
            IsDefault      = req.IsDefault,
            CreatedBy      = _user.UserId,
        };

        _db.EmrTemplates.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<EmrTemplateResponse>.Success(
            ListEmrTemplatesQueryHandler.MapTemplate(entity));
    }
}

public class UpdateEmrTemplateCommandHandler : IRequestHandler<UpdateEmrTemplateCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public UpdateEmrTemplateCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<bool>> Handle(UpdateEmrTemplateCommand cmd, CancellationToken ct)
    {
        var entity = await _db.EmrTemplates.FirstOrDefaultAsync(e => e.Id == cmd.TemplateId, ct);
        if (entity is null)
            return Result<bool>.Failure("TEMPLATE_NOT_FOUND", "Không tìm thấy mẫu bệnh án");

        // §5.7.2 — validate structured_json
        if (!ListEmrTemplatesQueryHandler.TrySerializeStructured(cmd.Request.StructuredJson, out var structuredJsonStr))
            return Result<bool>.Failure("EMR_TEMPLATE_STRUCTURED_INVALID",
                "Cấu hình biểu mẫu (structuredJson) không phải JSON hợp lệ");

        entity.Name           = cmd.Request.Name;
        entity.ContentJson    = JsonSerializer.Serialize(cmd.Request.ContentJson);
        entity.StructuredJson = structuredJsonStr;
        entity.Speciality     = cmd.Request.Speciality;
        entity.IsDefault      = cmd.Request.IsDefault;
        entity.UpdatedBy      = _user.UserId;

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class DeleteEmrTemplateCommandHandler : IRequestHandler<DeleteEmrTemplateCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _user;

    public DeleteEmrTemplateCommandHandler(IApplicationDbContext db, ICurrentUser user)
    { _db = db; _user = user; }

    public async Task<Result<bool>> Handle(DeleteEmrTemplateCommand cmd, CancellationToken ct)
    {
        var entity = await _db.EmrTemplates.FirstOrDefaultAsync(e => e.Id == cmd.TemplateId, ct);
        if (entity is null)
            return Result<bool>.Failure("TEMPLATE_NOT_FOUND", "Không tìm thấy mẫu bệnh án");
        if (entity.IsSystem)
            return Result<bool>.Failure("TEMPLATE_SYSTEM", "Không thể xóa mẫu bệnh án hệ thống");

        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = _user.UserId;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}
