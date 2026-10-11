using FluentValidation.TestHelper;
using ProDiabHis.Application.Patients;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>
/// BM-81: khi TAO benh nhan moi bat buoc toi thieu ngay sinh.
/// </summary>
public class CreatePatientRequestValidatorTests
{
    private readonly CreatePatientRequestValidator _validator = new();

    private static CreatePatientRequest ValidRequest(DateOnly? dob) => new(
        FullName: "Nguyen Van Test",
        Gender: "MALE",
        DateOfBirth: dob,
        IdNumber: null,
        Phone: null,
        Email: null,
        Address: null,
        Occupation: null,
        Ethnicity: null,
        BloodType: null,
        IdCardIssuedDate: null,
        IdCardIssuedPlace: null);

    [Fact]
    public void Missing_DateOfBirth_Is_Rejected()
    {
        var result = _validator.TestValidate(ValidRequest(null));
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Vui lòng nhập ngày sinh");
    }

    [Fact]
    public void Present_DateOfBirth_Passes()
    {
        var result = _validator.TestValidate(ValidRequest(new DateOnly(1990, 5, 20)));
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }
}
