namespace ProDiabHis.Application.LabResults;

/// <inheritdoc/>
public class LabResultFlagCalculator : ILabResultFlagCalculator
{
    // Nguong phan tang: ngoai 50% khoang tham chieu -> HH/LL; ngoai 20% -> Critical se override
    // Logic don gian theo spec: NORMAL / H / L / HH / LL / CRITICAL
    // CRITICAL = ngoai >=50% khoang tham chieu
    // HH/LL   = ngoai >=20% khoang tham chieu
    // H/L     = ngoai khoang tham chieu
    // NORMAL  = trong khoang

    private const double HhThreshold       = 0.50;  // 50% ngoai khoang -> HH/LL
    private const double CriticalThreshold  = 1.00;  // 100% ngoai khoang -> CRITICAL

    public string Calculate(decimal? valueNumeric, decimal? low, decimal? high)
    {
        if (valueNumeric is null) return LabResultFlag.Normal;
        if (low is null && high is null) return LabResultFlag.Normal;

        var v = (double)valueNumeric.Value;

        // Chi co high
        if (low is null && high is not null)
        {
            var h = (double)high.Value;
            if (v > h * (1 + CriticalThreshold)) return LabResultFlag.Critical;
            if (v > h * (1 + HhThreshold))       return LabResultFlag.HH;
            if (v > h)                            return LabResultFlag.H;
            return LabResultFlag.Normal;
        }

        // Chi co low
        if (high is null && low is not null)
        {
            var l = (double)low.Value;
            if (l > 0 && v < l * (1 - CriticalThreshold)) return LabResultFlag.Critical;
            if (l > 0 && v < l * (1 - HhThreshold))       return LabResultFlag.LL;
            if (v < l)                                      return LabResultFlag.L;
            return LabResultFlag.Normal;
        }

        // Ca hai dau
        {
            var l = (double)low!.Value;
            var h = (double)high!.Value;
            var range = h - l;

            if (range <= 0) return v >= l && v <= h ? LabResultFlag.Normal : LabResultFlag.H;

            if (v > h + range * CriticalThreshold) return LabResultFlag.Critical;
            if (v < l - range * CriticalThreshold) return LabResultFlag.Critical;
            if (v > h + range * HhThreshold)       return LabResultFlag.HH;
            if (v < l - range * HhThreshold)       return LabResultFlag.LL;
            if (v > h)                             return LabResultFlag.H;
            if (v < l)                             return LabResultFlag.L;
            return LabResultFlag.Normal;
        }
    }
}
