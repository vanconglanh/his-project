using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Cdss;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Telehealth;
using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Pharmacy.Prescriptions;

// ═══════════════════════════════════════════════════════════════════════════════
// Commands & Queries
// ═══════════════════════════════════════════════════════════════════════════════

public record ListPrescriptionsQuery(
    string? Status, Guid? PatientId, Guid? EncounterId, Guid? DoctorId,
    DateOnly? FromDate, DateOnly? ToDate, string? Q, int Page, int PageSize)
    : IRequest<Result<PagedResult<PrescriptionResponse>>>;

public record GetPrescriptionQuery(Guid Id) : IRequest<Result<PrescriptionResponse>>;

public record CreatePrescriptionCommand(PrescriptionCreateRequest Request)
    : IRequest<Result<PrescriptionResponse>>, IEncounterScopedCommand
{
    public Guid EncounterId => Request.EncounterId;
}

public record UpdatePrescriptionCommand(Guid Id, PrescriptionUpdateRequest Request)
    : IRequest<Result<PrescriptionResponse>>, IEncounterChildScopedCommand
{
    public Guid ChildId => Id;
    public string ChildKind => EncounterChildKind.Prescription;
}

public record DeletePrescriptionCommand(Guid Id) : IRequest<Result<bool>>, IEncounterChildScopedCommand
{
    public Guid ChildId => Id;
    public string ChildKind => EncounterChildKind.Prescription;
}

public record AddPrescriptionItemsCommand(Guid PrescriptionId, IReadOnlyList<PrescriptionItemRequest> Items)
    : IRequest<Result<IReadOnlyList<PrescriptionItemResponse>>>, IEncounterChildScopedCommand
{
    public Guid ChildId => PrescriptionId;
    public string ChildKind => EncounterChildKind.Prescription;
}

public record RemovePrescriptionItemCommand(Guid PrescriptionId, Guid ItemId)
    : IRequest<Result<bool>>, IEncounterChildScopedCommand
{
    public Guid ChildId => PrescriptionId;
    public string ChildKind => EncounterChildKind.Prescription;
}

public record SignPrescriptionCommand(Guid Id, SignPrescriptionRequest Request)
    : IRequest<Result<PrescriptionResponse>>;

public record CancelPrescriptionCommand(Guid Id, string Reason)
    : IRequest<Result<PrescriptionResponse>>;

public record CheckDdiQuery(Guid PrescriptionId) : IRequest<Result<DdiCheckResponse>>;

public record GetPrescriptionQrQuery(Guid Id) : IRequest<Result<byte[]>>;

public record GetPrescriptionPdfQuery(Guid Id) : IRequest<Result<byte[]>>;

/// <summary>
/// Query rieng cho Patient Portal: giong GetPrescriptionPdfQuery nhung BAT BUOC loc them
/// theo PatientId (tu JWT claim "patient_id" cua PortalBearer) de benh nhan KHONG xem duoc
/// don thuoc cua benh nhan khac cung tenant.
/// </summary>
public record GetPortalPrescriptionPdfQuery(Guid PrescriptionId, Guid PatientId, int TenantId) : IRequest<Result<byte[]>>;

public record GetPrintHistoryQuery(Guid PrescriptionId) : IRequest<Result<IReadOnlyList<PrintHistoryItem>>>;

// ═══════════════════════════════════════════════════════════════════════════════
// Handlers
// ═══════════════════════════════════════════════════════════════════════════════

