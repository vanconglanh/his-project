using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ProDiabHis.Infrastructure.Lab;
using Xunit;

namespace ProDiabHis.UnitTests.LabResults;

public class HmacSignatureVerifierTests
{
    private readonly HmacSignatureVerifier _sut = new();

    private static string ComputeHmac(string secret, byte[] body)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, body);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Happy path: chu ky hop le
    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var secret    = "my-super-secret";
        var body      = Encoding.UTF8.GetBytes("{\"test\":1}");
        var signature = ComputeHmac(secret, body);

        _sut.Verify(secret, body, signature).Should().BeTrue();
    }

    // Chu ky sai
    [Fact]
    public void Verify_WrongSignature_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{\"test\":1}");
        _sut.Verify("secret", body, "deadbeef").Should().BeFalse();
    }

    // Body bi thay doi
    [Fact]
    public void Verify_TamperedBody_ReturnsFalse()
    {
        var secret    = "my-super-secret";
        var body      = Encoding.UTF8.GetBytes("{\"test\":1}");
        var signature = ComputeHmac(secret, body);
        var tampered  = Encoding.UTF8.GetBytes("{\"test\":2}");

        _sut.Verify(secret, tampered, signature).Should().BeFalse();
    }

    // Secret sai
    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var body      = Encoding.UTF8.GetBytes("hello");
        var signature = ComputeHmac("correct-secret", body);
        _sut.Verify("wrong-secret", body, signature).Should().BeFalse();
    }

    // Signature rong
    [Fact]
    public void Verify_EmptySignature_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("hello");
        _sut.Verify("secret", body, "").Should().BeFalse();
    }

    // Signature voi prefix sha256= (dang GitHub webhook)
    [Fact]
    public void Verify_WithSha256Prefix_ReturnsTrue()
    {
        var secret    = "github-style";
        var body      = Encoding.UTF8.GetBytes("payload");
        var hash      = ComputeHmac(secret, body);
        var signature = "sha256=" + hash;

        _sut.Verify(secret, body, signature).Should().BeTrue();
    }
}
