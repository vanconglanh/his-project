using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.Billing;

// ────────────────────────────────────────────────
// BUG-F01: "hang cho thu ngan" - liet ke luot kham co dich vu (CLS/thuoc) nhung CHUA duoc
// lap hoa don. Dung Dapper read, bat buoc filter tenant_id.
// ────────────────────────────────────────────────

public record ListPendingEncountersQuery(int? BranchId, DateOnly? Date)
    : IRequest<Result<PendingEncounterListResponse>>;

public record PendingEncounterItemResponse(
    Guid EncounterId,
    string PatientCode,
    string PatientName,
    string? DoctorName,
    bool HasLab,
    bool HasRad,
    bool HasDrug,
    decimal EstimatedTotal,
    DateTime CreatedAt);

public record PendingEncounterListResponse(List<PendingEncounterItemResponse> Data, int Total);

public class ListPendingEncountersQueryHandler
    : IRequestHandler<ListPendingEncountersQuery, Result<PendingEncounterListResponse>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListPendingEncountersQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    {
        _db = db; _tenant = tenant;
    }

    public async Task<Result<PendingEncounterListResponse>> Handle(ListPendingEncountersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        var tid = _tenant.TenantId;

        var where = "WHERE e.tenant_id=@TId AND e.deleted_at IS NULL";
        var p = new DynamicParameters();
        p.Add("TId", tid);

        if (q.BranchId.HasValue)
        {
            where += " AND e.branch_id=@BranchId";
            p.Add("BranchId", q.BranchId.Value);
        }
        if (q.Date.HasValue)
        {
            where += " AND DATE(e.created_at)=@Date";
            p.Add("Date", q.Date.Value.ToString("yyyy-MM-dd"));
        }

        // Chua lap hoa don: khong co billing (con hieu luc, khac VOID) gan voi encounter nay.
        where += @" AND NOT EXISTS (
                SELECT 1 FROM diab_his_bil_billing b
                WHERE b.tenant_id=@TId AND b.encounter_id=e.id AND b.deleted_at IS NULL AND b.status <> 'VOID')";

        // Chi lay luot kham THUC SU co dich vu (CLS da chi dinh hoac thuoc da cap phat).
        where += @" AND (
                EXISTS (SELECT 1 FROM diab_his_cli_lab_orders lo WHERE lo.tenant_id=@TId AND lo.encounter_id=e.id AND lo.deleted_at IS NULL)
             OR EXISTS (SELECT 1 FROM diab_his_cli_rad_orders ro WHERE ro.tenant_id=@TId AND ro.encounter_id=e.id AND ro.deleted_at IS NULL)
             OR EXISTS (SELECT 1 FROM diab_his_pha_prescriptions pr WHERE pr.tenant_id=@TId AND pr.encounter_id=e.id
                        AND pr.status='DISPENSED' AND pr.deleted_at IS NULL)
            )";

        var sql = $@"
            SELECT
                e.id AS encounter_id,
                pat.code AS patient_code,
                pat.full_name AS patient_name,
                doc.full_name AS doctor_name,
                e.created_at,
                EXISTS (SELECT 1 FROM diab_his_cli_lab_orders lo WHERE lo.tenant_id=@TId AND lo.encounter_id=e.id AND lo.deleted_at IS NULL) AS has_lab,
                EXISTS (SELECT 1 FROM diab_his_cli_rad_orders ro WHERE ro.tenant_id=@TId AND ro.encounter_id=e.id AND ro.deleted_at IS NULL) AS has_rad,
                EXISTS (SELECT 1 FROM diab_his_pha_prescriptions pr WHERE pr.tenant_id=@TId AND pr.encounter_id=e.id
                        AND pr.status='DISPENSED' AND pr.deleted_at IS NULL) AS has_drug,
                (
                    COALESCE((SELECT SUM(r.total_amount) FROM diab_his_cls_order_rounds r
                              WHERE r.tenant_id=@TId AND r.encounter_id=e.id AND r.status <> 'CANCELLED' AND r.deleted_at IS NULL), 0)
                    +
                    COALESCE((SELECT SUM(pi.line_total) FROM diab_his_pha_prescription_items pi
                              INNER JOIN diab_his_pha_prescriptions pr2 ON pr2.id = pi.prescription_id
                              WHERE pi.tenant_id=@TId AND pr2.encounter_id=e.id
                                AND pr2.status='DISPENSED' AND pr2.deleted_at IS NULL), 0)
                ) AS estimated_total
            FROM diab_his_enc_encounters e
            INNER JOIN diab_his_pat_patients pat ON pat.id = e.patient_id
            LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
            {where}
            ORDER BY e.created_at DESC";

        var rows = (await conn.QueryAsync<dynamic>(sql, p)).ToList();

        var items = rows.Select(r => new PendingEncounterItemResponse(
            Guid.Parse((string)r.encounter_id),
            (string)r.patient_code,
            (string)r.patient_name,
            (string?)r.doctor_name,
            Convert.ToBoolean(r.has_lab),
            Convert.ToBoolean(r.has_rad),
            Convert.ToBoolean(r.has_drug),
            Convert.ToDecimal(r.estimated_total),
            (DateTime)r.created_at)).ToList();

        return Result<PendingEncounterListResponse>.Success(new PendingEncounterListResponse(items, items.Count));
    }
}
