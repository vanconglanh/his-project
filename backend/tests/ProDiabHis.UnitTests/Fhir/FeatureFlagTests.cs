using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.FeatureFlags;
using Xunit;

namespace ProDiabHis.UnitTests.Fhir;

public class FeatureFlagTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values != null)
            builder.AddInMemoryCollection(values);
        return builder.Build();
    }

    [Fact]
    public async Task IsEnabledAsync_ConfigOverride_True_ReturnsTrue()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["FeatureFlags:fhir.export.bundle"] = "true"
        });
        var db = Substitute.For<IDapperConnectionFactory>();
        var svc = new FeatureFlagService(db, config, NullLogger<FeatureFlagService>.Instance);

        var result = await svc.IsEnabledAsync("fhir.export.bundle");

        result.Should().BeTrue();
        // DB should not be called because config override
        db.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task IsEnabledAsync_ConfigOverride_False_ReturnsFalse()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["FeatureFlags:ai.diagnosis.suggest"] = "false"
        });
        var db = Substitute.For<IDapperConnectionFactory>();
        var svc = new FeatureFlagService(db, config, NullLogger<FeatureFlagService>.Instance);

        var result = await svc.IsEnabledAsync("ai.diagnosis.suggest");

        result.Should().BeFalse();
        db.DidNotReceive().CreateConnection();
    }

    [Fact]
    public async Task IsEnabledAsync_DbException_ReturnsFalse()
    {
        var config = BuildConfig(); // no override
        var db = Substitute.For<IDapperConnectionFactory>();
        db.CreateConnection().Returns(_ => throw new Exception("DB down"));

        var svc = new FeatureFlagService(db, config, NullLogger<FeatureFlagService>.Instance);

        // Should not throw — graceful fallback
        var result = await svc.IsEnabledAsync("some.flag");
        result.Should().BeFalse();
    }

    [Fact]
    public void IFeatureFlagService_InterfaceHasCorrectMembers()
    {
        // Verify interface contract
        var type = typeof(IFeatureFlagService);

        type.GetMethod("IsEnabledAsync").Should().NotBeNull();
        type.GetMethod("GetAllAsync").Should().NotBeNull();
        type.GetMethod("SetEnabledAsync").Should().NotBeNull();
    }

    [Fact]
    public void FeatureFlagDto_RecordEquality()
    {
        var now = DateTime.UtcNow;
        var dto1 = new FeatureFlagDto("fhir.read", true, "desc", now);
        var dto2 = new FeatureFlagDto("fhir.read", true, "desc", now);

        dto1.Should().Be(dto2);
        dto1.Key.Should().Be("fhir.read");
        dto1.Enabled.Should().BeTrue();
    }
}
