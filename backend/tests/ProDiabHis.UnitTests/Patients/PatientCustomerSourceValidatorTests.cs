using FluentValidation.TestHelper;
using ProDiabHis.Application.Patients;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>BM-91: nguồn khách + chi tiết nguồn.</summary>
public class PatientCustomerSourceValidatorTests
{
    private static CreatePatientRequest Create(string? src, string? detail) => new(
        "Nguyen Van Test", "MALE", new DateOnly(1990, 5, 20), null, null, null, null,
        null, null, null, null, null, CustomerSource: src, CustomerSourceDetail: detail);

    private static UpdatePatientRequest Update(string? src, string? detail) => new(
        "Nguyen Van Test", "MALE", new DateOnly(1990, 5, 20), null, null, null, null,
        null, null, null, null, CustomerSource: src, CustomerSourceDetail: detail);

    [Theory]
    [InlineData("WALK_IN", null)]
    [InlineData("MKT", null)]
    [InlineData("REFERRAL", "BS Minh")]
    [InlineData("B2B", "Cong ty ABC")]
    [InlineData(null, null)]
    public void Create_Valid_Combinations_Pass(string? s, string? d) =>
        new CreatePatientRequestValidator().TestValidate(Create(s, d)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Create_Unknown_Source_Rejected() =>
        new CreatePatientRequestValidator().TestValidate(Create("ABC", null))
            .ShouldHaveValidationErrorFor(x => x.CustomerSource);

    [Theory]
    [InlineData("REFERRAL")]
    [InlineData("B2B")]
    public void Create_Referral_Or_B2B_Requires_Detail(string s) =>
        new CreatePatientRequestValidator().TestValidate(Create(s, " "))
            .ShouldHaveValidationErrorFor(x => x.CustomerSourceDetail);

    [Fact]
    public void Update_Unknown_Source_Rejected() =>
        new UpdatePatientRequestValidator().TestValidate(Update("ABC", null))
            .ShouldHaveValidationErrorFor(x => x.CustomerSource);

    [Fact]
    public void Update_B2B_Requires_Detail() =>
        new UpdatePatientRequestValidator().TestValidate(Update("B2B", null))
            .ShouldHaveValidationErrorFor(x => x.CustomerSourceDetail);

    [Fact]
    public void Update_Referral_With_Detail_Passes() =>
        new UpdatePatientRequestValidator().TestValidate(Update("REFERRAL", "REF001"))
            .ShouldNotHaveAnyValidationErrors();
}
