using FluentAssertions;
using ProDiabHis.Application.Billing.InterBranchDebts;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// Dot 4 da chi nhanh — BR-85 (tra no cheo chi nhanh): logic tinh debtor/creditor cho but toan
/// cong no noi bo khi thu ngan thu tien hoa don thuoc chi nhanh khac.
/// </summary>
public class InterBranchDebtLogicTests
{
    // AC-5.2.1: thu ngan CN Thu Duc (2) thu ho hoa don CN Q1 (1) -> debtor = 2 (Thu Duc, giu ho tien),
    // creditor = 1 (Q1, duoc no).
    [Fact]
    public void KhacChiNhanh_PhaiSinhCongNoNoiBo_DungDebtorCreditor()
    {
        var result = InterBranchDebtCalculator.ComputeForCrossBranchPayment(billingBranchId: 1, currentBranchId: 2);

        result.Should().NotBeNull();
        result!.Value.DebtorBranchId.Should().Be(2);
        result.Value.CreditorBranchId.Should().Be(1);
    }

    // Cung chi nhanh -> KHONG sinh cong no noi bo.
    [Fact]
    public void CungChiNhanh_KhongDuocSinhCongNoNoiBo()
    {
        var result = InterBranchDebtCalculator.ComputeForCrossBranchPayment(billingBranchId: 3, currentBranchId: 3);

        result.Should().BeNull();
    }

    // Hoa don chua gan chi nhanh (du lieu cu / migrate) -> an toan, khong sinh.
    [Fact]
    public void BillingChuaGanChiNhanh_KhongDuocSinhCongNoNoiBo()
    {
        var result = InterBranchDebtCalculator.ComputeForCrossBranchPayment(billingBranchId: null, currentBranchId: 2);

        result.Should().BeNull();
    }
}
