using FluentValidation.TestHelper;
using ProDiabHis.Application.Patients;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>BM-90: Hình thức đến khám có 3 giá trị hợp lệ.</summary>
public class PatientVisitTypeValidatorTests
{
    private static CreatePatientRequest Create(string? visitType) => new(
        "Nguyen Van Test", "MALE", new DateOnly(1990, 5, 20), null, null, null, null,
        null, null, null, null, null, VisitType: visitType);

    private static UpdatePatientRequest Update(string? visitType) => new(
        "Nguyen Van Test", "MALE", new DateOnly(1990, 5, 20), null, null, null, null,
        null, null, null, null, VisitType: visitType);

    [Theory]
    [InlineData("FIRST_VISIT")]
    [InlineData("RE_EXAM")]
    [InlineData("RE_EXAM_IN_PACKAGE")]
    [InlineData(null)]
    public void Create_Accepts_Valid_VisitType(string? v) =>
        new CreatePatientRequestValidator().TestValidate(Create(v)).ShouldNotHaveValidationErrorFor(x => x.VisitType);

    [Theory]
    [InlineData("FIRST_VISIT")]
    [InlineData("RE_EXAM")]
    [InlineData("RE_EXAM_IN_PACKAGE")]
    [InlineData(null)]
    public void Update_Accepts_Valid_VisitType(string? v) =>
        new UpdatePatientRequestValidator().TestValidate(Update(v)).ShouldNotHaveValidationErrorFor(x => x.VisitType);

    [Fact]
    public void Create_Rejects_Unknown_VisitType() =>
        new CreatePatientRequestValidator().TestValidate(Create("XYZ")).ShouldHaveValidationErrorFor(x => x.VisitType);

    [Fact]
    public void Update_Rejects_Unknown_VisitType() =>
        new UpdatePatientRequestValidator().TestValidate(Update("XYZ")).ShouldHaveValidationErrorFor(x => x.VisitType);
}
