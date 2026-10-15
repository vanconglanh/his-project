namespace ProDiabHis.Application.LabResults;

/// <inheritdoc/>
public class LabResultFlagCalculator : ILabResultFlagCalculator
{
    // Nguong phan tang mac dinh: ngoai 50% khoang tham chieu -> HH/LL, 100% -> CRITICAL
    private const double DefaultHhThreshold       = 0.50;
    private const double DefaultCriticalThreshold  = 1.00;

    // BM-68: Cac test co khoang tham chieu hep nhung khong nguy hiem lam sang ngay ca khi cao >50%
    // (HbA1c: khoang 4-6%, nhung 8.5% chi la "Cao" lam sang, khong CRITICAL).
    // Nguong: HH khi ngoai 150% khoang, CRITICAL khi ngoai 300% khoang (HbA1c 4-6%: HH=9%, CRITICAL=12%).
    private static readonly HashSet<string> WideThresholdTestCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "HBA1C", "HbA1c", "GLYCO_HB", "A1C",
        "TSH",   // TSH cung co khoang hep 0.4-4.0, gia tri 10 TSH nguy hiem nhung khong bang 100% range
        "FT3",   "FT4",
    };

    private (double hh, double critical) GetThresholds(string? testCode)
    {
        if (testCode is not null && WideThresholdTestCodes.Contains(testCode))
            return (1.50, 3.00);
        return (DefaultHhThreshold, DefaultCriticalThreshold);
    }

    public string Calculate(decimal? valueNumeric, decimal? low, decimal? high)
        => Calculate(valueNumeric, low, high, DefaultHhThreshold, DefaultCriticalThreshold);

    public string CalculateForTest(string? testCode, decimal? valueNumeric, decimal? low, decimal? high)
    {
        var (hh, critical) = GetThresholds(testCode);
        return Calculate(valueNumeric, low, high, hh, critical);
    }

    // BM-68: nguong nguy kich tuyet doi cau hinh per-test.
    // value >= criticalHigh -> CRITICAL; value > high -> H; value <= criticalLow -> CRITICAL; value < low -> L.
    public string CalculateForTest(string? testCode, decimal? valueNumeric, decimal? low, decimal? high,
        decimal? criticalLow, decimal? criticalHigh)
    {
        if (criticalLow is null && criticalHigh is null)
            return CalculateForTest(testCode, valueNumeric, low, high);
        if (valueNumeric is null) return LabResultFlag.Normal;

        var v = valueNumeric.Value;
        if (criticalHigh is not null && v >= criticalHigh.Value) return LabResultFlag.Critical;
        if (criticalLow  is not null && v <= criticalLow.Value)  return LabResultFlag.Critical;
        if (high is not null && v > high.Value) return LabResultFlag.H;
        if (low  is not null && v < low.Value)  return LabResultFlag.L;
        return LabResultFlag.Normal;
    }

    // BM-68: overload nhan nguong tuy chinh de test co khoang tham chieu hep (HbA1c, TSH...)
    // khong bi cat sai muc CRITICAL khi gia tri chi "Cao" (H) ve mat lam sang.
    // Vd HbA1c ref 4-6%: voi DefaultHhThreshold 50%  nguong HH = 7%, CRITICAL = 8% ->
    //    8.5% bi cat CRITICAL sai. Truyen hhThresholdRatio=1.50, criticalThresholdRatio=3.00 ->
    //    nguong HH = 9%, CRITICAL = 12% -> 8.5% duoc cat HH (Rat cao) dung hon.
    public string Calculate(decimal? valueNumeric, decimal? low, decimal? high,
        double hhThresholdRatio, double criticalThresholdRatio)
    {
        if (valueNumeric is null) return LabResultFlag.Normal;
        if (low is null && high is null) return LabResultFlag.Normal;

        var v = (double)valueNumeric.Value;

        // Chi co high
        if (low is null && high is not null)
        {
            var h = (double)high.Value;
            if (v > h * (1 + criticalThresholdRatio)) return LabResultFlag.Critical;
            if (v > h * (1 + hhThresholdRatio))       return LabResultFlag.HH;
            if (v > h)                                 return LabResultFlag.H;
            return LabResultFlag.Normal;
        }

        // Chi co low
        if (high is null && low is not null)
        {
            var l = (double)low.Value;
            if (l > 0 && v < l * (1 - criticalThresholdRatio)) return LabResultFlag.Critical;
            if (l > 0 && v < l * (1 - hhThresholdRatio))       return LabResultFlag.LL;
            if (v < l)                                           return LabResultFlag.L;
            return LabResultFlag.Normal;
        }

        // Ca hai dau
        {
            var l = (double)low!.Value;
            var h = (double)high!.Value;
            var range = h - l;

            if (range <= 0) return v >= l && v <= h ? LabResultFlag.Normal : LabResultFlag.H;

            if (v > h + range * criticalThresholdRatio) return LabResultFlag.Critical;
            if (v < l - range * criticalThresholdRatio) return LabResultFlag.Critical;
            if (v > h + range * hhThresholdRatio)       return LabResultFlag.HH;
            if (v < l - range * hhThresholdRatio)       return LabResultFlag.LL;
            if (v > h)                                  return LabResultFlag.H;
            if (v < l)                                  return LabResultFlag.L;
            return LabResultFlag.Normal;
        }
    }
}
