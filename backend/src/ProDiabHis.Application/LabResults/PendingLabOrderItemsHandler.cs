using Dapper;
using MediatR;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Common.Interfaces;

namespace ProDiabHis.Application.LabResults;

// ═══════════════════════════════════════════════
// PENDING LAB ORDER ITEMS
// Danh sach chi dinh XN (diab_his_cli_lab_orders) CHUA co ket qua, phuc vu
// bo chon "chi dinh dang cho ket qua" khi nhap ket qua XN moi.
// ═══════════════════════════════════════════════

public record PendingLabOrderItemResponse(
    Guid     LabOrderItemId,
    Guid     EncounterId,
    Guid?    PatientId,
    string?  PatientName,
    string?  PatientCode,
    string   TestCode,
    string   TestName,
    string   Status,
    DateTime OrderedAt,
    string?  SampleType,
    string?  Priority);

public record ListPendingLabOrderItemsQuery(string? Q, int Limit)
    : IRequest<Result<IReadOnlyList<PendingLabOrderItemResponse>>>;

public class ListPendingLabOrderItemsQueryHandler
    : IRequestHandler<ListPendingLabOrderItemsQuery, Result<IReadOnlyList<PendingLabOrderItemResponse>>>
{
    private readonly IDapperConnectionFactory _db;
    private readonly ITenantProvider _tenant;

    public ListPendingLabOrderItemsQueryHandler(IDapperConnectionFactory db, ITenantProvider tenant)
    { _db = db; _tenant = tenant; }

    public async Task<Result<IReadOnlyList<PendingLabOrderItemResponse>>> Handle(
        ListPendingLabOrderItemsQuery q, CancellationToken ct)
    {
        var term  = (q.Q ?? string.Empty).Trim();
        var like  = $"%{term}%";
        var limit = Math.Clamp(q.Limit, 1, 100);

        const string sql = @"
            SELECT  o.id            AS Id,
                    o.encounter_id  AS EncounterId,
                    e.patient_id    AS PatientId,
                    p.full_name     AS PatientName,
                    p.code          AS PatientCode,
                    o.test_code     AS TestCode,
                    o.test_name     AS TestName,
                    o.status        AS Status,
                    o.ordered_at    AS OrderedAt,
                    o.sample_type   AS SampleType,
                    o.priority      AS Priority
            FROM diab_his_cli_lab_orders o
            JOIN diab_his_enc_encounters e
                 ON e.id = o.encounter_id AND e.tenant_id = o.tenant_id AND e.deleted_at IS NULL
            LEFT JOIN diab_his_pat_patients p
                 ON p.id = e.patient_id AND p.tenant_id = o.tenant_id
            WHERE o.tenant_id = @TId
              AND o.deleted_at IS NULL
              AND o.status <> 'cancelled'
              AND NOT EXISTS (
                    SELECT 1 FROM diab_his_lab_results r
                    WHERE r.lab_order_item_id = o.id
                      AND r.tenant_id = o.tenant_id
                      AND r.deleted_at IS NULL)
              AND (@Term = ''
                   OR p.full_name LIKE @Like
                   OR p.code      LIKE @Like
                   OR o.test_name LIKE @Like
                   OR o.test_code LIKE @Like)
            ORDER BY o.ordered_at DESC
            LIMIT @Limit";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(new CommandDefinition(sql,
            new { TId = _tenant.TenantId, Term = term, Like = like, Limit = limit },
            cancellationToken: ct));

        var items = new List<PendingLabOrderItemResponse>();
        foreach (var r in rows)
        {
            Guid.TryParse((string?)r.Id, out var itemId);
            Guid.TryParse((string?)r.EncounterId, out var encId);
            Guid? patientId = Guid.TryParse((string?)r.PatientId, out var pg) ? pg : (Guid?)null;

            items.Add(new PendingLabOrderItemResponse(
                itemId,
                encId,
                patientId,
                (string?)r.PatientName,
                (string?)r.PatientCode,
                (string)r.TestCode,
                (string)r.TestName,
                (string)r.Status,
                (DateTime)r.OrderedAt,
                (string?)r.SampleType,
                (string?)r.Priority));
        }

        return Result<IReadOnlyList<PendingLabOrderItemResponse>>.Success(items);
    }
}
