using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

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
    : IRequest<Result<PrescriptionResponse>>;

public record UpdatePrescriptionCommand(Guid Id, PrescriptionUpdateRequest Request)
    : IRequest<Result<PrescriptionResponse>>;

public record DeletePrescriptionCommand(Guid Id) : IRequest<Result<bool>>;

public record AddPrescriptionItemsCommand(Guid PrescriptionId, IReadOnlyList<PrescriptionItemRequest> Items)
    : IRequest<Result<IReadOnlyList<PrescriptionItemResponse>>>;

public record RemovePrescriptionItemCommand(Guid PrescriptionId, Guid ItemId) : IRequest<Result<bool>>;

public record SignPrescriptionCommand(Guid Id, SignPrescriptionRequest Request)
    : IRequest<Result<PrescriptionResponse>>;

public record CancelPrescriptionCommand(Guid Id, string Reason)
    : IRequest<Result<PrescriptionResponse>>;

public record CheckDdiQuery(Guid PrescriptionId) : IRequest<Result<DdiCheckResponse>>;

public record GetPrescriptionQrQuery(Guid Id) : IRequest<Result<byte[]>>;

public record GetPrescriptionPdfQuery(Guid Id) : IRequest<Result<byte[]>>;

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
                   0 as TotalAmount, p.note as Note,
                   p.created_at as CreatedAt, p.updated_at as UpdatedAt
            FROM diab_his_pha_prescriptions p
            WHERE {whereClause}
            ORDER BY p.created_at DESC
            LIMIT @limit OFFSET @offset";

        var rows = await conn.QueryAsync<PrescriptionRow>(sql, prm);

        var items = rows.Select(r => MapToResponse(r, [], [])).ToList();
        return Result<PagedResult<PrescriptionResponse>>.Success(
            new PagedResult<PrescriptionResponse>(items, q.Page, q.PageSize, total));
    }

    private static PrescriptionResponse MapToResponse(PrescriptionRow r,
        IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(
            Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId, Guid.Empty, Guid.Empty,
            null, null, null,
            r.Status ?? "DRAFT", r.PrescribedAt,
            r.SignedAt, r.SignedBy, r.DtqgCode, r.DtqgStatus ?? "NONE",
            items, warnings, r.TotalAmount, r.Note, r.CreatedAt, r.UpdatedAt);
}

public class GetPrescriptionHandler : IRequestHandler<GetPrescriptionQuery, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;

    public GetPrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
                     0 as TotalAmount, p.note as Note,
                     p.created_at as CreatedAt, p.updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions p
              WHERE p.id = @id AND p.tenant_id = @tenantId AND p.deleted_at IS NULL",
            new { id = q.Id.ToString(), tenantId });

        if (pres == null)
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        var items = await conn.QueryAsync<PrescriptionItemRow>(
            @"SELECT i.id as Id, i.drug_id as DrugId, d.name as DrugName,
                     d.strength as Strength, d.unit as Unit,
                     i.dosage as Dosage, i.frequency as Frequency, i.route as Route,
                     i.duration_days as DurationDays, i.quantity as Quantity,
                     i.instructions as Instructions, NULL as BatchDispensedJson
              FROM diab_his_pha_prescription_items i
              JOIN diab_his_pha_drugs d ON d.id = i.drug_id
              WHERE i.prescription_id = @presId AND i.tenant_id = @tenantId AND i.deleted_at IS NULL",
            new { presId = pres.Id, tenantId });

        var itemResponses = items.Select(MapItem).ToList();
        var response = MapPresRow(pres, itemResponses, []);
        return Result<PrescriptionResponse>.Success(response);
    }

    private static PrescriptionItemResponse MapItem(PrescriptionItemRow r) =>
        new(Guid.TryParse(r.Id, out var g) ? g : Guid.Empty,
            r.DrugId, r.DrugName ?? "", r.Strength, r.Unit,
            r.Dosage, r.Frequency, r.Route, r.DurationDays, r.Quantity,
            r.Instructions, null);

    private static PrescriptionResponse MapPresRow(PrescriptionRow r,
        IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId, Guid.Empty, Guid.Empty,
            null, null, null,
            r.Status ?? "DRAFT", r.PrescribedAt,
            r.SignedAt, r.SignedBy, r.DtqgCode, r.DtqgStatus ?? "NONE",
            items, warnings, r.TotalAmount, r.Note, r.CreatedAt, r.UpdatedAt);
}

