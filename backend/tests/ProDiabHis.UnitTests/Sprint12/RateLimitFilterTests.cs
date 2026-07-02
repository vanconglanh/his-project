using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Api.Filters;
using ProDiabHis.Application.PublicApi;
using System.Security.Claims;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint12;

public class RateLimitFilterTests
{
    private static AuthorizationFilterContext BuildContext(bool authenticated, ClaimsPrincipal? principal = null)
    {
        var httpCtx = new DefaultHttpContext();
        if (principal != null)
            httpCtx.User = principal;
        else if (authenticated)
            httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", "user-1"), new Claim("tenant_id", "1") }, "Bearer"));

        var actionContext = new ActionContext(
            httpCtx,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task Filter_ShouldAllow_WhenUnderLimit()
    {
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter.AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(true));

        var filter = new GlobalRateLimitFilter(rateLimiter, NullLogger<GlobalRateLimitFilter>.Instance);
        var ctx = BuildContext(true);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task Filter_ShouldBlock_WhenUserLimitExceeded()
    {
        var rateLimiter = Substitute.For<IRateLimiter>();
        // Per-user check returns false (limit exceeded), tenant check not reached
        rateLimiter.AllowAsync(Arg.Is<string>(k => k.StartsWith("user:")), Arg.Any<int>(), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(false));
        rateLimiter.AllowAsync(Arg.Is<string>(k => k.StartsWith("tenant:")), Arg.Any<int>(), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(true));

        var filter = new GlobalRateLimitFilter(rateLimiter, NullLogger<GlobalRateLimitFilter>.Instance);
        var ctx = BuildContext(true);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.Should().NotBeNull();
        var objectResult = ctx.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task Filter_ShouldBlock_WhenTenantLimitExceeded()
    {
        var rateLimiter = Substitute.For<IRateLimiter>();
        rateLimiter.AllowAsync(Arg.Is<string>(k => k.StartsWith("user:")), Arg.Any<int>(), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(true));
        rateLimiter.AllowAsync(Arg.Is<string>(k => k.StartsWith("tenant:")), Arg.Any<int>(), Arg.Any<TimeSpan>())
            .Returns(Task.FromResult(false));

        var filter = new GlobalRateLimitFilter(rateLimiter, NullLogger<GlobalRateLimitFilter>.Instance);
        var ctx = BuildContext(true);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.Should().NotBeNull();
        var objectResult = ctx.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task Filter_ShouldSkip_WhenNotAuthenticated()
    {
        var rateLimiter = Substitute.For<IRateLimiter>();
        var filter = new GlobalRateLimitFilter(rateLimiter, NullLogger<GlobalRateLimitFilter>.Instance);

        var anon = new ClaimsPrincipal(new ClaimsIdentity()); // not authenticated
        var ctx = BuildContext(false, anon);

        await filter.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
        await rateLimiter.DidNotReceive().AllowAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<TimeSpan>());
    }
}