public class ListPrescriptionsHandler : IRequestHandler<ListPrescriptionsQuery, Result<PagedResult<PrescriptionResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public ListPrescriptionsHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<PrescriptionResponse>>> Handle(ListPrescriptionsQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var offset = (q.Page - 1) * q.PageSize;

        var where = new List<string> { "p.tenant_id = @tenantId", "p.deleted_at IS NULL" };
        var prm = new DynamicParameters();
        prm.Add("tenantId", tenantId);
        prm.Add("offset", offset);
        prm.Add("limit", q.PageSize);

        if (!string.IsNullOrWhiteSpace(q.Status)) { where.Add("p.status = @status"); prm.Add("status", q.Status); }
        if (q.PatientId.HasValue) { where.Add("p.patient_id = @patientId"); prm.Add("patientId", q.PatientId.Value.ToString()); }
        if (q.EncounterId.HasValue) { where.Add("p.encounter_id = @encounterId"); prm.Add("encounterId", q.EncounterId.Value.ToString()); }
        if (q.FromDate.HasValue) { where.Add("DATE(p.created_at) >= @fromDate"); prm.Add("fromDate", q.FromDate.Value); }
        if (q.ToDate.HasValue) { where.Add("DATE(p.created_at) <= @toDate"); prm.Add("toDate", q.ToDate.Value); }

        var whereClause = string.Join(" AND ", where);

        var countSql = $"SELECT COUNT(*) FROM diab_his_pha_prescriptions p WHERE {whereClause}";
        var total = await conn.ExecuteScalarAsync<int>(countSql, prm);

        var sql = $@"
            SELECT p.id as Id, p.tenant_id as TenantId, p.encounter_id as EncounterId,
                   p.patient_id as PatientId, p.doctor_id as DoctorId,
                   p.status as Status, p.created_at as PrescribedAt,
                   p.signed_at as SignedAt, NULL as SignedBy,
                   p.dtqg_code as DtqgCode, NULL as DtqgStatus,
                   (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                     WHERE i.prescription_id = p.id AND i.deleted_at IS NULL) as TotalAmount, p.note as Note,
                   p.created_at as CreatedAt, p.updated_at as UpdatedAt
            FROM diab_his_pha_prescriptions p
            WHERE {whereClause}
            ORDER BY p.created_at DESC
            LIMIT @limit OFFSET @offset";

        var rowList = (await conn.QueryAsync<PrescriptionRow>(sql, prm)).ToList();

        // BUG FIX (BUG-09): list truoc day khong tra patient_summary khien FE khong
        // hien duoc ten benh nhan o /prescriptions. Batch load 1 lan cho ca trang.
        var patientIds = rowList
            .Select(r => r.PatientId?.ToString())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        var patientMap = new Dictionary<string, PatientSummary>();
        if (patientIds.Count > 0)
        {
            // BUG FIX (BUG-01): bhyt_card_no_masked khong ton tai o bang diab_his_pat_patients,
            // ma nam o bang diab_his_pat_insurances (cot card_no_masked). Lay so the BHYT
            // (con hieu luc gan nhat) qua correlated subquery de tranh JOIN N-N lam trung dong.
            var patientRows = await conn.QueryAsync<dynamic>(
                @"SELECT p.id, p.full_name, p.gender, p.date_of_birth AS dob,
                         (SELECT i.card_no_masked
                          FROM diab_his_pat_insurances i
                          WHERE i.patient_id = p.id AND i.deleted_at IS NULL
                          ORDER BY i.valid_to DESC
                          LIMIT 1) AS bhyt_card_no_masked
                  FROM diab_his_pat_patients p
                  WHERE p.id IN @ids AND p.tenant_id = @tenantId AND p.deleted_at IS NULL",
                new { ids = patientIds, tenantId = _currentUser.TenantId!.Value });
            foreach (var pr in patientRows)
            {
                patientMap[(string)pr.id] = new PatientSummary(
                    (string)pr.full_name,
                    (string?)pr.gender,
                    pr.dob == null ? null : DateOnly.FromDateTime((DateTime)pr.dob),
                    (string?)pr.bhyt_card_no_masked);
            }
        }

        // BUG FIX: doctor_name chua tung duoc query o day (luon truyen null cho FE) - batch load
        // ten bac si giong cach da lam voi patient_summary (BUG-09) o tren.
        var doctorIds = rowList
            .Select(r => r.DoctorId?.ToString())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
        var doctorMap = new Dictionary<string, string>();
        if (doctorIds.Count > 0)
        {
            var doctorRows = await conn.QueryAsync<dynamic>(
                "SELECT id, full_name FROM diab_his_sec_users WHERE id IN @ids AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { ids = doctorIds, tenantId = _currentUser.TenantId!.Value });
            foreach (var dr in doctorRows)
                doctorMap[(string)dr.id] = (string)dr.full_name;
        }

        // BUG FIX (phát hiện khi test data migrate): list truoc day LUON tra "items: []"
        // cho moi don thuoc (hardcode) — man /prescriptions hien duoc nhung tab "Don thuoc"
        // trong Kham benh (goi list voi page_size=1 de lay 1 don) luon thay "Chua co don
        // thuoc" du DB co du du lieu. Batch load items giong pattern patient/doctor o tren.
        var presIds = rowList.Select(r => r.Id?.ToString()).Where(id => !string.IsNullOrEmpty(id)).ToList();
        var itemsByPres = new Dictionary<string, List<PrescriptionItemResponse>>();
        if (presIds.Count > 0)
        {
            var itemRows = await conn.QueryAsync<PrescriptionItemRow>(
                @"SELECT i.id as Id, i.prescription_id as PrescriptionId, i.drug_id as DrugId, d.name as DrugName,
                         d.strength as Strength, d.unit as Unit,
                         i.dosage as Dosage, i.frequency as Frequency, i.route as Route,
                         i.duration_days as DurationDays, i.quantity as Quantity,
                         i.note as Instructions, NULL as BatchDispensedJson
                  FROM diab_his_pha_prescription_items i
                  JOIN diab_his_pha_drugs d ON d.id = i.drug_id
                  WHERE i.prescription_id IN @presIds AND i.tenant_id = @tenantId AND i.deleted_at IS NULL",
                new { presIds, tenantId });
            foreach (var ir in itemRows)
            {
                if (!itemsByPres.TryGetValue(ir.PrescriptionId, out var list))
                    itemsByPres[ir.PrescriptionId] = list = new List<PrescriptionItemResponse>();
                list.Add(GetPrescriptionHandler.MapItem(ir));
            }
        }

        var items = rowList.Select(r =>
        {
            var pid = r.PatientId?.ToString();
            var patient = pid != null && patientMap.TryGetValue(pid, out var p) ? p : null;
            var did = r.DoctorId?.ToString();
            var doctorName = did != null && doctorMap.TryGetValue(did, out var dn) ? dn : null;
            var presId = r.Id?.ToString();
            var presItems = presId != null && itemsByPres.TryGetValue(presId, out var pi)
                ? (IReadOnlyList<PrescriptionItemResponse>)pi : [];
            return MapToResponse(r, patient, doctorName, presItems, []);
        }).ToList();
        return Result<PagedResult<PrescriptionResponse>>.Success(
            new PagedResult<PrescriptionResponse>(items, q.Page, q.PageSize, total));
    }

    private static PrescriptionResponse MapToResponse(PrescriptionRow r, PatientSummary? patient,
        string? doctorName, IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(
            Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId,
            // BUG FIX: truoc day hard-code Guid.Empty cho ca encounter_id/patient_id khien response
            // luon tra ve GUID rong du DB da luu dung - dung gia tri that tu row (Guid hoac string
            // tuy MySqlConnector suy dien, xem GuidFormat=None o Infrastructure/DependencyInjection.cs).
            Guid.TryParse(r.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(r.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            patient,
            Guid.TryParse(r.DoctorId?.ToString(), out var dg) ? dg : null,
            doctorName,
            r.Status ?? "DRAFT", r.PrescribedAt,
            r.SignedAt, r.SignedBy, r.DtqgCode, r.DtqgStatus ?? "NONE",
            items, warnings, r.TotalAmount, r.Note, r.CreatedAt, r.UpdatedAt);
}

public class GetPrescriptionHandler : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public GetPrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<PrescriptionResponse>> Handle(GetPrescriptionQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pres = await conn.QueryFirstOrDefaultAsync<PrescriptionRow>(
            @"SELECT p.id as Id, p.tenant_id as TenantId, p.encounter_id as EncounterId,
                     p.patient_id as PatientId, p.doctor_id as DoctorId,
                     p.status as Status, p.created_at as PrescribedAt,
                     p.signed_at as SignedAt, NULL as SignedBy,
                     p.dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                     WHERE i.prescription_id = p.id AND i.deleted_at IS NULL) as TotalAmount, p.note as Note,
                     p.created_at as CreatedAt, p.updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions p
              WHERE p.id = @id AND p.tenant_id = @tenantId AND p.deleted_at IS NULL",
            new { id = q.Id.ToString(), tenantId });

        if (pres == null)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        // BUG FIX: man chi tiet /prescriptions/[id] truoc day luon hien "Thong tin benh nhan"
        // rong (Ho ten/Gioi tinh/Ngay sinh/Bac si ke deu trong) vi handler nay chua tung query
        // patient_summary/doctor_name - chi ListPrescriptionsHandler (BUG-09) duoc fix truoc do.
        PatientSummary? patient = null;
        if (pres.PatientId != null)
        {
            var pr = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT p.full_name, p.gender, p.date_of_birth AS dob,
                         (SELECT i.card_no_masked FROM diab_his_pat_insurances i
                          WHERE i.patient_id = p.id AND i.deleted_at IS NULL
                          ORDER BY i.valid_to DESC LIMIT 1) AS bhyt_card_no_masked
                  FROM diab_his_pat_patients p
                  WHERE p.id = @id AND p.tenant_id = @tenantId AND p.deleted_at IS NULL",
                new { id = pres.PatientId.ToString(), tenantId });
            if (pr != null)
            {
                patient = new PatientSummary(
                    (string)pr.full_name,
                    (string?)pr.gender,
                    pr.dob == null ? null : DateOnly.FromDateTime((DateTime)pr.dob),
                    (string?)pr.bhyt_card_no_masked);
            }
        }

        string? doctorName = null;
        if (pres.DoctorId != null)
        {
            doctorName = await conn.ExecuteScalarAsync<string?>(
                "SELECT full_name FROM diab_his_sec_users WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
                new { id = pres.DoctorId.ToString(), tenantId });
        }

        var items = await conn.QueryAsync<PrescriptionItemRow>(
            @"SELECT i.id as Id, i.drug_id as DrugId, d.name as DrugName,
                     d.strength as Strength, d.unit as Unit,
                     i.dosage as Dosage, i.frequency as Frequency, i.route as Route,
                     i.duration_days as DurationDays, i.quantity as Quantity,
                     i.note as Instructions, NULL as BatchDispensedJson
              FROM diab_his_pha_prescription_items i
              JOIN diab_his_pha_drugs d ON d.id = i.drug_id
              WHERE i.prescription_id = @presId AND i.tenant_id = @tenantId AND i.deleted_at IS NULL",
            new { presId = pres.Id, tenantId });

        var itemResponses = items.Select(MapItem).ToList();
        var response = MapPresRow(pres, patient, doctorName, itemResponses, []);

        // P0-01: ghi nhat ky truy cap (doc) don thuoc - yeu cau tuan thu TT 13/2025/TT-BYT
        await _audit.LogAsync(AuditAction.View, "Prescription", response.Id.ToString(), null, ct);

        return Result<PrescriptionResponse>.Success(response);
    }

    // internal (khong private) de ListPrescriptionsHandler tai su dung khi batch-load items.
    internal static PrescriptionItemResponse MapItem(PrescriptionItemRow r) =>
        new(Guid.TryParse(r.Id, out var g) ? g : Guid.Empty,
            r.DrugId, r.DrugName ?? "", r.Strength, r.Unit,
            r.Dosage, r.Frequency, r.Route, r.DurationDays, r.Quantity,
            r.Instructions, null);

    private static PrescriptionResponse MapPresRow(PrescriptionRow r, PatientSummary? patient,
        string? doctorName, IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId,
            Guid.TryParse(r.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(r.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            patient,
            Guid.TryParse(r.DoctorId?.ToString(), out var dg) ? dg : null,
            doctorName,
            r.Status ?? "DRAFT", r.PrescribedAt,
            r.SignedAt, r.SignedBy, r.DtqgCode, r.DtqgStatus ?? "NONE",
            items, warnings, r.TotalAmount, r.Note, r.CreatedAt, r.UpdatedAt);
}

public class CreatePrescriptionHandler : IRequestHandler<CreatePrescriptionCommand, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly ProDiabHis.Application.Common.Interfaces.IPackageEntitlementService _packageEntitlement;

    public CreatePrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit,
        ProDiabHis.Application.Common.Interfaces.IPackageEntitlementService packageEntitlement)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _packageEntitlement = packageEntitlement;
    }

    public async Task<Result<PrescriptionResponse>> Handle(CreatePrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId;

        // FR-803: ke don tu context telehealth BAT BUOC gan voi 1 encounter loai telehealth
        // (diab_his_enc_encounters.telehealth_session_id KHONG NULL) - khong tin co client, luon
        // kiem tra lai o server truoc khi tao don.
        if (cmd.Request.IsTelehealthContext)
        {
            var encounter = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT id, telehealth_session_id FROM diab_his_enc_encounters
                  WHERE id=@EncId AND tenant_id=@TenantId AND deleted_at IS NULL",
                new { EncId = cmd.Request.EncounterId.ToString(), TenantId = tenantId });

            if (encounter is null || encounter.telehealth_session_id is null)
            {
                return Result<PrescriptionResponse>.Failure("TELEHEALTH_ENCOUNTER_REQUIRED",
                    "Đơn thuốc tư vấn từ xa bắt buộc phải gắn với một lượt khám telehealth hợp lệ (thiếu encounterId hoặc lượt khám không phải telehealth)");
            }

            // FR-804: chan cung neu chan doan chinh cua encounter nam ngoai danh muc ICD-10
            // duoc phep tu van tu xa (danh muc configurable qua Admin API, khong hardcode).
            var primaryIcd10 = await conn.ExecuteScalarAsync<string?>(
                @"SELECT icd10_code FROM diab_his_enc_diagnoses
                  WHERE encounter_id=@EncId AND tenant_id=@TenantId AND type='PRIMARY' AND deleted_at IS NULL
                  ORDER BY created_at DESC LIMIT 1",
                new { EncId = cmd.Request.EncounterId.ToString(), TenantId = tenantId });

            if (!string.IsNullOrWhiteSpace(primaryIcd10))
            {
                var allowed = await TelehealthIcd10Guard.IsAllowedAsync(_db, tenantId, primaryIcd10, ct);
                if (!allowed)
                {
                    return Result<PrescriptionResponse>.Failure("TELEHEALTH_ICD10_NOT_ALLOWED",
                        $"Mã chẩn đoán '{primaryIcd10}' không nằm trong danh mục được phép tư vấn từ xa. Vui lòng chỉ định bệnh nhân khám trực tiếp.");
                }
            }
        }

        var presId = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pha_prescriptions
              (id, tenant_id, encounter_id, patient_id, doctor_id, status, note, created_at, updated_at, created_by)
              VALUES (@presId, @tenantId, @encounterId, @patientId, @doctorId, 'DRAFT', @note, NOW(), NOW(), @createdBy)",
            new
            {
                presId,
                tenantId,
                encounterId = cmd.Request.EncounterId.ToString(),
                patientId = cmd.Request.PatientId.ToString(),
                doctorId = userId?.ToString(),
                note = cmd.Request.Note,
                createdBy = userId?.ToString()
            });

        var packageLines = new List<ProDiabHis.Application.Common.Interfaces.PackageCoverageLineRequest>();
        if (cmd.Request.Items?.Count > 0)
        {
            foreach (var item in cmd.Request.Items)
            {
                // Cot thuc te cua diab_his_pha_prescription_items (verify qua information_schema 2026-08-29):
                // id, tenant_id, prescription_id, drug_id, drug_name(NOT NULL), drug_strength, quantity(NOT NULL),
                // unit(NOT NULL), dosage(NOT NULL), frequency, duration_days, route, unit_price, line_total,
                // bhyt_applicable, note, created_at, deleted_at (them boi migration 9132), deleted_by.
                // LUU Y: cot ghi chu dung thuoc la "note" - KHONG phai "instructions".
                // Lay thong tin thuoc tu catalog de dien cac cot NOT NULL (drug_name, unit) + gia.
                var drug = await conn.QueryFirstOrDefaultAsync(
                    "SELECT name, strength, unit, price FROM diab_his_pha_drugs WHERE id = @drugId",
                    new { drugId = item.DrugId });
                string drugName = (string?)(drug?.name) ?? "";
                string? drugStrength = (string?)(drug?.strength);
                string drugUnit = (string?)(drug?.unit) ?? "";
                decimal unitPrice = drug?.price == null ? 0m : (decimal)drug.price;
                var itemId = Guid.NewGuid();
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_prescription_items
                      (id, tenant_id, prescription_id, drug_id, drug_name, drug_strength, unit, dosage, frequency, route, duration_days, quantity, unit_price, line_total, note)
                      VALUES (@itemId, @tenantId, @presId, @drugId, @drugName, @drugStrength, @unit, @dosage, @frequency, @route, @durationDays, @quantity, @unitPrice, @lineTotal, @instructions)",
                    new { itemId = itemId.ToString(), tenantId, presId, drugId = item.DrugId,
                          drugName, drugStrength, unit = drugUnit,
                          dosage = item.Dosage, frequency = item.Frequency, route = item.Route,
                          durationDays = item.DurationDays, quantity = item.Quantity,
                          unitPrice, lineTotal = unitPrice * item.Quantity, instructions = item.Instructions });

                if (Guid.TryParse(item.DrugId, out var drugGuid))
                {
                    packageLines.Add(new ProDiabHis.Application.Common.Interfaces.PackageCoverageLineRequest(
                        ProDiabHis.Application.Common.Interfaces.PackageItemType.DRUG, drugGuid, item.Quantity, itemId));
                }
            }
        }

        // Quyet dinh nghiep vu #2 (chot voi PO): tru dinh muc thuoc NGAY LUC KE DON (luu don thuoc),
        // KHONG phai luc cap phat tai quay duoc. Best-effort: chay o transaction rieng (khong chung
        // voi INSERT prescription o tren do CreatePrescriptionHandler hien dang dung nhieu lenh
        // Dapper roi rac khong bao trong 1 transaction) - neu that bai se khong rollback prescription,
        // nhung ConsumeAsync la idempotent nen co the goi lai an toan qua retry/reconciliation job.
        if (packageLines.Count > 0)
        {
            try
            {
                await _packageEntitlement.ConsumeAsync(
                    new ProDiabHis.Application.Common.Interfaces.PackageCoverageRequest(
                        cmd.Request.PatientId, "PRESCRIPTION", Guid.Parse(presId), packageLines, userId),
                    ct);
            }
            catch (ProDiabHis.Application.Common.Interfaces.PackageBalanceConflictException)
            {
                // Xung dot dong thoi hiem gap (2 giao dich tru cung 1 balance) - khong chan viec tao don,
                // duoc ban chi don gian khong ap dung dinh muc cho lan nay (BN van duoc kham/dung thuoc binh thuong,
                // se tinh phi day du). TODO: xem xet retry 1 lan hoac canh bao rieng cho nghiep vu.
            }
        }

        await _audit.LogAsync("CREATE", "diab_his_pha_prescriptions", presId, new { status = "DRAFT" }, ct);

        var response = await GetById(conn, presId, tenantId, ct);
        return Result<PrescriptionResponse>.Success(response);
    }

    private static async Task<PrescriptionResponse> GetById(System.Data.IDbConnection conn, string presId, int tenantId, CancellationToken ct)
    {
        var pres = await conn.QueryFirstAsync<PrescriptionRow>(
            @"SELECT id as Id, tenant_id as TenantId, encounter_id as EncounterId,
                     patient_id as PatientId, doctor_id as DoctorId,
                     status as Status, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @presId AND tenant_id = @tenantId",
            new { presId, tenantId });

        return new PrescriptionResponse(
            Guid.TryParse(pres.Id?.ToString(), out var g) ? g : Guid.NewGuid(),
            pres.TenantId,
            // BUG FIX: cung loi hard-code Guid.Empty nhu MapToResponse ben tren.
            Guid.TryParse(pres.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(pres.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            null, null, null,
            pres.Status ?? "DRAFT", pres.PrescribedAt,
            pres.SignedAt, pres.SignedBy, pres.DtqgCode, pres.DtqgStatus ?? "NONE",
            [], [], pres.TotalAmount, pres.Note, pres.CreatedAt, pres.UpdatedAt);
    }
}

public class UpdatePrescriptionHandler : IRequestHandler<UpdatePrescriptionCommand, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public UpdatePrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PrescriptionResponse>> Handle(UpdatePrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id.ToString(), tenantId });

        if (status == null)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");
        if (status != "DRAFT")
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Chi co the cap nhat don thuoc o trang thai DRAFT.");

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_prescriptions SET note = @note, updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { note = cmd.Request.Note, id = cmd.Id.ToString(), tenantId });

        var pres = await conn.QueryFirstAsync<PrescriptionRow>(
            @"SELECT id as Id, tenant_id as TenantId, encounter_id as EncounterId,
                     patient_id as PatientId, doctor_id as DoctorId,
                     status as Status, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id.ToString(), tenantId });

        return Result<PrescriptionResponse>.Success(new PrescriptionResponse(
            Guid.TryParse(pres.Id?.ToString(), out var g) ? g : Guid.Empty,
            pres.TenantId,
            Guid.TryParse(pres.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(pres.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            null, null, null,
            pres.Status ?? "DRAFT", pres.PrescribedAt, pres.SignedAt, pres.SignedBy,
            pres.DtqgCode, pres.DtqgStatus ?? "NONE", [], [], pres.TotalAmount, pres.Note,
            pres.CreatedAt, pres.UpdatedAt));
    }
}

