using FluentAssertions;
using NSubstitute;
using ProDiabHis.Application.EMR;
using Xunit;

namespace ProDiabHis.UnitTests.EMR;

/// <summary>
/// Unit test EMR sign flow (US-E09):
/// - Mock verifier always accepts
/// - Verify interface contract
/// </summary>
public class EmrSignFlowTests
{
    [Fact]
    public async Task MockVerifier_AcceptsAnySignature_IsValid()
    {
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.EMR.MockEmrSignatureVerifier>>();
        var verifier = new ProDiabHis.Infrastructure.EMR.MockEmrSignatureVerifier(logger);

        var content = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"doc\"}");
        var signature = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };

        var result = await verifier.VerifyAsync(content, signature);

        result.IsValid.Should().BeTrue();
        result.CertificateSerial.Should().NotBeNullOrEmpty();
        result.CertificateSubject.Should().Contain("MOCK_CERT");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MockVerifier_LogsWarning()
    {
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.EMR.MockEmrSignatureVerifier>>();
        var verifier = new ProDiabHis.Infrastructure.EMR.MockEmrSignatureVerifier(logger);

        await verifier.VerifyAsync(new byte[] { 1, 2 }, new byte[] { 3, 4 });

        logger.Received().Log(
            Microsoft.Extensions.Logging.LogLevel.Warning,
            Arg.Any<Microsoft.Extensions.Logging.EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void SignEmrRequest_WithBase64_CanDecode()
    {
        var originalBytes = new byte[] { 1, 2, 3, 4, 5 };
        var base64 = Convert.ToBase64String(originalBytes);

        var decoded = Convert.FromBase64String(base64);
        decoded.Should().Equal(originalBytes);
    }

    [Fact]
    public void EmrSignatureVerifyResult_Fields_AreCorrect()
    {
        var result = new EmrSignatureVerifyResult(
            IsValid: true,
            CertificateSerial: "ABC123",
            CertificateSubject: "CN=Test",
            Algorithm: "SHA256withRSA",
            ErrorMessage: null);

        result.IsValid.Should().BeTrue();
        result.CertificateSerial.Should().Be("ABC123");
        result.Algorithm.Should().Be("SHA256withRSA");
    }
}
