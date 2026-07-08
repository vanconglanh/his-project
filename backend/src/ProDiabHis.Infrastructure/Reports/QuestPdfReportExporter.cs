using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

public class QuestPdfReportExporter : IPdfReportExporter
{
    // Cac hang so mau + helper dung chung nam o ReportPdfCommon (tai su dung cho GenericReportPdfExporter).
    private static readonly string Brand = ReportPdfCommon.Brand;
    private static readonly string BrandDark = ReportPdfCommon.BrandDark;
    private static readonly string Ink = ReportPdfCommon.Ink;
    private static readonly string Muted = ReportPdfCommon.Muted;
    private static readonly string LineColor = ReportPdfCommon.LineColor;
    private static readonly string ZebraBg = ReportPdfCommon.ZebraBg;
    private static readonly string TintTeal = ReportPdfCommon.TintTeal;
    private static readonly string TintAmber = ReportPdfCommon.TintAmber;
    private static readonly string TintRed = ReportPdfCommon.TintRed;

    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<QuestPdfReportExporter>? _logger;
    private readonly IReadOnlyList<string> _allowedLogoHosts;

    static QuestPdfReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfReportExporter(
        IHttpClientFactory? httpClientFactory = null,
        ILogger<QuestPdfReportExporter>? logger = null,
        IConfiguration? configuration = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var configuredHosts = configuration?.GetSection("Reports:AllowedLogoHosts")
            .Get<string[]>();
        _allowedLogoHosts = configuredHosts ?? Array.Empty<string>();
    }

    // ===================== BACKWARD COMPAT ===================== //

