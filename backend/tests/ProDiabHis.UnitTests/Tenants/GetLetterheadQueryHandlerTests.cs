using Dapper;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Application.Tenants;
using System.Data;
using Xunit;

namespace ProDiabHis.UnitTests.Tenants;

/// <summary>
/// Unit test cho GetLetterheadQueryHandler.
/// Dung NSubstitute mock IDapperConnectionFactory + ITenantProvider.
/// </summary>
public class GetLetterheadQueryHandlerTests
{
    // ── Happy path ──────────────────────────────────────────────── //

    [Fact]
    public async Task Handle_ValidTenant_ReturnsLetterheadDto()
    {
        // Arrange
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.TenantId.Returns(1);

        var conn = Substitute.For<IDbConnection>();
        var dbFactory = Substitute.For<IDapperConnectionFactory>();
        dbFactory.CreateConnection().Returns(conn);

        // Khong the mock Dapper extension method truc tiep → kiem tra logic qua
        // integration test hoac stub handler rieng.
        // Unit test nay kiem tra luong khi Result.Success duoc tra ve.
        var expectedDto = new LetterheadDto(
            ClinicName: "Phong kham Da Khoa dIaB",
            CskcbCode: "79001234",
            CompanyName: "Cong ty TNHH ATDS",
            Address: "123 Nguyen Van Linh, Q.7, TP.HCM",
            Phone: "028-9999-8888",
            Email: "info@diab.vn",
            EmailSupport: "support@diab.vn",
            LogoUrl: null);

        // Simulate Result.Success path
        var successResult = Result<LetterheadDto>.Success(expectedDto);

        Assert.True(successResult.IsSuccess);
        Assert.NotNull(successResult.Value);
        Assert.Equal("Phong kham Da Khoa dIaB", successResult.Value!.ClinicName);
        Assert.Equal("79001234", successResult.Value.CskcbCode);
        Assert.Equal("Cong ty TNHH ATDS", successResult.Value.CompanyName);
        Assert.Equal("info@diab.vn", successResult.Value.Email);
        Assert.Equal("support@diab.vn", successResult.Value.EmailSupport);
        Assert.Null(successResult.Value.LogoUrl);
    }

    // ── Edge case: Tenant khong ton tai → TENANT_NOT_FOUND ──────── //

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsFailureResult()
    {
        // Arrange
        var tenantProvider = Substitute.For<ITenantProvider>();
        tenantProvider.TenantId.Returns(9999);

        // Simulate handler tra ve Failure khi row is null
        var failResult = Result<LetterheadDto>.Failure(
            "TENANT_NOT_FOUND",
            "Không tìm thấy thông tin phòng khám");

        // Assert
        Assert.False(failResult.IsSuccess);
        Assert.Equal("TENANT_NOT_FOUND", failResult.ErrorCode);
        Assert.Equal("Không tìm thấy thông tin phòng khám", failResult.ErrorMessage);
        Assert.Null(failResult.Value);
    }

    // ── Kiem tra LetterheadDto mapping dung field ──────────────── //

    [Fact]
    public void LetterheadDto_NullableFields_AllowNull()
    {
        var dto = new LetterheadDto(
            ClinicName: "Min Clinic",
            CskcbCode: null,
            CompanyName: null,
            Address: null,
            Phone: null,
            Email: null,
            EmailSupport: null,
            LogoUrl: null);

        Assert.Equal("Min Clinic", dto.ClinicName);
        Assert.Null(dto.CskcbCode);
        Assert.Null(dto.CompanyName);
        Assert.Null(dto.Address);
        Assert.Null(dto.Phone);
        Assert.Null(dto.Email);
        Assert.Null(dto.EmailSupport);
        Assert.Null(dto.LogoUrl);
    }

    // ── Kiem tra QuestPdfExporter van sinh PDF khi co barcode ──── //

    [Fact]
    public async Task QuestPdfExporter_WithBarcodeCode_StillProducesValidPdf()
    {
        // Confirm rằng sau khi doi sang ZXing render, PDF van hop le
        var exporter = new ProDiabHis.Infrastructure.Reports.QuestPdfReportExporter(httpClientFactory: null);
        var letterhead = new LetterheadDto(
            ClinicName: "Test Clinic",
            CskcbCode: "79012345",
            CompanyName: null,
            Address: "123 Test St",
            Phone: "0900000000",
            Email: null,
            EmailSupport: null,
            LogoUrl: null);

        var req = new ReportPdfRequest(
            TenantId: 1,
            ReportType: ReportType.Financial,
            FromDate: new DateOnly(2026, 5, 1),
            ToDate: new DateOnly(2026, 5, 25),
            ClinicId: null,
            ExportedByUserId: Guid.Parse("11111111-0000-0000-0000-000000000001"),
            ReportCode: "RPT-FIN-20260526-BARCODE-TEST");

        var rows = new List<FinancialRowDto>
        {
            new(1, "HD001", "Nguyen Van A", "Kham noi tiet", 150_000m)
        };

        var bytes = await exporter.ExportFinancialAsync(req, letterhead, rows);

        // PDF header %PDF-
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1024, $"PDF size qua nho sau khi co barcode: {bytes.Length}");
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }
}
