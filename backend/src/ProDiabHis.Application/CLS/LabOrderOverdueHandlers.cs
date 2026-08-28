using Dapper;
using MediatR;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.LabPartners;

namespace ProDiabHis.Application.CLS;

// ────────────────────────────────────────────────
// FR-511 [P1]: Canh bao ket qua XN qua han SLA cam ket voi doi tac lab.
// Overdue duoc TINH TOAN truc tiep trong query tu
// (LabOrder.ordered_at + LabPartner.sla_days) so voi thoi diem hien tai,
// khong luu cot trang thai rieng de tranh du lieu drift/qua han "gia".
// Quy uoc trang thai "da co ket qua": LabOrderStatus.Done (enum hien tai
// khong co gia tri rieng "result_received" - Done la trang thai cuoi cung
// tuong duong da nhan/xac nhan ket qua, xem LabOrder.cs).
// ────────────────────────────────────────────────
public record ListOverdueLabOrdersQuery() : IRequest<Result<IReadOnlyList<LabOrderOverdueResponse>>>;

public class ListOverdueLabOrdersQueryHandler
    : IRequestHandler<ListOverdueLabOrdersQuery, Result<IReadOnlyList<LabOrderOverdueResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;
    private readonly IBranchProvider _branch;

    public ListOverdueLabOrdersQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant, IBranchProvider branch)
    { _db = db; _tenant = tenant; _branch = branch; }

    public async Task<Result<IReadOnlyList<LabOrderOverdueResponse>>> Handle(
        ListOverdueLabOrdersQuery q, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT
                lo.id             AS lo_id,
                lo.encounter_id   AS lo_encounter_id,
                lo.test_code      AS lo_test_code,
                lo.test_name      AS lo_test_name,
                lo.status         AS lo_status,
                lo.ordered_at     AS lo_ordered_at,
                lo.lab_partner_id AS lo_lab_partner_id,
                lo.branch_id      AS lo_branch_id,
                e.patient_id      AS enc_patient_id,
                p.full_name       AS pat_full_name,
                lp.name           AS lp_name,
                COALESCE(lp.sla_days, 3) AS lp_sla_days,
                DATE_ADD(lo.ordered_at, INTERVAL COALESCE(lp.sla_days, 3) DAY) AS due_date
            FROM diab_his_cli_lab_orders lo
            LEFT JOIN diab_his_enc_encounters e ON e.id = lo.encounter_id
            LEFT JOIN diab_his_pat_patients   p ON p.id = e.patient_id
            LEFT JOIN diab_his_int_lab_partners lp ON lp.id = lo.lab_partner_id
            WHERE lo.tenant_id = @TId
              AND lo.deleted_at IS NULL
              AND lo.lab_partner_id IS NOT NULL
              AND lo.status NOT IN ('done', 'cancelled')
              AND (@Ignore = 1 OR lo.branch_id IS NULL OR lo.branch_id = @BranchId)
              AND DATE_ADD(lo.ordered_at, INTERVAL COALESCE(lp.sla_days, 3) DAY) < UTC_TIMESTAMP()
            ORDER BY lo.ordered_at ASC",
            new { TId = _tenant.TenantId, Ignore = _branch.IgnoreBranchFilter ? 1 : 0, BranchId = _branch.BranchId });

        var now = DateTime.UtcNow;
        var result = rows.Select(r =>
        {
            var dueDate = (DateTime)r.due_date;
            var daysOverdue = (int)Math.Max(0, Math.Floor((now - dueDate).TotalDays));
            string? partnerId = (string?)r.lo_lab_partner_id;
            string? patientId = (string?)r.enc_patient_id;

            return new LabOrderOverdueResponse(
                Guid.Parse((string)r.lo_id),
                Guid.Parse((string)r.lo_encounter_id),
                string.IsNullOrEmpty(patientId) ? null : Guid.Parse(patientId),
                (string?)r.pat_full_name,
                (string)r.lo_test_code,
                (string)r.lo_test_name,
                (string)r.lo_status,
                string.IsNullOrEmpty(partnerId) ? null : Guid.Parse(partnerId),
                (string?)r.lp_name,
                (int)r.lp_sla_days,
                (DateTime)r.lo_ordered_at,
                dueDate,
                daysOverdue,
                (int?)r.lo_branch_id);
        }).ToList();

        return Result<IReadOnlyList<LabOrderOverdueResponse>>.Success(result.AsReadOnly());
    }
}
