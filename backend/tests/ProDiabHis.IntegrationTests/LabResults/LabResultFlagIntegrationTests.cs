using Dapper;
using FluentAssertions;
using MySqlConnector;
using ProDiabHis.Application.LabResults;
using Xunit;

namespace ProDiabHis.IntegrationTests.LabResults;

/// <summary>
/// Bug A (fix 2026-08-31) — TRUOC day CreateLabResultCommandHandler luon truyen (null, null) vao
/// FlagCalculator nen MOI ket qua xet nghiem deu ra flag NORMAL, ke ca gia tri nguy hiem.
/// Fix: join bang <c>diab_his_dict_lab_tests</c> theo <c>code</c> de lay
/// reference_range_low/high roi moi tinh flag.
///
/// Test nay dung MySQL THAT (Testcontainers): dung lai dung cau SQL tra cuu khoang tham chieu
/// ma handler dang dung, roi kiem tra flag tinh ra dung. Neu ai do lam hong lai duong tra cuu
/// (doi ten bang/cot, mat seed) thi test do ngay.
/// </summary>
[Collection("MySql")]
public class LabResultFlagIntegrationTests : IClassFixture<MySqlTestFixture>
{
    private readonly MySqlTestFixture _fixture;
    private readonly ILabResultFlagCalculator _flagCalc = new LabResultFlagCalculator();

    public LabResultFlagIntegrationTests(MySqlTestFixture fixture) => _fixture = fixture;

    // Dung dung cau lenh cua LabRefRangeLookup trong LabResultHandlers.cs
    private const string LookupSql =
        "SELECT reference_range_low AS Low, reference_range_high AS High " +
        "FROM diab_his_dict_lab_tests WHERE code = @Code LIMIT 1";

    private const string CreateDictSql = @"
        CREATE TABLE IF NOT EXISTS diab_his_dict_lab_tests (
            code                  VARCHAR(50)  NOT NULL PRIMARY KEY,
            name                  VARCHAR(255) NOT NULL,
            unit                  VARCHAR(50)      NULL,
            reference_range_low   DECIMAL(18,4)    NULL,
            reference_range_high  DECIMAL(18,4)    NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

    private async Task<MySqlConnection> OpenSeededAsync()
    {
        var conn = new MySqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(CreateDictSql);
        // Seed giong migration thuc te (50f4f50 - seed reference range XN thuong quy)
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_dict_lab_tests (code, name, unit, reference_range_low, reference_range_high)
              VALUES (@code, @name, @unit, @low, @high)
              ON DUPLICATE KEY UPDATE reference_range_low = VALUES(reference_range_low),
                                      reference_range_high = VALUES(reference_range_high)",
            new[]
            {
                new { code = "HBA1C", name = "HbA1c (Glycated Hemoglobin)", unit = (string?)"%",      low = (decimal?)4.0m, high = (decimal?)5.6m },
                new { code = "GLU_F", name = "Duong huyet doi",             unit = (string?)"mmol/L", low = (decimal?)3.9m, high = (decimal?)5.5m },
                new { code = "CBC",   name = "Cong thuc mau toan phan",     unit = (string?)null,     low = (decimal?)null, high = (decimal?)null },
            });
        return conn;
    }

    private async Task<(decimal? Low, decimal? High)> LookupAsync(MySqlConnection conn, string code)
    {
        var row = await conn.QueryFirstOrDefaultAsync<(decimal? Low, decimal? High)>(LookupSql, new { Code = code });
        return row;
    }

    /// <summary>UTC-CLS-07: HbA1c 8.1 (khoang 4.0-5.6) — lech &gt;=50% do rong khoang -&gt; CRITICAL, TUYET DOI khong NORMAL.</summary>
    [DockerAvailableFact]
    public async Task HbA1c_RatCao_PhaiRaCritical_KhongDuocLaNormal()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "HBA1C");
        low.Should().Be(4.0m, "seed khoang tham chieu HbA1c phai co trong bang dict");
        high.Should().Be(5.6m);

        var flag = _flagCalc.Calculate(8.1m, low, high);

        flag.Should().NotBe("NORMAL", "Bug A: gia tri 8.1 vuot xa nguong 5.6 khong the la binh thuong");
        flag.Should().Be("CRITICAL");
    }

    /// <summary>UTC-CLS-09: gia tri chi vuot nhe nguong tren -&gt; H.</summary>
    [DockerAvailableFact]
    public async Task DuongHuyetDoi_VuotNhe_PhaiRaH()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "GLU_F");
        var flag = _flagCalc.Calculate(5.9m, low, high);

        flag.Should().Be("H");
    }

    /// <summary>UTC-CLS-10: gia tri trong khoang -&gt; NORMAL (khong bao dong gia).</summary>
    [DockerAvailableFact]
    public async Task DuongHuyetDoi_TrongKhoang_PhaiRaNormal()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "GLU_F");
        var flag = _flagCalc.Calculate(5.0m, low, high);

        flag.Should().Be("NORMAL");
    }

    /// <summary>UTC-CLS-11: thap hon nguong duoi -&gt; L/LL, khong duoc NORMAL.</summary>
    [DockerAvailableFact]
    public async Task DuongHuyetDoi_ThapHonNguongDuoi_PhaiCanhBaoThap()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "GLU_F");
        var flag = _flagCalc.Calculate(2.0m, low, high);

        flag.Should().NotBe("NORMAL");
        flag.Should().BeOneOf("L", "LL", "CRITICAL");
    }

    /// <summary>
    /// UTC-CLS-12: XN khong co khoang tham chieu trong dict (vd CBC) -&gt; lookup tra (null,null)
    /// -&gt; NORMAL. Day la hanh vi DUNG (khong the ket luan gi khi thieu khoang), khac han
    /// Bug A la MOI XN deu roi vao truong hop nay.
    /// </summary>
    [DockerAvailableFact]
    public async Task XnKhongCoKhoangThamChieu_TraVeNormal_VaKhongNem()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "CBC");
        low.Should().BeNull();
        high.Should().BeNull();

        var flag = _flagCalc.Calculate(5.6m, low, high);
        flag.Should().Be("NORMAL");
    }

    /// <summary>UTC-CLS-13: ma XN khong ton tai trong dict -&gt; khong nem, tra ve khong co khoang.</summary>
    [DockerAvailableFact]
    public async Task MaXnKhongTonTai_KhongNem()
    {
        await using var conn = await OpenSeededAsync();

        var (low, high) = await LookupAsync(conn, "KHONG_TON_TAI_XYZ");

        low.Should().BeNull();
        high.Should().BeNull();
        _flagCalc.Calculate(123m, low, high).Should().Be("NORMAL");
    }
}
