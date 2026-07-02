namespace ProDiabHis.Application.LabResults;

/// <summary>
/// Tinh flag NORMAL/H/L/HH/LL/CRITICAL dua tren gia tri so va khoang tham chieu.
/// Logic phan tang: ngoai +-50% = HH/LL, ngoai khoang tham chieu = H/L, trong = NORMAL.
/// </summary>
public interface ILabResultFlagCalculator
{
    string Calculate(decimal? valueNumeric, decimal? low, decimal? high);
}
