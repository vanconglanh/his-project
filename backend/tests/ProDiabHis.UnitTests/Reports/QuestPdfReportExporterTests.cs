using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using Xunit;

namespace ProDiabHis.UnitTests.Reports;

/// <summary>
/// Kiem tra QuestPdfReportExporter sinh ra byte[] hop le cho 3 loai bao cao A4.
/// </summary>
public class QuestPdfReportExporterTests
{
    private static readonly LetterheadDto SampleLetterhead = new(
        ClinicName: "Phong kham Da Khoa dIaB",
        CskcbCode: "79001234",
        CompanyName: "Cong ty TNHH ATDS",
        Address: "123 Nguyen Van Linh, Q.7, TP.HCM",
        Phone: "028-9999-8888",
        Email: "info@diab.vn",
        EmailSupport: "support@diab.vn",
        LogoUrl: null);  // khong fetch HTTP trong unit test

    private static ReportPdfRequest MakeRequest(ReportType type) => new(
        TenantId: 1,
        ReportType: type,
        FromDate: new DateOnly(2026, 5, 1),
        ToDate: new DateOnly(2026, 5, 25),
        ClinicId: null,
        ExportedByUserId: Guid.Parse("11111111-0000-0000-0000-000000000042"),
        ReportCode: $"RPT-{(type == ReportType.Financial ? "FIN" : type == ReportType.Clinical ? "CLN" : "PHA")}-20260526-0001",
        ExportedByFullName: "Nguyễn Thị Lành");

    // ===================== Financial ===================== //

    [Fact]
    public async Task ExportFinancialAsync_Returns_ValidPdfBytes()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var rows = new List<FinancialRowDto>
        {
            new(1, "HD001", "Nguyen Van A", "Kham noi tiet", 250_000m),
            new(2, "HD002", "Tran Thi B",   "Sieu am",       150_000m),
        };

