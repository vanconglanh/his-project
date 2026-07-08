using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ProDiabHis.Infrastructure.Notifications;
using Xunit;

namespace ProDiabHis.UnitTests.Notifications;

/// <summary>
/// Verify ma hoa Web Push theo TEST VECTOR CHINH THUC RFC 8291 muc 5.
/// https://www.rfc-editor.org/rfc/rfc8291#section-5
/// </summary>
public class WebPushCryptoTests
{
    [Fact]
    public void EncryptPayload_MatchesRfc8291Vector()
    {
        // Gia tri base64url lay nguyen van tu RFC 8291 muc 5
        var plaintext   = WebPushCrypto.B64UrlDecode("V2hlbiBJIGdyb3cgdXAsIEkgd2FudCB0byBiZSBhIHdhdGVybWVsb24");
        var authSecret  = WebPushCrypto.B64UrlDecode("BTBZMqHH6r4Tts7J_aSIgg");
        var uaPublic    = WebPushCrypto.B64UrlDecode("BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4");
        var asPrivate   = WebPushCrypto.B64UrlDecode("yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw");
        var asPublic    = WebPushCrypto.B64UrlDecode("BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8");
        var salt        = WebPushCrypto.B64UrlDecode("DGv6ra1nlYgDCS1FRnbzlw");
        var expectedBody = WebPushCrypto.B64UrlDecode(
            "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLoc" +
            "InmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPTpK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN");

        using var asKey = WebPushCrypto.ImportEcdhKey(asPrivate, asPublic);
        var body = WebPushCrypto.EncryptPayloadWith(uaPublic, authSecret, plaintext, asKey, salt);

        body.Should().Equal(expectedBody);
    }

    [Fact]
    public void VapidHeader_JwtVerifiesWithPublicKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var q = ecdsa.ExportParameters(false).Q;
        var publicRaw = new byte[65];
        publicRaw[0] = 0x04;
        Buffer.BlockCopy(q.X!, 0, publicRaw, 1, 32);
        Buffer.BlockCopy(q.Y!, 0, publicRaw, 33, 32);

        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var header = WebPushCrypto.BuildVapidAuthHeader(
            "https://fcm.googleapis.com/fcm/send/abc123", "mailto:test@prodiab.vn", publicRaw, ecdsa, now);

        header.Should().StartWith("vapid t=");
        // Tach JWT, verify chu ky ES256 bang chinh public key
        var token = header.Substring("vapid t=".Length).Split(',')[0];
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        var signingInput = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
        var sig = WebPushCrypto.B64UrlDecode(parts[2]);
        ecdsa.VerifyData(signingInput, sig, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation).Should().BeTrue();

        // aud = origin cua endpoint
        var payloadJson = Encoding.UTF8.GetString(WebPushCrypto.B64UrlDecode(parts[1]));
        payloadJson.Should().Contain("https://fcm.googleapis.com");
    }
}