public class DeletePrescriptionHandler : IRequestHandler<DeletePrescriptionCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public DeletePrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeletePrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id.ToString(), tenantId });

        if (status == null)
            return Result<bool>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");
        if (status != "DRAFT")
            return Result<bool>.Failure("PRESCRIPTION_INVALID_STATE", "Chi co the xoa don thuoc o trang thai DRAFT.");

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_prescriptions SET deleted_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id.ToString(), tenantId });

        return Result<bool>.Success(true);
    }
}

public class AddPrescriptionItemsHandler : IRequestHandler<AddPrescriptionItemsCommand, Result<IReadOnlyList<PrescriptionItemResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public AddPrescriptionItemsHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PrescriptionItemResponse>>> Handle(AddPrescriptionItemsCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.PrescriptionId.ToString(), tenantId });

        if (status == null)
            return Result<IReadOnlyList<PrescriptionItemResponse>>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");
        if (status != "DRAFT")
            return Result<IReadOnlyList<PrescriptionItemResponse>>.Failure("PRESCRIPTION_INVALID_STATE", "Chi co the them thuoc vao don DRAFT.");

        // presciption_id la CHAR(36) UUID (khong phai INT) - dung truc tiep GUID da xac thuc o tren
        var presId = cmd.PrescriptionId.ToString();

        var addedItems = new List<PrescriptionItemResponse>();
        foreach (var item in cmd.Items)
        {
            var itemId = Guid.NewGuid().ToString();
            // Cot thuc te cua diab_his_pha_prescription_items (xem ghi chu trong CreatePrescriptionHandler).
            // Lay thong tin thuoc tu catalog de dien cac cot NOT NULL (drug_name, unit) + gia.
            var drug = await conn.QueryFirstOrDefaultAsync(
                "SELECT name, strength, unit, price FROM diab_his_pha_drugs WHERE id = @drugId",
                new { drugId = item.DrugId });
            string drugName = (string?)(drug?.name) ?? "";
            string? drugStrength = (string?)(drug?.strength);
            string drugUnit = (string?)(drug?.unit) ?? "";
            decimal unitPrice = drug?.price == null ? 0m : (decimal)drug.price;
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_prescription_items
                  (id, tenant_id, prescription_id, drug_id, drug_name, drug_strength, unit, dosage, frequency, route, duration_days, quantity, unit_price, line_total, note)
                  VALUES (@id, @tenantId, @presId, @drugId, @drugName, @drugStrength, @unit, @dosage, @frequency, @route, @durationDays, @quantity, @unitPrice, @lineTotal, @instructions)",
                new { id = itemId, tenantId, presId, drugId = item.DrugId,
                      drugName, drugStrength, unit = drugUnit,
                      dosage = item.Dosage, frequency = item.Frequency, route = item.Route,
                      durationDays = item.DurationDays, quantity = item.Quantity,
                      unitPrice, lineTotal = unitPrice * item.Quantity, instructions = item.Instructions });

            addedItems.Add(new PrescriptionItemResponse(
                Guid.Parse(itemId), item.DrugId, drugName, drugStrength, drugUnit,
                item.Dosage, item.Frequency, item.Route, item.DurationDays, item.Quantity, item.Instructions, null));
        }

        return Result<IReadOnlyList<PrescriptionItemResponse>>.Success(addedItems);
    }
}

