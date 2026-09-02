using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProDiabHis.Application.Auth;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-H10-xx — H-10 (FR-1011) doc cau hinh Security:MandatoryMfaRoles.
/// Bo sung cho LoginCommandHandlerTests (da cover flag mfaSetupRequired): o day kiem RIENG
/// phan parse cau hinh — ho tro ca dang mang JSON lan chuoi CSV, va default an toan la ["admin"].
/// Sai o day = khoa nham hoac bo sot role bat buoc 2FA -> anh huong dang nhap toan he thong.
/// </summary>
public class MandatoryMfaConfigTests
{
    private static async Task<IReadOnlyList<string>> ReadRoles(IConfiguration config)
    {
        var handler = new LoginCommandHandler(
            Substitute.For<IApplicationDbContext>(),
            Substitute.For<IJwtService>(),
            Substitute.For<IPasswordHasher>(),
            Substitute.For<ILogger<LoginCommandHandler>>(),
            config,
            // FakeEmptyDapperConnectionFactory tra ve khong row nao -> ham fallback dung config
            // (dung y dinh goc cua test nay: chi kiem tra nhanh doc config, khong dung setting DB).
            new FakeEmptyDapperConnectionFactory());

        var method = typeof(LoginCommandHandler).GetMethod(
            "GetMandatoryMfaRoleCodesAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<IReadOnlyList<string>>)method.Invoke(handler, new object[] { 1, CancellationToken.None })!;
        return await task;
    }

    private static IConfiguration Cfg(params (string key, string value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.key, p.value)))
            .Build();

    // UTC-H10-01 — khong cau hinh gi -> mac dinh chi "admin" bat buoc 2FA
    [Fact]
    public async Task KhongCauHinh_MacDinhLaAdmin()
    {
        var roles = await ReadRoles(new ConfigurationBuilder().Build());

        roles.Should().ContainSingle().Which.Should().Be("admin");
    }

    // UTC-H10-02 — dang mang JSON (appsettings.json thuc te)
    [Fact]
    public async Task CauHinhDangMang_DocDuTatCaRole()
    {
        var roles = await ReadRoles(Cfg(
            ("Security:MandatoryMfaRoles:0", "admin"),
            ("Security:MandatoryMfaRoles:1", "ke_toan")));

        roles.Should().BeEquivalentTo(new[] { "admin", "ke_toan" });
    }

    // UTC-H10-03 — dang chuoi CSV (bien moi truong docker-compose)
    [Fact]
    public async Task CauHinhDangChuoiCsv_TachDungVaTrimKhoangTrang()
    {
        var roles = await ReadRoles(Cfg(("Security:MandatoryMfaRoles", "admin, ke_toan ,duoc_si")));

        roles.Should().BeEquivalentTo(new[] { "admin", "ke_toan", "duoc_si" });
    }

    // UTC-H10-04 — BIEN: chuoi rong -> fallback ve mac dinh, KHONG tra danh sach rong
    // (danh sach rong = khong role nao bat buoc 2FA = ha thap bao mat am tham)
    [Fact]
    public async Task CauHinhChuoiRong_FallbackVeMacDinhAdmin()
    {
        var roles = await ReadRoles(Cfg(("Security:MandatoryMfaRoles", "   ")));

        roles.Should().ContainSingle().Which.Should().Be("admin");
    }

    // UTC-H10-05 — CSV co dau phay thua -> khong sinh phan tu rong
    [Fact]
    public async Task CauHinhCsvCoDauPhayThua_KhongSinhPhanTuRong()
    {
        var roles = await ReadRoles(Cfg(("Security:MandatoryMfaRoles", "admin,,ke_toan,")));

        roles.Should().BeEquivalentTo(new[] { "admin", "ke_toan" });
        roles.Should().NotContain(string.Empty);
    }
}
