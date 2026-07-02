using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Infrastructure.Notifications;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint10;

public class WebPushSenderTests
{
    [Fact]
    public void WebPushPayload_ShouldHoldAllFields()
    {
        var payload = new WebPushPayload("Test Title", "Test Body", "/icon.png", new { key = "value" });

        payload.Title.Should().Be("Test Title");
        payload.Body.Should().Be("Test Body");
        payload.Icon.Should().Be("/icon.png");
        payload.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task WebPushSender_SendAsync_ShouldNotThrow_WhenVapidMissing()
    {
        // Arrange: VapidKeyService returns a stub keypair
        var vapidService = Substitute.For<IVapidKeyService>();
        vapidService.GetOrCreateKeyPairAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VapidKeyPair("pub_key_stub", "priv_key_stub"));

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        var dbFactory = Substitute.For<ProDiabHis.Application.Common.IDapperConnectionFactory>();
        var logger = NullLogger<WebPushSenderImpl>.Instance;

        var sender = new WebPushSenderImpl(vapidService, httpFactory, dbFactory, logger);

        // Act + Assert: should not throw even with stub connection
        var act = async () => await sender.SendAsync(
            "https://push.example.com/subscription123",
            "p256dh_key_base64",
            "auth_key_base64",
            tenantId: 1,
            new WebPushPayload("Hello", "World"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void VapidKeyPair_ShouldHoldPublicAndPrivate()
    {
        var pair = new VapidKeyPair("public_key_base64url", "private_key_base64");
        pair.PublicKey.Should().Be("public_key_base64url");
        pair.PrivateKey.Should().Be("private_key_base64");
    }
}