public class RemovePrescriptionItemHandler : IRequestHandler<RemovePrescriptionItemCommand, Result<bool>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public RemovePrescriptionItemHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(RemovePrescriptionItemCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT status FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.PrescriptionId.ToString(), tenantId });

        if (status == null)
            return Result<bool>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");
        if (status != "DRAFT")
            return Result<bool>.Failure("PRESCRIPTION_INVALID_STATE", "Chi co the xoa thuoc trong don DRAFT.");

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_prescription_items SET deleted_at = NOW() WHERE id = @itemId AND tenant_id = @tenantId",
            new { itemId = cmd.ItemId.ToString(), tenantId });

        return Result<bool>.Success(true);
    }
}

public class SignPrescriptionHandler : IRequestHandler<SignPrescriptionCommand, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IUsbTokenSigner _signer;
    private readonly ICdssEngine _cdssEngine;
    private readonly IAuditService _audit;
    private readonly ILogger<SignPrescriptionHandler> _logger;

    public SignPrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser,
        IUsbTokenSigner signer, ICdssEngine cdssEngine, IAuditService audit,
        ILogger<SignPrescriptionHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _signer = signer;
        _cdssEngine = cdssEngine;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<PrescriptionResponse>> Handle(SignPrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pres = await conn.QueryFirstOrDefaultAsync<PrescriptionRow>(
            @"SELECT id as Id, status as Status, tenant_id as TenantId,
                     encounter_id as EncounterId, patient_id as PatientId,
                     doctor_id as DoctorId, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id.ToString(), tenantId });

        if (pres == null)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");
        if (pres.Status != "DRAFT")
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_ALREADY_SIGNED", "Don thuoc da duoc ky, khong the ky lai.");

        // Verify signature
        var verifyResult = await _signer.VerifyAsync(cmd.Request.SignatureData, cmd.Request.CertificateThumbprint, ct);
        if (!verifyResult.IsValid)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_SIGNATURE_FAILED", $"Chu ky so khong hop le: {verifyResult.ErrorReason}");

        // CDSS check - chan luong ky neu co canh bao interruptive chua duoc override
        var presIdStr = pres.Id?.ToString() ?? cmd.Id.ToString();
        var drugItems = (await conn.QueryAsync<(string DrugId, string? GenericName, string? AtcCode)>(
            @"SELECT pi.drug_id AS DrugId, d.generic_name AS GenericName, d.atc_code AS AtcCode
              FROM diab_his_pha_prescription_items pi
              JOIN diab_his_pha_drugs d ON d.id = pi.drug_id
              WHERE pi.prescription_id = @presId AND pi.tenant_id = @tenantId AND pi.deleted_at IS NULL",
            new { presId = presIdStr, tenantId })).ToList();

        var cdssCtx = new CdssEvaluationContext(
            tenantId,
            Guid.TryParse((string?)pres.PatientId?.ToString(), out var patGuid) ? patGuid : null,
            Guid.TryParse((string?)pres.EncounterId?.ToString(), out var encGuid) ? encGuid : null,
            cmd.Id,
            drugItems.Select(d => new PrescribedDrug(d.DrugId, d.GenericName, d.AtcCode)).ToList());

        var cdssResult = await _cdssEngine.EvaluateAsync(cdssCtx, "SIGN", logEvents: true, ct);

        if (cdssResult.HasInterruptive)
        {
            var overrideCount = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM diab_his_cdss_alert_override_log WHERE prescription_id = @presId AND tenant_id = @tenantId",
                new { presId = presIdStr, tenantId });

            if (overrideCount == 0)
            {
                _logger.LogWarning("Prescription {Id} sign blocked due to interruptive CDSS alert", cmd.Id);
                return Result<PrescriptionResponse>.Failure("PRESCRIPTION_CDSS_BLOCKED",
                    "Đơn thuốc có cảnh báo nghiêm trọng chưa được xác nhận. Vui lòng xem lại hoặc nhập lý do bỏ qua.");
            }
        }

        var signatureBytes = Convert.FromBase64String(cmd.Request.SignatureData);
        var signedAt = cmd.Request.SigningTime ?? DateTime.UtcNow;
        var userId = _currentUser.UserId;

        await conn.ExecuteAsync(
            @"UPDATE diab_his_pha_prescriptions
              SET status = 'SIGNED', signed_at = @signedAt, updated_at = NOW()
              WHERE id = @id AND tenant_id = @tenantId",
            new { signedAt, id = pres.Id, tenantId });

        await _audit.LogAsync("SIGN", "diab_his_pha_prescriptions", pres.Id?.ToString() ?? "", new { status = "SIGNED" }, ct);

        var updated = await conn.QueryFirstAsync<PrescriptionRow>(
            @"SELECT id as Id, tenant_id as TenantId, encounter_id as EncounterId,
                     patient_id as PatientId, doctor_id as DoctorId,
                     status as Status, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = pres.Id, tenantId });

        return Result<PrescriptionResponse>.Success(MapRow(updated, [], []));
    }

    private static PrescriptionResponse MapRow(PrescriptionRow r, IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId,
            Guid.TryParse(r.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(r.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            null, null, null,
            r.Status ?? "DRAFT", r.PrescribedAt, r.SignedAt, r.SignedBy,
            r.DtqgCode, r.DtqgStatus ?? "NONE", items, warnings, r.TotalAmount, r.Note, r.CreatedAt, r.UpdatedAt);
}

public class CancelPrescriptionHandler : IRequestHandler<CancelPrescriptionCommand, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CancelPrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<PrescriptionResponse>> Handle(CancelPrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var pres = await conn.QueryFirstOrDefaultAsync<PrescriptionRow>(
            @"SELECT id as Id, status as Status, tenant_id as TenantId,
                     doctor_id as DoctorId, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions
              WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = cmd.Id.ToString(), tenantId });

        if (pres == null)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        if (pres.Status == "DISPENSED" || pres.Status == "PARTIAL_DISPENSED")
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Khong the huy don thuoc da phat.");

        if (pres.Status == "CANCELLED")
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_INVALID_STATE", "Don thuoc da bi huy.");

        await conn.ExecuteAsync(
            "UPDATE diab_his_pha_prescriptions SET status = 'CANCELLED', note = CONCAT(IFNULL(note,''), ' [HUY:', @reason, ']'), updated_at = NOW() WHERE id = @id AND tenant_id = @tenantId",
            new { reason = cmd.Reason, id = pres.Id, tenantId });

        await _audit.LogAsync("CANCEL", "diab_his_pha_prescriptions", pres.Id?.ToString() ?? "", new { reason = cmd.Reason }, ct);

        var updated = await conn.QueryFirstAsync<PrescriptionRow>(
            @"SELECT id as Id, tenant_id as TenantId, encounter_id as EncounterId,
                     patient_id as PatientId, doctor_id as DoctorId,
                     status as Status, created_at as PrescribedAt,
                     signed_at as SignedAt, NULL as SignedBy,
                     dtqg_code as DtqgCode, NULL as DtqgStatus,
                     (SELECT COALESCE(SUM(i.line_total),0) FROM diab_his_pha_prescription_items i
                       WHERE i.prescription_id = diab_his_pha_prescriptions.id AND i.deleted_at IS NULL) as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = pres.Id, tenantId });

        return Result<PrescriptionResponse>.Success(new PrescriptionResponse(
            Guid.TryParse(updated.Id?.ToString(), out var g) ? g : Guid.Empty,
            updated.TenantId,
            Guid.TryParse(updated.EncounterId?.ToString(), out var eg) ? eg : Guid.Empty,
            Guid.TryParse(updated.PatientId?.ToString(), out var pg) ? pg : Guid.Empty,
            null, null, null,
            updated.Status ?? "CANCELLED", updated.PrescribedAt, updated.SignedAt, updated.SignedBy,
            updated.DtqgCode, updated.DtqgStatus ?? "NONE", [], [], updated.TotalAmount, updated.Note,
            updated.CreatedAt, updated.UpdatedAt));
    }
}

