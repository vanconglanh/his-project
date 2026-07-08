using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.CLS;

// ────────────── Queries ──────────────
public record GetLabOrdersPdfQuery(Guid EncounterId) : IRequest<Result<byte[]>>;
public record GetRadOrdersPdfQuery(Guid EncounterId) : IRequest<Result<byte[]>>;

internal class ClsSlipEncounterRow
{
    public string? PatientCode { get; set; }
    public string? PatientFullName { get; set; }
    public string? PatientGender { get; set; }
    public DateTime? PatientDob { get; set; }
    public string? DoctorFullName { get; set; }
    public string? EncounterNo { get; set; }
}

/// <summary>Helper dung chung: lay thong tin lượt khám + benh nhan + bac si + chan doan chinh + letterhead.</summary>
internal static class ClsPdfQueryHelper
{
    public static async Task<(ClsSlipEncounterRow? enc, string? dxCode, string? dxName, LetterheadDto lh)> LoadCommonAsync(
        System.Data.IDbConnection conn, string encId, int tenantId)
    {
        var enc = await conn.QueryFirstOrDefaultAsync<ClsSlipEncounterRow>(
            @"SELECT pat.code AS PatientCode, pat.full_name AS PatientFullName, pat.gender AS PatientGender,
                     pat.date_of_birth AS PatientDob, doc.full_name AS DoctorFullName, e.encounter_no AS EncounterNo
              FROM diab_his_enc_encounters e
              LEFT JOIN diab_his_pat_patients pat ON pat.id = e.patient_id AND pat.tenant_id = e.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
              WHERE e.id = @encId AND e.tenant_id = @tenantId AND e.deleted_at IS NULL",
            new { encId, tenantId });

        (string? dxCode, string? dxName) dx = (null, null);
        if (enc != null)
        {
            var dxRow = await conn.QueryFirstOrDefaultAsync<(string icd10_code, string name)?>(
                @"SELECT icd10_code, name FROM diab_his_enc_diagnoses
                  WHERE encounter_id = @encId AND tenant_id = @tenantId AND type = 'PRIMARY' AND deleted_at IS NULL
                  ORDER BY created_at LIMIT 1",
                new { encId, tenantId });
            if (dxRow != null) dx = (dxRow.Value.icd10_code, dxRow.Value.name);
        }

        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });
        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        return (enc, dx.dxCode, dx.dxName, lh);
    }
}

// ────────────────────────────────────────────────
// Phieu chi dinh Xet nghiem
// ────────────────────────────────────────────────
public class GetLabOrdersPdfQueryHandler : IRequestHandler<GetLabOrdersPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IClsOrderSlipPdfBuilder _builder;

    public GetLabOrdersPdfQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IClsOrderSlipPdfBuilder builder)
    { _db = db; _tenant = tenant; _builder = builder; }

    public async Task<Result<byte[]>> Handle(GetLabOrdersPdfQuery q, CancellationToken ct)
    {
        using var conn = (System.Data.IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var encId = q.EncounterId.ToString();

        var (enc, dxCode, dxName, lh) = await ClsPdfQueryHelper.LoadCommonAsync(conn, encId, tenantId);
        if (enc == null)
            return Result<byte[]>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var rows = await conn.QueryAsync<(string test_name, string? sample_type, string priority, string? note)>(
            @"SELECT test_name, sample_type, priority, note FROM diab_his_cli_lab_orders
              WHERE encounter_id = @encId AND tenant_id = @tenantId AND deleted_at IS NULL
              ORDER BY created_at",
            new { encId, tenantId });

        var items = rows.Select((r, i) => new ClsOrderSlipItemDto(
            i + 1, r.test_name,
            string.IsNullOrWhiteSpace(r.sample_type) ? null : $"Mẫu bệnh phẩm: {r.sample_type}",
            r.priority, r.note)).ToList();

        if (items.Count == 0)
            return Result<byte[]>.Failure("LAB_ORDER_EMPTY", "Chưa có chỉ định xét nghiệm cho lượt khám này");

        var data = new ClsOrderSlipData(
            lh, "PHIẾU CHỈ ĐỊNH XÉT NGHIỆM", "(Xét nghiệm)",
            enc.EncounterNo ?? q.EncounterId.ToString("N")[..8].ToUpper(),
            DateTime.Now,
            enc.PatientCode ?? "", enc.PatientFullName ?? "", enc.PatientGender,
            enc.PatientDob.HasValue ? DateOnly.FromDateTime(enc.PatientDob.Value) : null,
            dxCode, dxName, enc.DoctorFullName, items);

        var pdf = _builder.BuildLabOrderSlip(data);
        return Result<byte[]>.Success(pdf);
    }
}

// ────────────────────────────────────────────────
// Phieu chi dinh Chan doan hinh anh
// ────────────────────────────────────────────────
public class GetRadOrdersPdfQueryHandler : IRequestHandler<GetRadOrdersPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IClsOrderSlipPdfBuilder _builder;

    public GetRadOrdersPdfQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IClsOrderSlipPdfBuilder builder)
    { _db = db; _tenant = tenant; _builder = builder; }

    public async Task<Result<byte[]>> Handle(GetRadOrdersPdfQuery q, CancellationToken ct)
    {
        using var conn = (System.Data.IDbConnection)_db.CreateConnection();
        var tenantId = _tenant.TenantId;
        var encId = q.EncounterId.ToString();

        var (enc, dxCode, dxName, lh) = await ClsPdfQueryHelper.LoadCommonAsync(conn, encId, tenantId);
        if (enc == null)
            return Result<byte[]>.Failure("ENCOUNTER_NOT_FOUND", "Không tìm thấy lượt khám");

        var rows = await conn.QueryAsync<(string procedure_name, string modality, string? body_part, bool contrast, string priority, string? note)>(
            @"SELECT procedure_name, modality, body_part, contrast, priority, note FROM diab_his_cli_rad_orders
              WHERE encounter_id = @encId AND tenant_id = @tenantId AND deleted_at IS NULL
              ORDER BY created_at",
            new { encId, tenantId });

        var items = rows.Select((r, i) =>
        {
            var detailParts = new List<string> { $"Kỹ thuật: {r.modality}" };
            if (!string.IsNullOrWhiteSpace(r.body_part)) detailParts.Add($"Vùng chụp: {r.body_part}");
            detailParts.Add(r.contrast ? "Có thuốc cản quang" : "Không thuốc cản quang");
            return new ClsOrderSlipItemDto(i + 1, r.procedure_name, string.Join(" • ", detailParts), r.priority, r.note);
        }).ToList();

        if (items.Count == 0)
            return Result<byte[]>.Failure("RAD_ORDER_EMPTY", "Chưa có chỉ định CĐHA cho lượt khám này");

        var data = new ClsOrderSlipData(
            lh, "PHIẾU CHỈ ĐỊNH CHẨN ĐOÁN HÌNH ẢNH", "(Chẩn đoán hình ảnh)",
            enc.EncounterNo ?? q.EncounterId.ToString("N")[..8].ToUpper(),
            DateTime.Now,
            enc.PatientCode ?? "", enc.PatientFullName ?? "", enc.PatientGender,
            enc.PatientDob.HasValue ? DateOnly.FromDateTime(enc.PatientDob.Value) : null,
            dxCode, dxName, enc.DoctorFullName, items);

        var pdf = _builder.BuildRadOrderSlip(data);
        return Result<byte[]>.Success(pdf);
    }
}
