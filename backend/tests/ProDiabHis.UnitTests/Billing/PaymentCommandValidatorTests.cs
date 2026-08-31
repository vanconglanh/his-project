using FluentAssertions;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.Pharmacy.Drugs;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Billing;

/// <summary>
/// BUG-04 (kiem soat tai chinh): chan so tien thanh toan &lt;= 0 va gia override &lt;= 0.
/// Loi goc: validator khai bao cho *Request nhung KHONG co lop boc cap Command ->
/// FluentValidation KHONG chay -> so tien 0/am/vuot lot qua (201). Test nay chay THANG
/// tren validator cap Command (dung kieu MediatR dispatch thuc te) de khoa lai hanh vi dung.
/// </summary>
public class PaymentCommandValidatorTests
{
    private static CreatePaymentRequest Req(decimal amount) =>
        new(BillingId: Guid.NewGuid(), Amount: amount, Method: "CASH",
            Reference: null, Provider: null, ProviderTxnId: null, Note: null);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50000)]
    public void CreatePaymentCommand_WithNonPositiveAmount_FailsValidation(decimal amount)
    {
        var validator = new CreatePaymentCommandValidator();
        var result = validator.Validate(new CreatePaymentCommand(Req(amount)));

        result.IsValid.Should().BeFalse("so tien <= 0 phai bi chan (BUG-04)");
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Amount"));
    }

    [Fact]
    public void CreatePaymentCommand_WithPositiveAmount_PassesValidation()
    {
        var validator = new CreatePaymentCommandValidator();
        var result = validator.Validate(new CreatePaymentCommand(Req(150000)));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-999999)]
    public void CreateServicePriceOverrideCommand_WithNonPositivePrice_FailsValidation(decimal price)
    {
        var req = new CreateServicePriceOverrideRequest(
            ServiceId: Guid.NewGuid(), Scope: PriceOverrideScope.Branch, BranchId: 1, GroupId: null,
            Price: price, IsActive: true, EffectiveFrom: DateOnly.FromDateTime(DateTime.Today),
            EffectiveTo: null, Note: null);

        var validator = new CreateServicePriceOverrideCommandValidator();
        var result = validator.Validate(new CreateServicePriceOverrideCommand(req));

        result.IsValid.Should().BeFalse("gia override <= 0 phai bi chan (BUG-04, da chung minh -999.999d lot qua)");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-12345)]
    public void CreateDrugPriceOverrideCommand_WithNonPositivePrice_FailsValidation(decimal price)
    {
        var req = new CreateDrugPriceOverrideRequest(
            DrugId: "d-1", Scope: PriceOverrideScope.Branch, BranchId: 1, GroupId: null,
            Price: price, IsActive: true, EffectiveFrom: DateOnly.FromDateTime(DateTime.Today),
            EffectiveTo: null, Note: null);

        var validator = new CreateDrugPriceOverrideCommandValidator();
        var result = validator.Validate(new CreateDrugPriceOverrideCommand(req));

        result.IsValid.Should().BeFalse("gia override thuoc <= 0 phai bi chan (BUG-04)");
    }
}