public class CheckDdiHandler : IRequestHandler<CheckDdiQuery, Result<DdiCheckResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly ICdssEngine _cdssEngine;

    public CheckDdiHandler(IDapperConnectionFactory db, ICurrentUser currentUser, ICdssEngine cdssEngine)
    {
        _db = db;
        _currentUser = currentUser;
        _cdssEngine = cdssEngine;
    }

    public async Task<Result<DdiCheckResponse>> Handle(CheckDdiQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var presIdStr = q.PrescriptionId.ToString();

        var pres = await conn.QueryFirstOrDefaultAsync<(object? PatientId, object? EncounterId)?>(
            "SELECT patient_id AS PatientId, encounter_id AS EncounterId FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = presIdStr, tenantId });

        if (pres is null)
            return Result<DdiCheckResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        var drugItems = (await conn.QueryAsync<(string DrugId, string? GenericName, string? AtcCode)>(
            @"SELECT pi.drug_id AS DrugId, d.generic_name AS GenericName, d.atc_code AS AtcCode
              FROM diab_his_pha_prescription_items pi
              JOIN diab_his_pha_drugs d ON d.id = pi.drug_id
              WHERE pi.prescription_id = @presId AND pi.tenant_id = @tenantId AND pi.deleted_at IS NULL",
            new { presId = presIdStr, tenantId })).ToList();

        var cdssCtx = new CdssEvaluationContext(
            tenantId,
            Guid.TryParse(pres.Value.PatientId?.ToString(), out var patGuid) ? patGuid : null,
            Guid.TryParse(pres.Value.EncounterId?.ToString(), out var encGuid) ? encGuid : null,
            q.PrescriptionId,
            drugItems.Select(d => new PrescribedDrug(d.DrugId, d.GenericName, d.AtcCode)).ToList());

        var cdssResult = await _cdssEngine.EvaluateAsync(cdssCtx, "CHECK", logEvents: true, ct);

        var warnings = cdssResult.Alerts
            .Where(a => a.RuleType == "DRUG_DRUG")
            .Select(a =>
            {
                var pairText = a.Title.Replace("Tương tác thuốc: ", "");
                var parts = pairText.Split(" + ", 2);
                var ingredientA = parts.Length > 0 ? parts[0] : pairText;
                var ingredientB = parts.Length > 1 ? parts[1] : "";
                return new DdiWarning(0, ingredientA, 0, ingredientB, a.Severity, a.Detail, "");
            })
            .ToList();

        var hasContraindicated = cdssResult.Alerts.Any(a => a.Severity == "CONTRAINDICATED");

        return Result<DdiCheckResponse>.Success(new DdiCheckResponse(q.PrescriptionId, warnings, hasContraindicated));
    }
}

