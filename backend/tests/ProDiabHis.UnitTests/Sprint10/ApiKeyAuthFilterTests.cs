using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.PublicApi;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint10;

public class ApiKeyAuthFilterTests
{
    private static ApiPartnerContext CreatePartner(
        string status = "ACTIVE",
        DateTime? expiresAt = null,
        List<string>? scopes = null,
        List<string>? ipWhitelist = null) =>
        new(
            Guid.NewGuid(),
            TenantId: 1,
            Name: "Test Partner",
            Scopes: (scopes ?? new() { "public.appointment.write" }).AsReadOnly(),
            RateLimitPerMin: 60,
            DailyQuota: 10000,
            Status: status,
            ExpiresAt: expiresAt,
            IpWhitelist: (ipWhitelist ?? new()).AsReadOnly()
        );

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static AuthorizationFilterContext BuildContext(
        string? apiKey = null,
        string? scope = null,
        string clientIp = "127.0.0.1")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(clientIp);

        if (apiKey != null)
            httpContext.Request.Headers["X-Api-Key"] = apiKey;

        var metadata = new List<object>();
        if (scope != null)
            metadata.Add(new ApiKeyAuthAttribute { Scope = scope });

        var actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = metadata
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task Valid_ApiKey_WithCorrectScope_ShouldPass()
    {
        // Arrange
        var rawKey = "pdh_live_testkey123";
        var hash = HashKey(rawKey);
        var partner = CreatePartner(scopes: new() { "public.appointment.write" });

        var keyStore = Substitute.For<IApiKeyStore>();
        keyStore.FindByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(partner);

        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter.AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(true);
        rateLimiter.GetCountAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0L);

        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext(rawKey, "public.appointment.write");

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull(); // null = pass
        context.HttpContext.Items["ApiPartner"].Should().NotBeNull();
        context.HttpContext.Items["TenantId"].Should().Be(1);
    }

    [Fact]
    public async Task Missing_ApiKey_ShouldReturn401()
    {
        var keyStore = Substitute.For<IApiKeyStore>();
        var rateLimiter = Substitute.For<IRateLimiter>();
        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext(apiKey: null);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().NotBeNull();
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Invalid_ApiKey_ShouldReturn401()
    {
        var keyStore = Substitute.For<IApiKeyStore>();
        keyStore.FindByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var rateLimiter = Substitute.For<IRateLimiter>();
        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext("invalid_key");

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().NotBeNull();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Expired_ApiKey_ShouldReturn401()
    {
        var rawKey = "pdh_live_expiredkey";
        var hash = HashKey(rawKey);
        var partner = CreatePartner(expiresAt: DateTime.UtcNow.AddDays(-1));

        var keyStore = Substitute.For<IApiKeyStore>();
        keyStore.FindByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(partner);

        var rateLimiter = Substitute.For<IRateLimiter>();
        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext(rawKey);

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().NotBeNull();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Wrong_Scope_ShouldReturn403()
    {
        var rawKey = "pdh_live_scopetest";
        var hash = HashKey(rawKey);
        var partner = CreatePartner(scopes: new() { "public.catalog.read" });

        var keyStore = Substitute.For<IApiKeyStore>();
        keyStore.FindByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(partner);
        keyStore.LogRequestAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter.AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(true);
        rateLimiter.GetCountAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(0L);

        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext(rawKey, scope: "public.appointment.write");

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().NotBeNull();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task RateLimited_ShouldReturn429()
    {
        var rawKey = "pdh_live_ratelimited";
        var hash = HashKey(rawKey);
        var partner = CreatePartner();

        var keyStore = Substitute.For<IApiKeyStore>();
        keyStore.FindByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(partner);
        keyStore.LogRequestAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter.AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(false); // blocked
        rateLimiter.GetCountAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(0L);

        var filter = new ApiKeyAuthFilter(keyStore, rateLimiter);
        var context = BuildContext(rawKey, scope: "public.appointment.write");

        await filter.OnAuthorizationAsync(context);

        context.Result.Should().NotBeNull();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }
}
