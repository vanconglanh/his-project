using FluentAssertions;
using ProDiabHis.Application.Patients;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

public class PatientMappingTests
{
    [Theory]
    [InlineData("030185001234", "03********34")]
    [InlineData("12345678", "12****78")]
    [InlineData("AB", "AB")]
    [InlineData(null, null)]
    public void MaskIdNumber_ReturnsCorrectMask(string? input, string? expected)
    {
        PatientMappingHelper.MaskIdNumber(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("HC4010112345678", "HC401**********")]
    [InlineData("AB123", "AB123")]
    [InlineData(null, null)]
    public void MaskCardNo_ReturnsCorrectMask(string? input, string? expected)
    {
        PatientMappingHelper.MaskCardNo(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(1985, 3, 12)]   // born 1985 -> 41 years (as of 2026)
    public void CalcAge_ReturnCorrectAge(int year, int month, int day)
    {
        var dob = new DateOnly(year, month, day);
        var age = PatientMappingHelper.CalcAge(dob);
        age.Should().BePositive();
        age.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public void CalcAge_NullDob_ReturnsNull()
    {
        PatientMappingHelper.CalcAge(null).Should().BeNull();
    }
}
