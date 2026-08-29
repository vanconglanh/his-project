using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace ProDiabHis.UnitTests.Sprint20260830;

/// <summary>
/// UTC-C-xx — Muc C (no ky thuat schema), RUI RO CAO NHAT phien 2026-08-29/30.
/// Migration 9171 da DROP 2 bang chet diab_his_lab_orders / diab_his_rad_orders sau khi
/// rewrite ~30 tham chieu sang bang song diab_his_cli_lab_orders / diab_his_cli_rad_orders.
///
/// Test nay la CHOT CHONG TAI PHAT: quet toan bo source backend/frontend, fail ngay neu co bat ky
/// cau SQL / ToTable / chuoi nao con tro toi bang da bi DROP. Neu lot luoi -> runtime se nem
/// "Table doesn't exist" ngay giua luong CLS (chi dinh -> nhap ket qua -> in phieu -> bao cao).
/// Comment giai thich lich su (// ...) duoc phep, chuoi trong code thi khong.
/// </summary>
public class DroppedLegacyTablesGuardTests
{
    private static readonly string[] DroppedTables = { "diab_his_lab_orders", "diab_his_rad_orders" };
    private static readonly string[] LiveTables = { "diab_his_cli_lab_orders", "diab_his_cli_rad_orders" };

    /// <summary>Tim thu muc goc repo bang cach di nguoc len tu thu muc chay test cho toi khi thay CLAUDE.md.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        dir.Should().NotBeNull("phai tim duoc thu muc goc repo (co CLAUDE.md) de quet source");
        return dir!.FullName;
    }

    private static IEnumerable<string> SourceFiles(string relativeDir, string pattern)
    {
        var root = Path.Combine(RepoRoot(), relativeDir);
        if (!Directory.Exists(root)) yield break;
        foreach (var f in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            var p = f.Replace('\\', '/');
            if (p.Contains("/bin/") || p.Contains("/obj/") || p.Contains("/node_modules/") ||
                p.Contains("/.next/") || p.Contains("/tests/")) continue;
            yield return f;
        }
    }

    /// <summary>Bo dong comment // va dong trong khoi /* */ don gian, giu lai code that.</summary>
    private static string StripComments(string content)
    {
        var noBlock = Regex.Replace(content, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        var lines = noBlock.Split('\n')
            .Select(l => Regex.Replace(l, @"//.*$", string.Empty))
            .Select(l => Regex.Replace(l, @"^\s*(///|\*)\s.*$", string.Empty));
        return string.Join('\n', lines);
    }

    // UTC-C-01 — khong con tham chieu bang da DROP trong code backend (.cs)
    [Fact]
    public void Backend_KhongConThamChieuBangDaDrop()
    {
        var viPham = new List<string>();

        foreach (var file in SourceFiles("backend/src", "*.cs"))
        {
            var code = StripComments(File.ReadAllText(file));
            foreach (var table in DroppedTables)
            {
                // Chi bat khi KHONG phai la hau to cua ten bang song (cli_lab_orders chua "lab_orders"
                // nhung khong chua "diab_his_lab_orders")
                if (code.Contains(table, StringComparison.Ordinal))
                    viPham.Add($"{Path.GetFileName(file)} -> {table}");
            }
        }

        viPham.Should().BeEmpty(
            "bang lab/rad orders cu da bi DROP o migration 9171, moi truy van phai dung bang cli_*");
    }

    // UTC-C-02 — khong con tham chieu bang da DROP trong frontend (.ts/.tsx)
    [Fact]
    public void Frontend_KhongConThamChieuBangDaDrop()
    {
        var viPham = new List<string>();

        foreach (var pattern in new[] { "*.ts", "*.tsx" })
        foreach (var file in SourceFiles("frontend", pattern))
        {
            var code = StripComments(File.ReadAllText(file));
            foreach (var table in DroppedTables)
                if (code.Contains(table, StringComparison.Ordinal))
                    viPham.Add($"{Path.GetFileName(file)} -> {table}");
        }

        viPham.Should().BeEmpty();
    }

    // UTC-C-03 — EF Core phai map entity LabOrder/RadOrder sang dung bang cli_*
    [Fact]
    public void EfCore_PhaiMapSangBangConSong()
    {
        var config = Path.Combine(RepoRoot(),
            "backend/src/ProDiabHis.Infrastructure/Persistence/Configurations/LabRadConfiguration.cs");
        File.Exists(config).Should().BeTrue();

        var code = StripComments(File.ReadAllText(config));

        code.Should().Contain("ToTable(\"diab_his_cli_lab_orders\")");
        code.Should().Contain("ToTable(\"diab_his_cli_rad_orders\")");
    }

    // UTC-C-04 — cong thanh toan CLS phai doc dung bang con song (chan chi dinh khi chua thu tien)
    [Fact]
    public void CongThanhToanCls_PhaiDocBangConSong()
    {
        var gate = Path.Combine(RepoRoot(), "backend/src/ProDiabHis.Infrastructure/CLS/ClsPaymentGateImpl.cs");
        File.Exists(gate).Should().BeTrue();

        var code = StripComments(File.ReadAllText(gate));

        code.Should().Contain("diab_his_cli_lab_orders");
        code.Should().Contain("diab_his_cli_rad_orders");
    }

    // UTC-C-05 — migration 9171 phai ton tai va la ban DROP co chu dich (khong phai xoa nham)
    [Fact]
    public void Migration9171_PhaiTonTaiVaDropDungHaiBang()
    {
        var mig = Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "db/migrations"), "9171_*.sql")
            .FirstOrDefault();

        mig.Should().NotBeNull("migration DROP bang chet phai duoc luu lai de tai lap moi truong");
        var sql = File.ReadAllText(mig!);

        foreach (var t in DroppedTables)
            sql.Should().Contain($"DROP TABLE IF EXISTS {t}");
        foreach (var t in LiveTables)
            sql.Should().NotContain($"DROP TABLE IF EXISTS {t}",
                "TUYET DOI khong duoc DROP nham bang dang chua du lieu CLS that");
    }
}
