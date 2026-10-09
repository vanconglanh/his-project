namespace ProDiabHis.Application.LabResults;

/// <summary>
/// Tinh flag NORMAL/H/L/HH/LL/CRITICAL dua tren gia tri so va khoang tham chieu.
/// Logic phan tang: ngoai +-50% = HH/LL, ngoai khoang tham chieu = H/L, trong = NORMAL.
/// BM-68: ho tro hhThresholdRatio/criticalThresholdRatio de test dac biet (HbA1c) dung nguong tuyet doi
/// thay vi nguong % - tranh cat HbA1c 8.5% sai thanh CRITICAL khi khoang tham chieu hep (4-6%).
/// </summary>
public interface ILabResultFlagCalculator
{
    string Calculate(decimal? valueNumeric, decimal? low, decimal? high);
    /// <param name="hhThresholdRatio">Ti le ngoai khoang coi la HH (mac dinh 0.50).</param>
    /// <param name="criticalThresholdRatio">Ti le ngoai khoang coi la CRITICAL (mac dinh 1.00).</param>
    string Calculate(decimal? valueNumeric, decimal? low, decimal? high,
        double hhThresholdRatio, double criticalThresholdRatio);
    /// BM-68: tinh flag voi nguong tu dong theo testCode (HbA1c, TSH dung nguong rong hon).
    string CalculateForTest(string? testCode, decimal? valueNumeric, decimal? low, decimal? high);
}
