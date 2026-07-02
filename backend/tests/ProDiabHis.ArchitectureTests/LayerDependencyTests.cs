using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ProDiabHis.ArchitectureTests;

/// <summary>Kiem tra rang buoc phu thuoc giua cac layer</summary>
public class LayerDependencyTests
{
    private const string DomainNamespace = "ProDiabHis.Domain";
    private const string ApplicationNamespace = "ProDiabHis.Application";
    private const string InfrastructureNamespace = "ProDiabHis.Infrastructure";
    private const string ApiNamespace = "ProDiabHis.Api";

    private static Types AllTypes => Types.InAssemblies(
    [
        typeof(ProDiabHis.Domain.Common.BaseEntity).Assembly,
        typeof(ProDiabHis.Application.DependencyInjection).Assembly,
        typeof(ProDiabHis.Infrastructure.DependencyInjection).Assembly,
        typeof(ProDiabHis.Api.Controllers.AuthController).Assembly,
    ]);

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer khong duoc phu thuoc vao Application");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer khong duoc phu thuoc vao Infrastructure");
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer khong duoc phu thuoc vao Api");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer khong duoc phu thuoc vao Infrastructure");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer khong duoc phu thuoc vao Api");
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(ProDiabHis.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure layer khong duoc phu thuoc vao Api");
    }
}