public class GetPrescriptionQrHandler : IRequestHandler<GetPrescriptionQrQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDtqgQrGenerator _qrGen;

    public GetPrescriptionQrHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDtqgQrGenerator qrGen)
    {
        _db = db;
        _currentUser = currentUser;
        _qrGen = qrGen;
    }

    public async Task<Result<byte[]>> Handle(GetPrescriptionQrQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var dtqgCode = await conn.ExecuteScalarAsync<string>(
            "SELECT dtqg_code FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.Id.ToString(), tenantId });

        if (dtqgCode == null)
            return Result<byte[]>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc hoac chua co ma DTQG.");

        var png = _qrGen.GenerateQrPng(dtqgCode, $"https://donthuocquocgia.vn/verify/{dtqgCode}");
        return Result<byte[]>.Success(png);
    }
}

public class GetPrescriptionPdfHandler : IRequestHandler<GetPrescriptionPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPrescriptionPdfBuilder _pdfBuilder;

    public GetPrescriptionPdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IPrescriptionPdfBuilder pdfBuilder)
    {
        _db = db;
        _currentUser = currentUser;
        _pdfBuilder = pdfBuilder;
    }

    public async Task<Result<byte[]>> Handle(GetPrescriptionPdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var presId = q.Id.ToString();

        // Header don thuoc + benh nhan + bac si + chan doan tu encounter
        var pres = await conn.QueryFirstOrDefaultAsync<PrescriptionPdfHeaderRow>(
            @"SELECT p.id as PrescriptionId, p.prescription_no as Code, p.created_at as PrescribedAt, p.note as Note,
                     p.diagnosis_icd10 as DiagnosisCode,
                     pat.full_name as PatientFullName, pat.gender as PatientGender,
                     pat.date_of_birth as PatientDateOfBirth, pat.street_enc as PatientAddress,
                     doc.full_name as DoctorFullName
              FROM diab_his_pha_prescriptions p
              LEFT JOIN diab_his_pat_patients pat ON pat.id = p.patient_id AND pat.tenant_id = p.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = p.doctor_id
              WHERE p.id = @presId AND p.tenant_id = @tenantId AND p.deleted_at IS NULL",
            new { presId, tenantId });

        if (pres == null)
            return Result<byte[]>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        // Ten chan doan (neu co) tu bang diagnoses khop ma ICD-10 chinh cua don thuoc (qua encounter)
        string? diagnosisName = null;
        if (!string.IsNullOrWhiteSpace(pres.DiagnosisCode))
        {
            diagnosisName = await conn.ExecuteScalarAsync<string>(
                @"SELECT d.name FROM diab_his_enc_diagnoses d
                  JOIN diab_his_pha_prescriptions p ON p.encounter_id = d.encounter_id AND p.tenant_id = d.tenant_id
                  WHERE p.id = @presId AND p.tenant_id = @tenantId AND d.icd10_code = @code AND d.deleted_at IS NULL
                  LIMIT 1",
                new { presId, tenantId, code = pres.DiagnosisCode });
        }

        // Danh sach thuoc trong don
        var itemRows = await conn.QueryAsync<PrescriptionPdfItemRow>(
            @"SELECT d.name as DrugName, d.strength as Strength, d.unit as Unit,
                     i.dosage as Dosage, i.frequency as Frequency, i.route as Route,
                     i.duration_days as DurationDays, i.quantity as Quantity, i.note as Instructions
              FROM diab_his_pha_prescription_items i
              JOIN diab_his_pha_drugs d ON d.id = i.drug_id
              WHERE i.prescription_id = @presId AND i.tenant_id = @tenantId AND i.deleted_at IS NULL
              ORDER BY i.created_at",
            new { presId, tenantId });

        // Letterhead phong kham (giong ExportReportHandler)
        var lh = await conn.QueryFirstOrDefaultAsync<PrescriptionPdfLetterheadRow>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName,
                     address AS Address, phone AS Phone, email AS Email, email_support AS EmailSupport,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });

        byte[]? logoBytes = null; // Logo tenant tuy chinh khong duoc fetch qua HTTP o day; PrescriptionPdfBuilder fallback sang logo bundled

        var items = itemRows.Select((r, idx) => new PrescriptionPdfItem(
            idx + 1, r.DrugName, r.Strength, r.Unit, r.Quantity, r.Dosage, r.Frequency, r.Route, r.DurationDays, r.Instructions)).ToList();

        var pdfData = new PrescriptionPdfData(
            PrescriptionCode: pres.Code ?? presId,
            PrescribedAt: pres.PrescribedAt,
            Note: pres.Note,
            ClinicName: lh?.ClinicName ?? "Pro-Diab HIS",
            ClinicAddress: lh?.Address,
            ClinicPhone: lh?.Phone,
            CskcbCode: lh?.CskcbCode,
            ClinicLogo: logoBytes,
            PatientFullName: pres.PatientFullName ?? "",
            PatientGender: pres.PatientGender,
            PatientDateOfBirth: pres.PatientDateOfBirth.HasValue ? DateOnly.FromDateTime(pres.PatientDateOfBirth.Value) : null,
            PatientAddress: PiiCrypto.Unprotect(pres.PatientAddress),
            DiagnosisCode: pres.DiagnosisCode,
            DiagnosisName: diagnosisName,
            DoctorFullName: pres.DoctorFullName,
            Items: items,
            ClinicCompanyName: lh?.CompanyName,
            ClinicSlogan: lh?.Slogan,
            ClinicWebsite: lh?.Website,
            ClinicEmail: lh?.EmailSupport ?? lh?.Email);

        var pdf = _pdfBuilder.Build(pdfData);

        // Record print event
        await conn.ExecuteAsync(
            "INSERT INTO diab_his_pha_prescription_print_history (id, tenant_id, prescription_id, printed_at) VALUES (UUID(), @tenantId, @presId, NOW())",
            new { tenantId, presId });

        return Result<byte[]>.Success(pdf);
    }
}