        var bytes = await exporter.ExportFinancialAsync(MakeRequest(ReportType.Financial), SampleLetterhead, rows);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1024, $"PDF Financial qua nho: {bytes.Length} bytes");
        // Kiem tra PDF header %PDF-
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public async Task ExportFinancialAsync_EmptyRows_StillProducesPdf()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var bytes = await exporter.ExportFinancialAsync(
            MakeRequest(ReportType.Financial), SampleLetterhead, Enumerable.Empty<FinancialRowDto>());

        // Handler da kiem soat empty truoc khi goi exporter, nhung exporter tu no van phai sinh PDF
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 512);
    }

    // ===================== Clinical ===================== //

    [Fact]
    public async Task ExportClinicalAsync_Returns_ValidPdfBytes()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var rows = new List<ClinicalRowDto>
        {
            new(1, "Nguyen Van A", "BS. Tran Minh", "E11.9", new DateOnly(2026, 5, 10)),
            new(2, "Le Thi C",    "BS. Nguyen Ha",  "E10.1", new DateOnly(2026, 5, 15)),
        };

        var bytes = await exporter.ExportClinicalAsync(MakeRequest(ReportType.Clinical), SampleLetterhead, rows);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1024, $"PDF Clinical qua nho: {bytes.Length} bytes");
        Assert.Equal(0x25, bytes[0]); // %PDF
    }

    // ===================== Pharmacy ===================== //

    [Fact]
    public async Task ExportPharmacyAsync_Returns_ValidPdfBytes()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var rows = new List<PharmacyRowDto>
        {
            new("MET500", "Metformin 500mg", "L001", new DateOnly(2027, 1, 1), 200m, "vien"),
            new("INS-GLA", "Insulin Glargine 100U", "L002", new DateOnly(2026, 9, 30), 50m, "but"),
        };

        var bytes = await exporter.ExportPharmacyAsync(MakeRequest(ReportType.Pharmacy), SampleLetterhead, rows);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1024, $"PDF Pharmacy qua nho: {bytes.Length} bytes");
        Assert.Equal(0x25, bytes[0]); // %PDF
    }

    // ===================== SSRF Protection ===================== //

    /// <summary>
    /// Dung reflection de goi TryFetchLogoAsync (private) — test SSRF logic
    /// ma khong can HTTP thuc su.
    /// </summary>
    private static async Task<byte[]?> InvokeTryFetchLogoAsync(
        QuestPdfReportExporter exporter, string? logoUrl)
    {
        var method = typeof(QuestPdfReportExporter)
            .GetMethod("TryFetchLogoAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException("Method TryFetchLogoAsync khong tim thay");

        var task = (Task<byte[]?>)method.Invoke(exporter, new object?[] { logoUrl, CancellationToken.None })!;
        return await task;
    }

    /// <summary>
    /// Test scheme / URL parse bi chan truoc khi den DNS check.
    /// Cac URL voi scheme sai bi filter truoc (khong can httpClientFactory).
    /// RFC1918 IP test: do httpClientFactory=null, ham return null som truoc DNS check —
    /// SSRF IP check duoc test rieng qua IsPrivateOrLoopbackAddress_Tests.
    /// </summary>
    [Theory]
    [InlineData("ftp://cdn.example.com/logo.png")]  // scheme sai
    [InlineData("javascript:alert(1)")]             // injection attempt
    [InlineData("file:///etc/passwd")]              // file scheme
    public async Task TryFetchLogoAsync_InvalidScheme_ReturnsNull(string blockedUrl)
    {
        var exporter = new QuestPdfReportExporter(
            httpClientFactory: null,
            logger: NullLogger<QuestPdfReportExporter>.Instance,
            configuration: null);

        var result = await InvokeTryFetchLogoAsync(exporter, blockedUrl);

        Assert.Null(result);
    }

    [Fact]
    public async Task TryFetchLogoAsync_NullUrl_ReturnsNull()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var result = await InvokeTryFetchLogoAsync(exporter, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task TryFetchLogoAsync_EmptyUrl_ReturnsNull()
    {
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var result = await InvokeTryFetchLogoAsync(exporter, "   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task TryFetchLogoAsync_HttpsUrl_WithNoHttpClientFactory_ReturnsNull()
    {
        // Khong co httpClientFactory => null duoc tra ve som (khong can fetch)
        var exporter = new QuestPdfReportExporter(httpClientFactory: null);
        var result = await InvokeTryFetchLogoAsync(exporter, "https://cdn.diab.com.vn/logo.png");
        Assert.Null(result);
    }

    // ===================== IsPrivateOrLoopbackAddress ===================== //

    private static bool InvokeIsPrivateOrLoopback(System.Net.IPAddress address)
    {
        // Report Engine: logic da chuyen sang ReportPdfCommon (dung chung voi GenericReportPdfExporter);
        // QuestPdfReportExporter gio chi goi lai qua ReportPdfCommon.
        var type = System.Reflection.Assembly.GetAssembly(typeof(QuestPdfReportExporter))!
            .GetType("ProDiabHis.Infrastructure.Reports.ReportPdfCommon")
            ?? throw new InvalidOperationException("Type ReportPdfCommon khong tim thay");
        var method = type.GetMethod("IsPrivateOrLoopbackAddress",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException("Method IsPrivateOrLoopbackAddress khong tim thay");
        return (bool)method.Invoke(null, new object[] { address })!;
    }

    [Theory]
    [InlineData("10.0.0.1")]          // RFC1918 10/8
    [InlineData("10.255.255.255")]    // RFC1918 10/8 max
    [InlineData("172.16.0.1")]        // RFC1918 172.16/12
    [InlineData("172.31.255.255")]    // RFC1918 172.16/12 max
    [InlineData("192.168.0.1")]       // RFC1918 192.168/16
    [InlineData("192.168.255.255")]   // RFC1918 192.168/16 max
    [InlineData("169.254.1.1")]       // APIPA
    [InlineData("127.0.0.1")]         // loopback
    [InlineData("::1")]               // IPv6 loopback
    public void IsPrivateOrLoopbackAddress_PrivateIPs_ReturnsTrue(string ipStr)
    {
        var ip = System.Net.IPAddress.Parse(ipStr);
        Assert.True(InvokeIsPrivateOrLoopback(ip), $"IP {ipStr} phai la private/loopback");
    }

    [Theory]
    [InlineData("8.8.8.8")]           // Google DNS — public
    [InlineData("1.1.1.1")]           // Cloudflare — public
    [InlineData("203.113.131.1")]     // VNPT — public
    [InlineData("2001:4860:4860::8888")] // Google IPv6 public
    public void IsPrivateOrLoopbackAddress_PublicIPs_ReturnsFalse(string ipStr)
    {
        var ip = System.Net.IPAddress.Parse(ipStr);
        Assert.False(InvokeIsPrivateOrLoopback(ip), $"IP {ipStr} la public, khong bi chan");
    }
}
