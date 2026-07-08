namespace ProDiabHis.Application.Reports;

/// <summary>
/// Tinh toan xu huong dien bien va nguy co benh nhan DTD. Thuan (khong goi DB) de
/// unit test doc lap.
/// </summary>
public static class DiabetesTrendCalculator
{
    public record AssessmentPoint(DateTime AssessedAt, decimal? Hba1c, int? BpSys, int? BpDia);

    public record DeteriorationFlag(string Code, string Message, string Severity);

    /// <summary>
    /// Phat hien dau hieu dien bien xau tu day cac ky danh gia (sap xep theo AssessedAt tang dan
    /// hoac giam dan deu duoc — ham se tu sap xep lai).
    /// </summary>
    public static List<DeteriorationFlag> DetectDeterioration(
        IReadOnlyList<AssessmentPoint> points, decimal hba1cTarget, int bpSysTarget, int bpDiaTarget)
    {
        var flags = new List<DeteriorationFlag>();
        if (points is null || points.Count == 0) return flags;

        var ordered = points.OrderBy(p => p.AssessedAt).ToList();

        // HbA1c tang >= 0.5 giua 2 ky gan nhat
        var withHba1c = ordered.Where(p => p.Hba1c.HasValue).ToList();
        if (withHba1c.Count >= 2)
        {
            var last = withHba1c[^1].Hba1c!.Value;
            var prev = withHba1c[^2].Hba1c!.Value;
            if (last - prev >= 0.5m)
            {
                flags.Add(new DeteriorationFlag("HBA1C_RISING",
                    $"HbA1c tăng {last - prev:0.0}% so với kỳ trước ({prev:0.0}% → {last:0.0}%)", "MEDIUM"));
            }
        }

        // HbA1c vuot target 2 ky lien tiep gan nhat
        if (withHba1c.Count >= 2)
        {
            var lastTwo = withHba1c.TakeLast(2).ToList();
            if (lastTwo.All(p => p.Hba1c!.Value > hba1cTarget))
            {
                flags.Add(new DeteriorationFlag("HBA1C_ABOVE_TARGET_2X",
                    $"HbA1c vượt mục tiêu ({hba1cTarget:0.0}%) 2 kỳ liên tiếp gần nhất", "HIGH"));
            }
        }

        // BP vuot target 2 ky lien tiep gan nhat
        var withBp = ordered.Where(p => p.BpSys.HasValue && p.BpDia.HasValue).ToList();
        if (withBp.Count >= 2)
        {
            var lastTwoBp = withBp.TakeLast(2).ToList();
            if (lastTwoBp.All(p => p.BpSys!.Value > bpSysTarget || p.BpDia!.Value > bpDiaTarget))
            {
                flags.Add(new DeteriorationFlag("BP_ABOVE_TARGET_2X",
                    $"Huyết áp vượt mục tiêu ({bpSysTarget}/{bpDiaTarget} mmHg) 2 kỳ liên tiếp gần nhất", "HIGH"));
            }
        }

        return flags;
    }

    /// <summary>
    /// Tinh diem nguy co cong don. Xem CLAUDE.md instructions cho cong thuc chi tiet.
    /// </summary>
    public static int ComputeRiskScore(
        decimal? hba1c, decimal? egfr, int? bpSys, bool risingTrend, bool overdueVisit)
    {
        var score = 0;

        if (hba1c.HasValue)
        {
            if (hba1c.Value >= 9m) score += 3;
            else if (hba1c.Value >= 8m) score += 2;
            else if (hba1c.Value >= 7m) score += 1;
        }

        if (egfr.HasValue)
        {
            if (egfr.Value < 30m) score += 3;
            else if (egfr.Value < 45m) score += 2;
        }

        if (bpSys.HasValue && bpSys.Value >= 140) score += 1;
        if (risingTrend) score += 1;
        if (overdueVisit) score += 1;

        return score;
    }

    /// <summary>Phan loai muc nguy co tu diem so. HIGH>=4, MEDIUM 2-3, LOW&lt;2.</summary>
    public static string ClassifyRisk(int score) => score switch
    {
        >= 4 => "HIGH",
        >= 2 => "MEDIUM",
        _ => "LOW"
    };
}
