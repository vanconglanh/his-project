using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProDiabHis.Infrastructure.Notifications;
using Xunit;

namespace ProDiabHis.UnitTests.Notifications;

/// <summary>
/// Cong tat tong nhac benh nhan (PatientNotifications:Enabled).
/// Lien quan bug prod: cac job nhac benh nhan that bai "Data truncated for column 'id'"
/// -> can tat o production (false) toi khi go-live, nhung mac dinh BAT o staging/dev.
/// </summary>
public class PatientNotificationSettingsTests
{
    private static IConfiguration Build(string? value) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(value is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { [PatientNotificationSettings.EnabledKey] = value })
            .Build();

    [Fact]
    public void IsEnabled_MacDinh_True_KhiKeyKhongCoTrongConfig()
    {
        // Khong dat key -> giu nguyen hanh vi staging/dev (BAT)
        PatientNotificationSettings.IsEnabled(Build(null)).Should().BeTrue();
    }

    [Theory]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    public void IsEnabled_DocDungGiaTriBool(string value, bool expected)
    {
        PatientNotificationSettings.IsEnabled(Build(value)).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-phai-bool")]
    [InlineData("0")]
    public void IsEnabled_GiaTriKhongParseDuoc_FallbackVe_True(string value)
    {
        // An toan: chi TAT khi cau hinh ro rang "false"; moi gia tri khac -> BAT
        PatientNotificationSettings.IsEnabled(Build(value)).Should().BeTrue();
    }
}
