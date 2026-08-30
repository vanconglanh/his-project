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
        var providerLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.Security.MockDigitalSignatureProvider>>();
        var provider = new ProDiabHis.Infrastructure.Security.MockDigitalSignatureProvider(providerLogger);
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.EMR.EmrSignatureVerifierAdapter>>();
        var verifier = new ProDiabHis.Infrastructure.EMR.EmrSignatureVerifierAdapter(provider, logger);

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
        var providerLogger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.Security.MockDigitalSignatureProvider>>();
        var provider = new ProDiabHis.Infrastructure.Security.MockDigitalSignatureProvider(providerLogger);
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProDiabHis.Infrastructure.EMR.EmrSignatureVerifierAdapter>>();
        var verifier = new ProDiabHis.Infrastructure.EMR.EmrSignatureVerifierAdapter(provider, logger);

        await verifier.VerifyAsync(new byte[] { 1, 2 }, new byte[] { 3, 4 });

        providerLogger.Received().Log(
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

    // ────────────────────────────────────────────────
    // §5.8.3 — payload chu ky v2 (gop content + structured_values + schema_snapshot).
    // Ky bang RSA SHA256 that (khong dung mock accept-all) de chung minh tinh chong sua doi.
    // ────────────────────────────────────────────────
    private static readonly string ContentJson = "{\"type\":\"doc\",\"content\":[]}";
    private static readonly string StructuredValues = "{\"hba1c\":7.2,\"bp_sys\":130}";
    private static readonly string SchemaSnapshot = "{\"fields\":[{\"key\":\"hba1c\",\"type\":\"number\"}]}";

    private static (byte[] sig, System.Security.Cryptography.RSA rsa) Sign(byte[] payload)
    {
        var rsa = System.Security.Cryptography.RSA.Create(2048);
        var hash = System.Security.Cryptography.SHA256.HashData(payload);
        var sig = rsa.SignHash(hash, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        return (sig, rsa);
    }

    private static bool Verify(byte[] payload, byte[] sig, System.Security.Cryptography.RSA rsa)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(payload);
        return rsa.VerifyHash(hash, sig, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
    }

    [Fact]
    public void V2_Sign_Then_Verify_Ok()
    {
        var payload = EmrSignPayload.Build(ContentJson, StructuredValues, SchemaSnapshot);
        var (sig, rsa) = Sign(payload);

        // Cung du lieu -> payload tai lap giong het -> verify OK
        var payloadAgain = EmrSignPayload.Build(ContentJson, StructuredValues, SchemaSnapshot);
        Verify(payloadAgain, sig, rsa).Should().BeTrue();
    }

    [Fact]
    public void V2_TamperStructuredValues_AfterSign_VerifyFails()
    {
        var payload = EmrSignPayload.Build(ContentJson, StructuredValues, SchemaSnapshot);
        var (sig, rsa) = Sign(payload);

        // Sua gia tri form sau khi ky (vd doi HbA1c) -> payload khac -> verify PHAI fail
        var tampered = EmrSignPayload.Build(ContentJson, "{\"hba1c\":5.0,\"bp_sys\":130}", SchemaSnapshot);
        Verify(tampered, sig, rsa).Should().BeFalse();
    }

    [Fact]
    public void V2_TamperSchemaSnapshot_AfterSign_VerifyFails()
    {
        var payload = EmrSignPayload.Build(ContentJson, StructuredValues, SchemaSnapshot);
        var (sig, rsa) = Sign(payload);

        var tampered = EmrSignPayload.Build(ContentJson, StructuredValues,
            "{\"fields\":[{\"key\":\"hba1c\",\"type\":\"text\"}]}");
        Verify(tampered, sig, rsa).Should().BeFalse();
    }

    [Fact]
    public void V1_Record_NullColumns_UsesV1Payload_VerifyOk()
    {
        // Ban ghi cu: ca 2 cot NULL -> payload = content_json don thuan (v1), khong tien to "v2\n"
        var payload = EmrSignPayload.Build(ContentJson, null, null);
        payload.Should().Equal(System.Text.Encoding.UTF8.GetBytes(ContentJson));

        var (sig, rsa) = Sign(payload);
        var payloadAgain = EmrSignPayload.Build(ContentJson, null, null);
        Verify(payloadAgain, sig, rsa).Should().BeTrue();
    }

    [Fact]
    public void V1_And_V2_Payloads_AreDistinct()
    {
        // Ranh gioi v1/v2 ro rang: co structured_values -> v2 (tien to "v2\n"), khac hoan toan v1
        var v1 = EmrSignPayload.Build(ContentJson, null, null);
        var v2 = EmrSignPayload.Build(ContentJson, StructuredValues, null);
        v2.Should().NotEqual(v1);
        System.Text.Encoding.UTF8.GetString(v2).Should().StartWith("v2\n");
    }
}
