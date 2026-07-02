using ProDiabHis.Application.Reports;
using StackExchange.Redis;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh ma bao cao doc nhat dung Redis INCR.
/// Key pattern: report:seq:{tenantId}:{yyyyMMdd}:{TYPE}
/// TTL: 26 gio (dam bao seq reset moi ngay, du cho cac truong hop cross-midnight).
/// Format output: RPT-{FIN|CLN|PHA}-{yyyyMMdd}-{seq:D4}
/// Redis la bat buoc — khong co fallback in-memory de tranh sinh trung ma khi multi-instance.
/// </summary>
public class ReportCodeGenerator : IReportCodeGenerator
{
    private readonly IConnectionMultiplexer _redis;

    public ReportCodeGenerator(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis),
            "Redis bat buoc de sinh ma bao cao. Kiem tra cau hinh ConnectionStrings:Redis.");
    }

    private static string TypeCode(ReportType type) => type switch
    {
        ReportType.Financial => "FIN",
        ReportType.Clinical  => "CLN",
        ReportType.Pharmacy  => "PHA",
        _                    => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public async Task<string> NextAsync(int tenantId, ReportType type, DateOnly date, CancellationToken ct = default)
    {
        if (!_redis.IsConnected)
            throw new InvalidOperationException(
                "Redis bat buoc de sinh ma bao cao. Ket noi Redis hien khong kha dung.");

        var prefix = TypeCode(type);
        var dateStr = date.ToString("yyyyMMdd");
        var key = $"report:seq:{tenantId}:{dateStr}:{prefix}";

        var db = _redis.GetDatabase();
        long seq = await db.StringIncrementAsync(key);

        // Dat TTL 26 gio neu day la lan dau (seq == 1)
        if (seq == 1)
            await db.KeyExpireAsync(key, TimeSpan.FromHours(26));

        return $"RPT-{prefix}-{dateStr}-{seq:D4}";
    }
}