/// <summary>Portal: xuat PDF don thuoc, chi cho phep benh nhan xem don thuoc CUA CHINH MINH.</summary>
public class GetPortalPrescriptionPdfHandler : IRequestHandler<GetPortalPrescriptionPdfQuery, Result<byte[]>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly IPrescriptionPdfBuilder _pdfBuilder;

    public GetPortalPrescriptionPdfHandler(IDapperConnectionFactory db, IPrescriptionPdfBuilder pdfBuilder)
    {
        _db = db;
        _pdfBuilder = pdfBuilder;
    }

    public async Task<Result<byte[]>> Handle(GetPortalPrescriptionPdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = q.TenantId;
        var presId = q.PrescriptionId.ToString();
        var patientId = q.PatientId.ToString();

        // Header don thuoc + benh nhan + bac si + chan doan tu encounter — BAT BUOC p.patient_id = @patientId
        var pres = await conn.QueryFirstOrDefaultAsync<PrescriptionPdfHeaderRow>(
            @"SELECT p.id as PrescriptionId, p.prescription_no as Code, p.created_at as PrescribedAt, p.note as Note,
                     p.diagnosis_icd10 as DiagnosisCode,
                     pat.full_name as PatientFullName, pat.gender as PatientGender,
                     pat.date_of_birth as PatientDateOfBirth, pat.street_enc as PatientAddress,
                     doc.full_name as DoctorFullName
              FROM diab_his_pha_prescriptions p
              LEFT JOIN diab_his_pat_patients pat ON pat.id = p.patient_id AND pat.tenant_id = p.tenant_id
              LEFT JOIN diab_his_sec_users doc ON doc.id = p.doctor_id
              WHERE p.id = @presId AND p.tenant_id = @tenantId AND p.patient_id = @patientId AND p.deleted_at IS NULL",
            new { presId, tenantId, patientId });

        if (pres == null)
            return Result<byte[]>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        string? diagnosisName = null;
        if (!string.IsNullOrWhiteSpace(pres.DiagnosisCode))
        {
            diagnosisName = await conn.ExecuteScalarAsync<string>(
                @"SELECT d.name FROM diab_his_enc_diagnoses d
                  JOIN diab_his_pha_prescriptions p ON p.encounter_id = d.encounter_id AND p.tenant_id = d.tenant_id
                  WHERE p.id = @presId AND p.tenant_id = @tenantId AND d.icd10_code = @code AND d.deleted_at IS NULL
                  LIMIT 1",
                new { presId, tenantId, code = pres.DiagnosisCode });
        }

        var itemRows = await conn.QueryAsync<PrescriptionPdfItemRow>(
            @"SELECT d.name as DrugName, d.strength as Strength, d.unit as Unit,
                     i.dosage as Dosage, i.frequency as Frequency, i.route as Route,
                     i.duration_days as DurationDays, i.quantity as Quantity, i.note as Instructions
              FROM diab_his_pha_prescription_items i
              JOIN diab_his_pha_drugs d ON d.id = i.drug_id
              WHERE i.prescription_id = @presId AND i.tenant_id = @tenantId AND i.deleted_at IS NULL
              ORDER BY i.created_at",
            new { presId, tenantId });

        var lh = await conn.QueryFirstOrDefaultAsync<PrescriptionPdfLetterheadRow>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName,
                     address AS Address, phone AS Phone, email AS Email, email_support AS EmailSupport,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants
              WHERE id = @tenantId",
            new { tenantId });

        var items = itemRows.Select((r, idx) => new PrescriptionPdfItem(
            idx + 1, r.DrugName, r.Strength, r.Unit, r.Quantity, r.Dosage, r.Frequency, r.Route, r.DurationDays, r.Instructions)).ToList();

        var pdfData = new PrescriptionPdfData(
            PrescriptionCode: pres.Code ?? presId,
            PrescribedAt: pres.PrescribedAt,
            Note: pres.Note,
            ClinicName: lh?.ClinicName ?? "Pro-Diab HIS",
            ClinicAddress: lh?.Address,
            ClinicPhone: lh?.Phone,
            CskcbCode: lh?.CskcbCode,
            ClinicLogo: null,
            PatientFullName: pres.PatientFullName ?? "",
            PatientGender: pres.PatientGender,
            PatientDateOfBirth: pres.PatientDateOfBirth.HasValue ? DateOnly.FromDateTime(pres.PatientDateOfBirth.Value) : null,
            PatientAddress: PiiCrypto.Unprotect(pres.PatientAddress),
            DiagnosisCode: pres.DiagnosisCode,
            DiagnosisName: diagnosisName,
            DoctorFullName: pres.DoctorFullName,
            Items: items,
            ClinicCompanyName: lh?.CompanyName,
            ClinicSlogan: lh?.Slogan,
            ClinicWebsite: lh?.Website,
            ClinicEmail: lh?.EmailSupport ?? lh?.Email);

        var pdf = _pdfBuilder.Build(pdfData);
        return Result<byte[]>.Success(pdf);
    }
}

