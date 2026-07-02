using FluentAssertions;
using Microsoft.AspNetCore.Http;
using ProDiabHis.Api.Middlewares;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint12;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldAddAllRequiredHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var headers = context.Response.Headers;
        headers["Strict-Transport-Security"].ToString().Should().Contain("max-age=31536000");
        headers["Strict-Transport-Security"].ToString().Should().Contain("includeSubDomains");
        headers["X-Frame-Options"].ToString().Should().Be("DENY");
        headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        headers["Content-Security-Policy"].ToString().Should().Contain("default-src 'self'");
        headers["Content-Security-Policy"].ToString().Should().Contain("frame-ancestors 'none'");
        headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        headers["Permissions-Policy"].ToString().Should().Contain("camera=()");
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }
}
