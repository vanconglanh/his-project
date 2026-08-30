using FluentAssertions;
using ProDiabHis.Application.Patients;
using ProDiabHis.Domain.Entities;
using Xunit;

namespace ProDiabHis.UnitTests.Patients;

/// <summary>Unit test cho CccdQrParser — theo BR-QR-001..005 trong PRD quet-qr-cccd-20260830.md</summary>
public class CccdQrParserTests
{
    private const string ValidQr =
        "001099012345|001234567890|Nguyễn Văn A|15031999|Nam|Số 1 Đường ABC, Phường X, Quận Y, TP Z|10052021";

    [Fact]
    public void Parse_ValidQrString_ReturnsAllFieldsCorrectly()
    {
        var result = CccdQrParser.Parse(ValidQr);

        result.Success.Should().BeTrue();
        result.Data!.IdNumber.Should().Be("001099012345");
        result.Data.OldIdNumber.Should().Be("001234567890");
        result.Data.FullName.Should().Be("Nguyễn Văn A");
        result.Data.DateOfBirth.Should().Be(new DateOnly(1999, 3, 15));
        result.Data.Gender.Should().Be(Gender.Male);
        result.Data.Address.Should().Be("Số 1 Đường ABC, Phường X, Quận Y, TP Z");
        result.Data.IssuedDate.Should().Be(new DateOnly(2021, 5, 10));
        result.Data.HasEncodingWarning.Should().BeFalse();
    }

    [Theory]
    [InlineData("Nữ")]
    [InlineData("nu")]
    public void Parse_FemaleGenderVariants_MapsToFemale(string genderText)
    {
        var qr = $"001099012345|001234567890|Nguyễn Thị B|15031999|{genderText}|Địa chỉ|10052021";
        var result = CccdQrParser.Parse(qr);

        result.Data!.Gender.Should().Be(Gender.Female);
    }

    [Fact]
    public void Parse_GenderNotNamNu_LeavesGenderEmpty()
    {
        var qr = "001099012345|001234567890|X|15031999|Khac|Địa chỉ|10052021";
        var result = CccdQrParser.Parse(qr);

        result.Success.Should().BeTrue();
        result.Data!.Gender.Should().BeNull();
    }

    // ── BR-QR-001: sai so field ──
    [Theory]
    [InlineData("001099012345|001234567890|Nguyễn Văn A|15031999|Nam|Địa chỉ")] // 6 field
    [InlineData("001099012345|001234567890|Nguyễn Văn A|15031999|Nam|Địa chỉ|10052021|thua")] // 8 field
    [InlineData("")]
    public void Parse_WrongFieldCount_FailsWithoutFillingAnything(string raw)
    {
        var result = CccdQrParser.Parse(raw);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
        result.ErrorCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_NullInput_FailsGracefully()
    {
        var result = CccdQrParser.Parse(null);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("CCCD_QR_EMPTY");
    }

    // ── BR-QR-002 + BR-QR-003: ngay sinh sai dinh dang -> field do de trong, cac field khac van dien ──
    [Theory]
    [InlineData("32131999")] // ngay/thang khong hop le
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1999")]
    public void Parse_InvalidDateOfBirth_LeavesDateEmptyButKeepsOtherFields(string dobRaw)
    {
        var qr = $"001099012345|001234567890|Nguyễn Văn A|{dobRaw}|Nam|Địa chỉ ABC|10052021";
        var result = CccdQrParser.Parse(qr);

        result.Success.Should().BeTrue("từng field xử lý độc lập, không throw vỡ luồng (BR-QR-002)");
        result.Data!.DateOfBirth.Should().BeNull();
        result.Data.FullName.Should().Be("Nguyễn Văn A");
        result.Data.IdNumber.Should().Be("001099012345");
    }

    [Fact]
    public void Parse_InvalidIssuedDate_LeavesIssuedDateEmpty()
    {
        var qr = "001099012345|001234567890|Nguyễn Văn A|15031999|Nam|Địa chỉ ABC|invalid";
        var result = CccdQrParser.Parse(qr);

        result.Success.Should().BeTrue();
        result.Data!.IssuedDate.Should().BeNull();
        result.Data.DateOfBirth.Should().Be(new DateOnly(1999, 3, 15));
    }

    // ── BR-QR-005: phat hien ky tu thay the do loi encoding ──
    [Fact]
    public void Parse_ReplacementCharacterInName_StillFillsButFlagsEncodingWarning()
    {
        var qr = "001099012345|001234567890|Nguy�n V�n A|15031999|Nam|Địa chỉ ABC|10052021";
        var result = CccdQrParser.Parse(qr);

        result.Success.Should().BeTrue();
        result.Data!.FullName.Should().Contain("�");
        result.Data.HasEncodingWarning.Should().BeTrue();
    }

    [Fact]
    public void Parse_EmptyOptionalFields_LeavesThemNull()
    {
        var qr = "001099012345||Nguyễn Văn A|15031999|Nam||10052021";
        var result = CccdQrParser.Parse(qr);

        result.Success.Should().BeTrue();
        result.Data!.OldIdNumber.Should().BeNull();
        result.Data.Address.Should().BeNull();
    }
}