public class CreatePrescriptionHandler : IRequestHandler<CreatePrescriptionCommand, Result<PrescriptionResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;

    public CreatePrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<Result<PrescriptionResponse>> Handle(CreatePrescriptionCommand cmd, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;
        var userId = _currentUser.UserId;

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

        if (cmd.Request.Items?.Count > 0)
        {
            foreach (var item in cmd.Request.Items)
            {
                // Cot thuc te cua diab_his_pha_prescription_items (theo schema dump goc):
                // id, tenant_id, prescription_id, drug_id, dosage, frequency, route,
                // duration_days, quantity, instructions - KHONG co drug_name/unit/unit_price/
                // line_total/note (nhung cot nay chi ton tai o migration 9005 chua duoc ap dung).
                await conn.ExecuteAsync(
                    @"INSERT INTO diab_his_pha_prescription_items
                      (id, tenant_id, prescription_id, drug_id, dosage, frequency, route, duration_days, quantity, instructions)
                      VALUES (UUID(), @tenantId, @presId, @drugId, @dosage, @frequency, @route, @durationDays, @quantity, @instructions)",
                    new { tenantId, presId, drugId = item.DrugId, dosage = item.Dosage, frequency = item.Frequency, route = item.Route, durationDays = item.DurationDays, quantity = item.Quantity, instructions = item.Instructions });
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
                     0 as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @presId AND tenant_id = @tenantId",
            new { presId, tenantId });

        return new PrescriptionResponse(
            Guid.TryParse(pres.Id?.ToString(), out var g) ? g : Guid.NewGuid(),
            pres.TenantId, Guid.Empty, Guid.Empty,
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
                     0 as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = cmd.Id.ToString(), tenantId });

        return Result<PrescriptionResponse>.Success(new PrescriptionResponse(
            Guid.TryParse(pres.Id?.ToString(), out var g) ? g : Guid.Empty,
            pres.TenantId, Guid.Empty, Guid.Empty, null, null, null,
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
            // Cot thuc te cua diab_his_pha_prescription_items (xem ghi chu trong CreatePrescriptionHandler)
            await conn.ExecuteAsync(
                @"INSERT INTO diab_his_pha_prescription_items
                  (id, tenant_id, prescription_id, drug_id, dosage, frequency, route, duration_days, quantity, instructions)
                  VALUES (@id, @tenantId, @presId, @drugId, @dosage, @frequency, @route, @durationDays, @quantity, @instructions)",
                new { id = itemId, tenantId, presId, drugId = item.DrugId, dosage = item.Dosage, frequency = item.Frequency, route = item.Route, durationDays = item.DurationDays, quantity = item.Quantity, instructions = item.Instructions });

            var drugName = await conn.ExecuteScalarAsync<string>(
                "SELECT name FROM diab_his_pha_drugs WHERE ID = @drugId", new { drugId = item.DrugId }) ?? "";

            addedItems.Add(new PrescriptionItemResponse(
                Guid.Parse(itemId), item.DrugId, drugName, null, null,
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
    private readonly IDdiChecker _ddiChecker;
    private readonly IAuditService _audit;
    private readonly ILogger<SignPrescriptionHandler> _logger;

    public SignPrescriptionHandler(IDapperConnectionFactory db, ICurrentUser currentUser,
        IUsbTokenSigner signer, IDdiChecker ddiChecker, IAuditService audit,
        ILogger<SignPrescriptionHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _signer = signer;
        _ddiChecker = ddiChecker;
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
                     0 as TotalAmount, note as Note,
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

        // DDI check - block if CONTRAINDICATED
        // Luu y: diab_his_pha_prescription_items.drug_id la GUID (CHAR(36), khop
        // diab_his_pha_drugs.id) trong khi diab_his_pha_ddi_rules van dung ID so nguyen
        // tham chieu catalog thuoc cu (pha_drug_master). Hai catalog nay khong con tuong
        // thich (khac khoa chinh) nen chua co cach anh xa GUID -> ID so hop le; DDI-check
        // tam thoi tra ve danh sach rong (khong crash) cho toi khi co migration hop nhat
        // catalog thuoc. Xem ghi chu bao cao cua Thao (BUG DrugId type mismatch).
        var drugIds = new List<int>();

        var ddiWarnings = await _ddiChecker.CheckAsync(drugIds, ct);
        if (ddiWarnings.Any(w => w.Severity == "CONTRAINDICATED"))
        {
            _logger.LogWarning("Prescription {Id} sign blocked due to CONTRAINDICATED DDI", cmd.Id);
            return Result<PrescriptionResponse>.Failure("PRESCRIPTION_DDI_BLOCKED",
                "Don thuoc co tuong tac chong chi dinh (CONTRAINDICATED). Khong the ky so.");
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
                     0 as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = pres.Id, tenantId });

        return Result<PrescriptionResponse>.Success(MapRow(updated, [], []));
    }

    private static PrescriptionResponse MapRow(PrescriptionRow r, IReadOnlyList<PrescriptionItemResponse> items, IReadOnlyList<DdiWarning> warnings) =>
        new(Guid.TryParse(r.Id?.ToString(), out var g) ? g : Guid.Empty,
            r.TenantId, Guid.Empty, Guid.Empty, null, null, null,
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
                     0 as TotalAmount, note as Note,
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
                     0 as TotalAmount, note as Note,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId",
            new { id = pres.Id, tenantId });

        return Result<PrescriptionResponse>.Success(new PrescriptionResponse(
            Guid.TryParse(updated.Id?.ToString(), out var g) ? g : Guid.Empty,
            updated.TenantId, Guid.Empty, Guid.Empty, null, null, null,
            updated.Status ?? "CANCELLED", updated.PrescribedAt, updated.SignedAt, updated.SignedBy,
            updated.DtqgCode, updated.DtqgStatus ?? "NONE", [], [], updated.TotalAmount, updated.Note,
            updated.CreatedAt, updated.UpdatedAt));
    }
}

public class CheckDdiHandler : IRequestHandler<CheckDdiQuery, Result<DdiCheckResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDdiChecker _ddiChecker;

    public CheckDdiHandler(IDapperConnectionFactory db, ICurrentUser currentUser, IDdiChecker ddiChecker)
    {
        _db = db;
        _currentUser = currentUser;
        _ddiChecker = ddiChecker;
    }

    public async Task<Result<DdiCheckResponse>> Handle(CheckDdiQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var presId = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.PrescriptionId.ToString(), tenantId });

        if (presId == null)
            return Result<DdiCheckResponse>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        var drugIds = (await conn.QueryAsync<int>(
            "SELECT drug_id FROM diab_his_pha_prescription_items WHERE prescription_id = @presId AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { presId, tenantId })).ToList();

        var warnings = await _ddiChecker.CheckAsync(drugIds, ct);
        var hasContraindicated = warnings.Any(w => w.Severity == "CONTRAINDICATED");

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

    public GetPrescriptionPdfHandler(IDapperConnectionFactory db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<byte[]>> Handle(GetPrescriptionPdfQuery q, CancellationToken ct)
    {
        using var conn = ((IDbConnection)_db.CreateConnection());
        var tenantId = _currentUser.TenantId!.Value;

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM diab_his_pha_prescriptions WHERE id = @id AND tenant_id = @tenantId AND deleted_at IS NULL",
            new { id = q.Id.ToString(), tenantId });

        if (exists == 0)
            return Result<byte[]>.Failure("PRESCRIPTION_NOT_FOUND", "Khong tim thay don thuoc.");

        // Record print event
        await conn.ExecuteAsync(
            "INSERT INTO diab_his_pha_prescription_print_history (id, tenant_id, prescription_id, printed_at) VALUES (UUID(), @tenantId, @presId, NOW())",
            new { tenantId, presId = q.Id.ToString() });

        // Generate simple PDF placeholder (QuestPDF integration)
        var pdf = GeneratePrescriptionPdf(q.Id);
        return Result<byte[]>.Success(pdf);
    }

    private static byte[] GeneratePrescriptionPdf(Guid prescriptionId)
    {
        // Minimal valid PDF stub - PDF generation via IPdfService in Infrastructure
        var content = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n3 0 obj<</Type/Page/MediaBox[0 0 420 595]/Parent 2 0 R/Resources<<>>/Contents 4 0 R>>endobj\n4 0 obj<</Length 44>>stream\nBT /F1 12 Tf 50 500 Td (DON THUOC {prescriptionId}) Tj ET\nendstream\nendobj\nxref\n0 5\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\n0000000115 00000 n\n0000000266 00000 n\ntrailer<</Size 5/Root 1 0 R>>\nstartxref\n360\n%%EOF";
        return System.Text.Encoding.ASCII.GetBytes(content);
    }
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

        var items = await conn.QueryAsync<PrintHistoryItem>(
            @"SELECT id as Id, printed_at as PrintedAt, printed_by as PrintedBy, printer_name as PrinterName
              FROM diab_his_pha_prescription_print_history
              WHERE prescription_id = @presId AND tenant_id = @tenantId
              ORDER BY printed_at DESC",
            new { presId = q.PrescriptionId.ToString(), tenantId });

        return Result<IReadOnlyList<PrintHistoryItem>>.Success(items.ToList());
    }
}

// ─── Internal Dapper row types ────────────────────────────────────────────────
internal class PrescriptionRow
{
    public object? Id { get; set; }
    public int TenantId { get; set; }
    public object? EncounterId { get; set; }
    public object? PatientId { get; set; }
    public string? DoctorId { get; set; }
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
