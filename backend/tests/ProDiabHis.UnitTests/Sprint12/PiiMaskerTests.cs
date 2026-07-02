using FluentAssertions;
using ProDiabHis.Infrastructure.Security;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint12;

public class PiiMaskerTests
{
    private readonly PiiMaskerImpl _masker = new();

    [Theory]
    [InlineData("079201012345", "07***45")]
    [InlineData("123456789", "12***89")]
    [InlineData("AB", "***")]
    [InlineData(null, "***")]
    public void MaskNationalId_ShouldMaskCorrectly(string? input, string expected)
    {
        _masker.MaskNationalId(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("DN1234567890123", "DN", "0123")] // prefix 2, suffix last-4
    [InlineData("DN123456789012345", "DN", "2345")]
    public void MaskBhyt_LongValue_ShouldKeepPrefixAndSuffix(string input, string expectedPrefix, string expectedSuffix)
    {
        var result = _masker.MaskBhyt(input);
        result.Should().StartWith(expectedPrefix);
        result.Should().EndWith(expectedSuffix);
        result.Should().Contain("***");
    }

    [Theory]
    [InlineData("AB")]
    [InlineData(null)]
    public void MaskBhyt_ShortOrNull_ShouldReturnMask(string? input)
    {
        _masker.MaskBhyt(input).Should().Be("***");
    }

    [Theory]
    [InlineData("0981234567", "098***67")]
    [InlineData("0912345678", "091***78")]
    [InlineData("123", "***")]
    [InlineData(null, "***")]
    public void MaskPhone_ShouldMaskCorrectly(string? input, string expected)
    {
        _masker.MaskPhone(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("nguyen@gmail.com", "ng***@gmail.com")]
    [InlineData("a@b.com", "***@b.com")]
    [InlineData("notanemail", "***")]
    [InlineData(null, "***")]
    public void MaskEmail_ShouldMaskCorrectly(string? input, string expected)
    {
        _masker.MaskEmail(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Nguyen Van An", "Nguyen Van A.")]
    [InlineData("Tran Thi Bich", "Tran Thi B.")]
    [InlineData("Lam", "L.")]
    [InlineData(null, "***")]
    public void MaskFullName_ShouldMaskCorrectly(string? input, string expected)
    {
        _masker.MaskFullName(input).Should().Be(expected);
    }
}
