namespace ProDiabHis.Application.Reports;

/// <summary>
/// Phan nhom cong no qua han theo tuoi no (age = so ngay ke tu ngay lap hoa don den moc as_of).
/// Tach ra pure function de unit test doc lap khoi DB (theo dung pattern DiabetesCohortCalculator).
/// </summary>
public static class DebtAgingCalculator
{
    public static DebtsAgingResponse Calculate(IReadOnlyList<DebtDetailItem> details)
    {
        decimal b0To30 = 0, b30To60 = 0, b60To90 = 0, bOver90 = 0;

        foreach (var d in details)
        {
            if (d.Balance <= 0) continue;

            if (d.DaysOverdue <= 30) b0To30 += d.Balance;
            else if (d.DaysOverdue <= 60) b30To60 += d.Balance;
            else if (d.DaysOverdue <= 90) b60To90 += d.Balance;
            else bOver90 += d.Balance;
        }

        var total = b0To30 + b30To60 + b60To90 + bOver90;
        return new DebtsAgingResponse(b0To30, b30To60, b60To90, bOver90, total, details);
    }
}
