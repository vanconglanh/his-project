using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ProDiabHis.Api.Filters;
using Xunit;

namespace ProDiabHis.UnitTests.Permissions;

public class PermissionCheckFilterTests
{
    [Fact]
    public void OnAuthorization_WhenUserHasPermission_DoesNotSetResult()
    {
        // Arrange
        var filter = new RequirePermissionAttribute("user.invite");
        var ctx = CreateAuthFilterContext(new[] { "user.invite", "user.read" }, isSuperAdmin: false);

        // Act
        filter.OnAuthorization(ctx);

        // Assert
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public void OnAuthorization_WhenUserMissingPermission_Returns403()
    {
        // Arrange
        var filter = new RequirePermissionAttribute("user.invite");
        var ctx = CreateAuthFilterContext(new[] { "user.read" }, isSuperAdmin: false);

        // Act
        filter.OnAuthorization(ctx);

        // Assert
        ctx.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)ctx.Result!).StatusCode.Should().Be(403);
    }

    [Fact]
    public void OnAuthorization_WhenSuperAdmin_BypassesPermissionCheck()
    {
        // Arrange
        var filter = new RequirePermissionAttribute("user.invite");
        var ctx = CreateAuthFilterContext(Array.Empty<string>(), isSuperAdmin: true);

        // Act
        filter.OnAuthorization(ctx);

        // Assert
        ctx.Result.Should().BeNull(); // Super admin bypass
    }

    [Fact]
    public void OnAuthorization_WhenNotAuthenticated_Returns401()
    {
        // Arrange
        var filter = new RequirePermissionAttribute("user.invite");
        var ctx = CreateUnauthenticatedContext();

        // Act
        filter.OnAuthorization(ctx);

        // Assert
        ctx.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    private static AuthorizationFilterContext CreateAuthFilterContext(string[] permissions, bool isSuperAdmin)
    {
        var claims = permissions
            .Select(p => new Claim("permissions", p))
            .ToList();

        if (isSuperAdmin)
            claims.Add(new Claim("is_super_admin", "true"));

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpCtx = new DefaultHttpContext { User = principal };
        var actionCtx = new ActionContext(httpCtx, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionCtx, new List<IFilterMetadata>());
    }

    private static AuthorizationFilterContext CreateUnauthenticatedContext()
    {
        var httpCtx = new DefaultHttpContext();
        var actionCtx = new ActionContext(httpCtx, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionCtx, new List<IFilterMetadata>());
    }
}