internal class PrescriptionPdfHeaderRow
{
    public string? PrescriptionId { get; set; }
    public string? Code { get; set; }
    public DateTime PrescribedAt { get; set; }
    public string? Note { get; set; }
    public string? PatientFullName { get; set; }
    public string? PatientGender { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public string? PatientAddress { get; set; }
    public string? DoctorFullName { get; set; }
    public string? DiagnosisCode { get; set; }
}

internal class PrescriptionPdfItemRow
{
    public string DrugName { get; set; } = "";
    public string? Strength { get; set; }
    public string? Unit { get; set; }
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public string Route { get; set; } = "ORAL";
    public int DurationDays { get; set; }
    public decimal Quantity { get; set; }
    public string? Instructions { get; set; }
}

internal class PrescriptionPdfLetterheadRow
{
    public string? ClinicName { get; set; }
    public string? CskcbCode { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? EmailSupport { get; set; }
    public string? Slogan { get; set; }
    public string? Website { get; set; }
}

public class GetPrintHistoryHandler : IRequestHandler<GetPrintHistoryQuery, Result<IReadOnlyList<PrintHistoryItem>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetPrintHistoryHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<PrintHistoryItem>>> Handle(GetPrintHistoryQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        // BUG FIX: Dapper KHONG tu convert string -> Guid non-nullable (Convert.ChangeType khong
        // ho tro Guid IConvertible) -> QueryAsync<PrintHistoryItem> voi Id kieu Guid nem
        // InvalidCastException khi cot id la string (GuidFormat=None). Truoc day khong lo ra vi
        // ban ghi test chua co du lieu print-history that (query rong -> khong deserialize dong nao).
        var rawItems = await conn.QueryAsync<(string Id, DateTime PrintedAt, int? PrintedBy, string? PrinterName)>(
            @"SELECT id as Id, printed_at as PrintedAt, printed_by as PrintedBy, printer_name as PrinterName
              FROM diab_his_pha_prescription_print_history
              WHERE prescription_id = @presId AND tenant_id = @tenantId
              ORDER BY printed_at DESC",
            new { presId = q.PrescriptionId.ToString(), tenantId });

        var items = rawItems.Select(r => new PrintHistoryItem
        {
            Id = Guid.TryParse(r.Id, out var g) ? g : Guid.Empty,
            PrintedAt = r.PrintedAt,
            PrintedBy = r.PrintedBy,
            PrinterName = r.PrinterName
        }).ToList();

        return Result<IReadOnlyList<PrintHistoryItem>>.Success(items);
    }
}

// ─── Internal Dapper row types ────────────────────────────────────────────────
internal class PrescriptionRow
{
    public object? Id { get; set; }
    public int TenantId { get; set; }
    public object? EncounterId { get; set; }
    public object? PatientId { get; set; }
    public object? DoctorId { get; set; }
    public string? Status { get; set; }
    public DateTime PrescribedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public int? SignedBy { get; set; }
    public string? DtqgCode { get; set; }
    public string? DtqgStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal class PrescriptionItemRow
{
    public string? Id { get; set; }
    public string PrescriptionId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string? DrugName { get; set; }
    public string? Strength { get; set; }
    public string? Unit { get; set; }
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public string Route { get; set; } = "ORAL";
    public int DurationDays { get; set; }
    public decimal Quantity { get; set; }
    public string? Instructions { get; set; }
    public string? BatchDispensedJson { get; set; }
}
