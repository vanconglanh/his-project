using Dapper;
using MediatR;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Application.PublicApi;

// ============================================================
// Portal: Xu huong suc khoe — chuoi ket qua XN so (value_numeric) theo thoi gian.
// Nguon: diab_his_lab_results (chi status VERIFIED). Gom theo test_code, moi chi so
// tra gia tri moi nhat + chuoi diem de ve bieu do (sparkline) tren Trang chu.
// ============================================================
public record HealthTrendPoint(DateTime Date, decimal Value);

public record HealthTrendMetric(
    string TestCode,
    string TestName,
    string? Unit,
    decimal LatestValue,
    string? LatestFlag,
    DateTime LatestDate,
    List<HealthTrendPoint> Series);

public record GetPortalHealthTrendsQuery(Guid PatientId, int TenantId) : IRequest<List<HealthTrendMetric>>;

public class GetPortalHealthTrendsHandler : IRequestHandler<GetPortalHealthTrendsQuery, List<HealthTrendMetric>>
{
    private readonly IDapperConnectionFactory _db;
    public GetPortalHealthTrendsHandler(IDapperConnectionFactory db) => _db = db;

    public async Task<List<HealthTrendMetric>> Handle(GetPortalHealthTrendsQuery q, CancellationToken cancellationToken)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<dynamic>(
            @"SELECT test_code, test_name, unit, flag, value_numeric, performed_at
              FROM diab_his_lab_results
              WHERE patient_id = @PatientId AND tenant_id = @TenantId
                AND status = 'VERIFIED' AND deleted_at IS NULL AND value_numeric IS NOT NULL
              ORDER BY test_code, performed_at",
            new { PatientId = q.PatientId.ToString(), q.TenantId });

        // Gom theo test_code, giu toi da 12 diem gan nhat moi chi so
        var groups = rows
            .Where(r => !string.IsNullOrWhiteSpace((string?)r.test_code))
            .GroupBy(r => (string)r.test_code);

        var metrics = new List<HealthTrendMetric>();
        foreach (var g in groups)
        {
            var pts = g.Select(r => new HealthTrendPoint(
                        (DateTime)r.performed_at, (decimal)r.value_numeric))
                       .OrderBy(p => p.Date)
                       .ToList();
            if (pts.Count == 0) continue;
            var last = g.OrderByDescending(r => (DateTime)r.performed_at).First();

            metrics.Add(new HealthTrendMetric(
                g.Key,
                (string?)last.test_name ?? g.Key,
                (string?)last.unit,
                (decimal)last.value_numeric,
                (string?)last.flag,
                (DateTime)last.performed_at,
                pts.Count > 12 ? pts.Skip(pts.Count - 12).ToList() : pts));
        }

        // Chi so co nhieu diem (co xu huong) len truoc, roi theo moi nhat
        return metrics
            .OrderByDescending(m => m.Series.Count)
            .ThenByDescending(m => m.LatestDate)
            .Take(6)
            .ToList();
    }
}