    public Task<byte[]> ExportRevenueAsync(RevenueReportResponse report, CancellationToken ct = default)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text($"Báo cáo doanh thu — {report.From:dd/MM/yyyy} đến {report.To:dd/MM/yyyy}")
                    .SemiBold().FontSize(16);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Tổng doanh thu: {report.TotalRevenue:#,##0} VND").FontSize(12);
                    col.Item().Text($"Số hóa đơn: {report.TotalInvoices}").FontSize(12);
                    col.Item().Text($"Doanh thu thuần: {report.NetRevenue:#,##0} VND").FontSize(12);

                    col.Item().PaddingTop(10).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().Text("Ngày").Bold();
                            h.Cell().Text("Doanh thu").Bold();
                        });

                        foreach (var pt in report.Series)
                        {
                            tbl.Cell().Text(pt.Label);
                            tbl.Cell().Text($"{pt.Value:#,##0}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ");
                    txt.CurrentPageNumber();
                    txt.Span(" / ");
                    txt.TotalPages();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    public Task<byte[]> ExportDoctorKpiAsync(
        IReadOnlyList<DoctorKpiResponse> rows, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);

                page.Header().Text($"KPI Bác sĩ — {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}")
                    .SemiBold().FontSize(16);

                page.Content().Table(tbl =>
                {
                    tbl.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(1);
                    });

                    tbl.Header(h =>
                    {
                        h.Cell().Text("Bác sĩ").Bold();
                        h.Cell().Text("Lượt khám").Bold();
                        h.Cell().Text("Doanh thu").Bold();
                        h.Cell().Text("TB/lượt").Bold();
                        h.Cell().Text("Đơn thuốc").Bold();
                    });

                    foreach (var r in rows)
                    {
                        tbl.Cell().Text(r.DoctorName);
                        tbl.Cell().Text(r.TotalEncounters.ToString());
                        tbl.Cell().Text($"{r.TotalRevenue:#,##0}");
                        tbl.Cell().Text($"{r.AvgRevenuePerEncounter:#,##0}");
                        tbl.Cell().Text(r.PrescriptionCount.ToString());
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ");
                    txt.CurrentPageNumber();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    // ===================== A4 REPORT (3 loai moi) ===================== //

    public async Task<byte[]> ExportFinancialAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<FinancialRowDto> rows,
        CancellationToken ct = default)
    {
        var rowList = rows.ToList();
        byte[]? logoBytes = await TryFetchLogoAsync(letterhead.LogoUrl, ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "BÁO CÁO DOANH THU"));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => ReportPdfCommon.RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderFinancialKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderFinancialBody(c, rowList));
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, req.ExportedByFullName));
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportClinicalAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<ClinicalRowDto> rows,
        CancellationToken ct = default)
    {
        var rowList = rows.ToList();
        byte[]? logoBytes = await TryFetchLogoAsync(letterhead.LogoUrl, ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "BÁO CÁO LƯỢT KHÁM"));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => ReportPdfCommon.RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderClinicalKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderClinicalBody(c, rowList));
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, req.ExportedByFullName));
            });
        }).GeneratePdf();

        return bytes;
    }

    public async Task<byte[]> ExportPharmacyAsync(
        ReportPdfRequest req,
        LetterheadDto letterhead,
        IEnumerable<PharmacyRowDto> rows,
        CancellationToken ct = default)
    {
        var rowList = rows.ToList();
        byte[]? logoBytes = await TryFetchLogoAsync(letterhead.LogoUrl, ct);

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "BÁO CÁO TỒN KHO DƯỢC"));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => ReportPdfCommon.RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderPharmacyKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderPharmacyBody(c, rowList));
                    col.Item().PaddingTop(8).Element(RenderPharmacyLegend);
                });

                page.Footer().Element(c => ReportPdfCommon.RenderFooter(c, req.ExportedByFullName));
            });
        }).GeneratePdf();

        return bytes;
    }

    // ===================== PRIVATE HELPERS (rieng cho 3 loai bao cao cu) ===================== //

    // ===== KPI cards (tinh tu du lieu that trong request/rows) ===== //

    private static void RenderFinancialKpis(IContainer container, List<FinancialRowDto> rows)
    {
        var total = rows.Sum(r => r.Amount);
        var count = rows.Count;
        var avg = count > 0 ? total / count : 0m;

        container.Row(r =>
        {
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "TỔNG DOANH THU", $"{total:#,##0} đ", TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "SỐ HÓA ĐƠN", count.ToString(), TintAmber));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "TRUNG BÌNH / HĐ", $"{avg:#,##0} đ", TintTeal));
        });
    }

    private static void RenderClinicalKpis(IContainer container, List<ClinicalRowDto> rows)
    {
        var totalVisits = rows.Count;
        var doctorCount = rows.Select(r => r.DoctorName).Distinct().Count();
        // Chan doan dai thao duong: ICD-10 nhom E10-E14 (theo pham vi nghiep vu Pro-Diab)
        var diabetesCount = rows.Count(r => !string.IsNullOrWhiteSpace(r.Icd10Code)
            && r.Icd10Code.Length >= 3
            && r.Icd10Code.StartsWith("E1", StringComparison.OrdinalIgnoreCase)
            && r.Icd10Code[2] is >= '0' and <= '4');

        container.Row(r =>
        {
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "TỔNG LƯỢT KHÁM", totalVisits.ToString(), TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "SỐ BÁC SĨ", doctorCount.ToString(), TintAmber));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "CHẨN ĐOÁN ĐTĐ", diabetesCount.ToString(), TintTeal));
        });
    }

    private static void RenderPharmacyKpis(IContainer container, List<PharmacyRowDto> rows)
    {
        var itemCount = rows.Count;
        var outOfStock = rows.Count(r => r.StockQuantity <= 0);
        var nearExpiry = rows.Count(r => r.ExpiryDate.HasValue
            && r.ExpiryDate.Value > DateOnly.FromDateTime(DateTime.Now)
            && r.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(90)));

        container.Row(r =>
        {
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "MẶT HÀNG", itemCount.ToString(), TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "HẾT HÀNG", outOfStock.ToString(), TintRed));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => ReportPdfCommon.RenderKpi(x, "CẬN HẠN SỬ DỤNG (≤90 ngày)", nearExpiry.ToString(), TintAmber));
        });
    }

    private static void RenderPharmacyLegend(IContainer container)
    {
        container.Text(t =>
        {
            t.Span("Chú thích:  ").FontColor(Muted).FontSize(8.5f);
            t.Span("● ").FontColor("#059669").FontSize(9);
            t.Span("Bình thường   ").FontSize(8.5f);
            t.Span("● ").FontColor("#D97706").FontSize(9);
            t.Span("Cận HSD   ").FontSize(8.5f);
            t.Span("● ").FontColor("#DC2626").FontSize(9);
            t.Span("Hết hàng / Hết hạn").FontSize(8.5f);
        });
    }

    /// <summary>Trang thai ton kho suy ra tu du lieu that (StockQuantity + ExpiryDate), khong bia du lieu.</summary>
    private static (string Label, string Color) GetStockStatus(PharmacyRowDto r)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (r.ExpiryDate.HasValue && r.ExpiryDate.Value <= today)
            return ("Hết hạn", "#DC2626");
        if (r.StockQuantity <= 0)
            return ("Hết hàng", "#DC2626");
        if (r.ExpiryDate.HasValue && r.ExpiryDate.Value <= today.AddDays(90))
            return ("Cận HSD", "#D97706");
        return ("Bình thường", "#059669");
    }

    private static void RenderFinancialBody(IContainer container, List<FinancialRowDto> rows)
    {
        // Financial: STT / So HD / Benh nhan / Dich vu / Thanh tien
        var total = rows.Sum(r => r.Amount);
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28);
                cols.RelativeColumn(2.1f);
                cols.RelativeColumn(2.4f);
                cols.RelativeColumn(3.2f);
                cols.RelativeColumn(2f);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "STT");
                ReportPdfCommon.HText(h.Cell(), "Số HĐ");
                ReportPdfCommon.HText(h.Cell(), "Bệnh nhân");
                ReportPdfCommon.HText(h.Cell(), "Dịch vụ");
                ReportPdfCommon.HeadCell(h.Cell()).AlignRight().Text("Thành tiền").FontColor("#FFFFFF").Bold().FontSize(9.5f);
            });

            var i = 0;
            foreach (var r in rows)
            {
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.Stt.ToString()).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.InvoiceNo).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.PatientName).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.ServiceName).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).AlignRight().Text($"{r.Amount:#,##0}").FontSize(9.5f).SemiBold();
                i++;
            }

            // Dai tong cong
            tbl.Cell().ColumnSpan(4)
                .Background(BrandDark).PaddingVertical(7).PaddingHorizontal(6)
                .AlignRight().Text("TỔNG CỘNG").FontColor("#FFFFFF").Bold();
            tbl.Cell()
                .Background(BrandDark).PaddingVertical(7).PaddingHorizontal(6)
                .AlignRight().Text($"{total:#,##0} đ").FontColor("#FFFFFF").Bold();
        });
    }

    private static void RenderClinicalBody(IContainer container, List<ClinicalRowDto> rows)
    {
        // Clinical: STT / Benh nhan / Bac si / ICD-10 / Ngay kham
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28);
                cols.RelativeColumn(2.6f);
                cols.RelativeColumn(2.6f);
                cols.RelativeColumn(3.4f);
                cols.RelativeColumn(1.8f);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "STT");
                ReportPdfCommon.HText(h.Cell(), "Bệnh nhân");
                ReportPdfCommon.HText(h.Cell(), "Bác sĩ");
                ReportPdfCommon.HText(h.Cell(), "Chẩn đoán (ICD-10)");
                ReportPdfCommon.HText(h.Cell(), "Ngày khám");
            });

            var i = 0;
            foreach (var r in rows)
            {
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.Stt.ToString()).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.PatientName).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.DoctorName).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.Icd10Code).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.EncounterDate.ToString("dd/MM/yyyy")).FontSize(9.5f);
                i++;
            }
        });
    }

    private static void RenderPharmacyBody(IContainer container, List<PharmacyRowDto> rows)
    {
        // Pharmacy: Ma thuoc / Ten thuoc / Lo / HSD / Ton / Don vi / Trang thai (suy tu du lieu that)
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(1.4f);
                cols.RelativeColumn(3f);
                cols.RelativeColumn(2f);
                cols.RelativeColumn(1.6f);
                cols.RelativeColumn(1.1f);
                cols.RelativeColumn(1.1f);
                cols.RelativeColumn(1.8f);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "Mã");
                ReportPdfCommon.HText(h.Cell(), "Tên thuốc");
                ReportPdfCommon.HText(h.Cell(), "Lô");
                ReportPdfCommon.HText(h.Cell(), "HSD");
                ReportPdfCommon.HeadCell(h.Cell()).AlignRight().Text("Tồn").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                ReportPdfCommon.HText(h.Cell(), "ĐVT");
                ReportPdfCommon.HText(h.Cell(), "Trạng thái");
            });

            var i = 0;
            foreach (var r in rows)
            {
                var (label, color) = GetStockStatus(r);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.DrugCode).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.DrugName).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.LotNumber ?? "-").FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.ExpiryDate?.ToString("dd/MM/yyyy") ?? "-").FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).AlignRight().Text($"{r.StockQuantity:#,##0}").FontSize(9.5f).SemiBold();
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(r.Unit).FontSize(9.5f);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(t =>
                {
                    t.Span("● ").FontColor(color).FontSize(9);
                    t.Span(label).FontSize(9);
                });
                i++;
            }
        });
    }

    private Task<byte[]?> TryFetchLogoAsync(string? logoUrl, CancellationToken ct)
        => ReportPdfCommon.TryFetchLogoAsync(_httpClientFactory, _logger, _allowedLogoHosts, logoUrl, ct);
}
