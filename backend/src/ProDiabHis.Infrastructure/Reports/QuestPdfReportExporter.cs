using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ZXing;
using ZXing.Common;

namespace ProDiabHis.Infrastructure.Reports;

public class QuestPdfReportExporter : IPdfReportExporter
{
    // Mau nhan dien diaB chinh thuc (dong bo voi preview thiet ke da duyet)
    private static readonly string Brand = "#01645A";
    private static readonly string BrandDark = "#014A42";
    private static readonly string Ink = "#0F172A";
    private static readonly string Muted = "#64748B";
    private static readonly string LineColor = "#E2E8F0";
    private static readonly string ZebraBg = "#F3F8F7";
    private static readonly string TintTeal = "#F0FDFA";
    private static readonly string TintAmber = "#FFFBEB";
    private static readonly string TintRed = "#FEF2F2";

    // Logo diaB bundled (wwwroot/brand/diab-logo.png) — fallback khi tenant chua cau hinh LogoUrl
    private static readonly byte[]? DefaultLogo = LoadDefaultLogo();
    private static byte[]? LoadDefaultLogo()
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "brand", "diab-logo.png");
            return System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;
        }
        catch { return null; }
    }

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

    /// <summary>
    /// Kiem tra IP co phai la private/loopback theo RFC1918 / RFC4193.
    /// Tra ve true neu host bi chan (SSRF risk).
    /// </summary>
    private static bool IsPrivateOrLoopbackAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        // Map IPv4-mapped IPv6 sang IPv4 truoc khi kiem tra
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            // IPv4: kiem tra RFC1918 + APIPA
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10                                    // 10.0.0.0/8
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) // 172.16.0.0/12
                || (bytes[0] == 192 && bytes[1] == 168)              // 192.168.0.0/16
                || (bytes[0] == 169 && bytes[1] == 254);             // 169.254.0.0/16 (APIPA)
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // IPv6: ::1 loopback da xu ly o tren; fc00::/7 (ULA)
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC; // fc00::/7
        }

        return false;
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
                ConfigureA4Page(page);

                page.Header().Element(c => RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => RenderTitle(c, "BÁO CÁO DOANH THU"));
                    col.Item().Element(c => RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderFinancialKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderFinancialBody(c, rowList));
                });

                page.Footer().Element(c => RenderFooter(c, req.ExportedByFullName));
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
                ConfigureA4Page(page);

                page.Header().Element(c => RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => RenderTitle(c, "BÁO CÁO LƯỢT KHÁM"));
                    col.Item().Element(c => RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderClinicalKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderClinicalBody(c, rowList));
                });

                page.Footer().Element(c => RenderFooter(c, req.ExportedByFullName));
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
                ConfigureA4Page(page);

                page.Header().Element(c => RenderLetterhead(c, letterhead, logoBytes));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => RenderTitle(c, "BÁO CÁO TỒN KHO DƯỢC"));
                    col.Item().Element(c => RenderBarcode(c, req.ReportCode));
                    col.Item().Element(c => RenderMeta(c, req));
                    col.Item().PaddingTop(10).Element(c => RenderPharmacyKpis(c, rowList));
                    col.Item().PaddingTop(12).Element(c => RenderPharmacyBody(c, rowList));
                    col.Item().PaddingTop(8).Element(RenderPharmacyLegend);
                });

                page.Footer().Element(c => RenderFooter(c, req.ExportedByFullName));
            });
        }).GeneratePdf();

        return bytes;
    }

    // ===================== PRIVATE HELPERS ===================== //

    private static void ConfigureA4Page(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginTop(15, Unit.Millimetre);
        page.MarginBottom(15, Unit.Millimetre);
        page.MarginHorizontal(15, Unit.Millimetre);
        page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));
    }

    // ===== helper bang: header xanh chu trang + zebra rows (dong bo preview da duyet) ===== //
    private static IContainer HeadCell(IContainer c) => c.Background(Brand).PaddingVertical(7).PaddingHorizontal(6);
    private static void HText(IContainer c, string s) => HeadCell(c).Text(s).FontColor("#FFFFFF").Bold().FontSize(9.5f);
    private static IContainer BodyCell(IContainer c, int i) => c.Background(i % 2 == 1 ? ZebraBg : "#FFFFFF")
        .PaddingVertical(6).PaddingHorizontal(6).BorderBottom(0.5f).BorderColor(LineColor);

    private static void RenderKpi(IContainer c, string label, string value, string tint) => c
        .Background(tint).Border(1).BorderColor(LineColor)
        .PaddingVertical(9).PaddingHorizontal(11)
        .Column(col =>
        {
            col.Item().Text(label).FontColor(Muted).FontSize(8.5f);
            col.Item().PaddingTop(2).Text(value).FontColor(Brand).Bold().FontSize(14);
        });

    private static void RenderLetterhead(IContainer container, LetterheadDto lh, byte[]? logoBytes)
    {
        container
            .Background(Brand)
            .Padding(12)
            .Row(row =>
            {
                // Logo ben trai: uu tien LogoUrl cua tenant, sau do logo diaB bundled
                var logo = (logoBytes != null && logoBytes.Length > 0) ? logoBytes : DefaultLogo;
                if (logo != null && logo.Length > 0)
                {
                    // Chip trang de logo mau noi tren nen header xanh (dong bo preview: 74px)
                    row.ConstantItem(74)
                        .AlignMiddle()
                        .Background("#FFFFFF")
                        .Padding(5)
                        .Image(logo)
                        .FitArea();
                }
                else
                {
                    row.ConstantItem(74)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text("diaB")
                        .FontColor("#FFFFFF")
                        .Bold()
                        .FontSize(16);
                }

                // Thong tin phong kham
                row.RelativeItem()
                    .PaddingLeft(10)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text(lh.ClinicName).FontColor("#FFFFFF").Bold().FontSize(16);
                        if (!string.IsNullOrWhiteSpace(lh.CompanyName))
                            col.Item().Text(lh.CompanyName).FontColor("#D7EBE7").FontSize(9.5f);
                        if (!string.IsNullOrWhiteSpace(lh.Address))
                            col.Item().PaddingTop(2).Text(lh.Address).FontColor("#D7EBE7").FontSize(9);

                        var line2 = new List<string>();
                        if (!string.IsNullOrWhiteSpace(lh.CskcbCode))
                            line2.Add($"Mã CSKCB: {lh.CskcbCode}");
                        if (!string.IsNullOrWhiteSpace(lh.Phone))
                            line2.Add($"ĐT: {lh.Phone}");
                        var email = lh.EmailSupport ?? lh.Email;
                        if (!string.IsNullOrWhiteSpace(email))
                            line2.Add(email!);
                        if (line2.Count > 0)
                            col.Item().Text(string.Join("   •   ", line2)).FontColor("#D7EBE7").FontSize(9);
                    });
            });
    }

    private static void RenderTitle(IContainer container, string title)
    {
        container
            .PaddingTop(14)
            .AlignCenter()
            .Text(title)
            .FontSize(17)
            .Bold()
            .FontColor(Brand)
            .LetterSpacing(0.02f);
    }

    private static void RenderBarcode(IContainer container, string code)
    {
        byte[]? barcodePng = TryGenerateCode128Png(code);

        if (barcodePng != null)
        {
            // Render barcode CODE128 that (ZXing.Net)
            container
                .PaddingTop(6)
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Width(160)
                        .Height(40)
                        .Image(barcodePng)
                        .FitArea();

                    col.Item()
                        .PaddingTop(3)
                        .AlignCenter()
                        .Border(1).BorderColor(Brand).Background(TintTeal)
                        .PaddingVertical(2).PaddingHorizontal(10)
                        .Text(t =>
                        {
                            t.Span("Mã: ").FontColor(Muted).FontSize(9);
                            t.Span(code).FontColor(Brand).Bold().FontSize(10).FontFamily("Courier New");
                        });
                });
        }
        else
        {
            // Fallback: chip mau khi ZXing khong sinh duoc barcode
            System.Diagnostics.Debug.WriteLine($"[QuestPdfReportExporter] ZXing khong the sinh barcode CODE128 cho ma {code}, dung fallback text");
            container
                .PaddingTop(6)
                .AlignCenter()
                .Border(1).BorderColor(Brand).Background(TintTeal)
                .PaddingVertical(2).PaddingHorizontal(10)
                .Text(t =>
                {
                    t.Span("Mã: ").FontColor(Muted).FontSize(9);
                    t.Span(code).FontColor(Brand).Bold().FontSize(10).FontFamily("Courier New");
                });
        }
    }

    /// <summary>Sinh barcode CODE128 dang PNG bytes dung ZXing.Net + SkiaSharp. Tra null neu loi.</summary>
    private static byte[]? TryGenerateCode128Png(string code)
    {
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 400,
                    Height = 80,
                    Margin = 4,
                    PureBarcode = false
                }
            };

            var pixelData = writer.Write(code);

            // ZXing tra ve BGRA raw pixels — dung SkiaSharp de encode sang PNG
            using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            System.Runtime.InteropServices.Marshal.Copy(
                pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static void RenderMeta(IContainer container, ReportPdfRequest req)
    {
        // Minor #13: Hien thi fullName thay vi Guid raw
        var exportedByLabel = !string.IsNullOrWhiteSpace(req.ExportedByFullName)
            ? req.ExportedByFullName
            : "—";

        container.PaddingTop(10).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Kỳ báo cáo: ").FontColor(Muted).FontSize(9.5f);
                    t.Span($"{req.FromDate:dd/MM/yyyy} - {req.ToDate:dd/MM/yyyy}").FontColor(Ink).SemiBold().FontSize(9.5f);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Người xuất: ").FontColor(Muted).FontSize(9.5f);
                    t.Span(exportedByLabel).FontColor(Ink).SemiBold().FontSize(9.5f);
                });
            });
            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Ngày xuất: ").FontColor(Muted).FontSize(9.5f);
                    t.Span($"{DateTime.Now:dd/MM/yyyy}").FontColor(Ink).SemiBold().FontSize(9.5f);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Mã báo cáo: ").FontColor(Muted).FontSize(9.5f);
                    t.Span(req.ReportCode).FontColor(Ink).SemiBold().FontSize(9.5f);
                });
            });
        });
    }

    private static void RenderFooter(IContainer container, string? exportedByFullName)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(LineColor);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem()
                    .Column(c =>
                    {
                        c.Item().Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                            .Italic().FontSize(9).FontColor(Muted);
                        c.Item().PaddingTop(1).Text("NGƯỜI LẬP BÁO CÁO").Bold().FontSize(9.5f).FontColor(Ink);
                        c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8.5f).FontColor(Muted);
                    });

                row.RelativeItem()
                    .AlignRight()
                    .AlignBottom()
                    .Text(txt =>
                    {
                        txt.Span("Trang ").FontSize(8.5f).FontColor(Muted);
                        txt.CurrentPageNumber().FontSize(8.5f).FontColor(Muted);
                        txt.Span(" / ").FontSize(8.5f).FontColor(Muted);
                        txt.TotalPages().FontSize(8.5f).FontColor(Muted);
                    });
            });
        });
    }

    // ===== KPI cards (tinh tu du lieu that trong request/rows) ===== //

    private static void RenderFinancialKpis(IContainer container, List<FinancialRowDto> rows)
    {
        var total = rows.Sum(r => r.Amount);
        var count = rows.Count;
        var avg = count > 0 ? total / count : 0m;

        container.Row(r =>
        {
            r.RelativeItem().Element(x => RenderKpi(x, "TỔNG DOANH THU", $"{total:#,##0} đ", TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "SỐ HÓA ĐƠN", count.ToString(), TintAmber));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "TRUNG BÌNH / HĐ", $"{avg:#,##0} đ", TintTeal));
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
            r.RelativeItem().Element(x => RenderKpi(x, "TỔNG LƯỢT KHÁM", totalVisits.ToString(), TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "SỐ BÁC SĨ", doctorCount.ToString(), TintAmber));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "CHẨN ĐOÁN ĐTĐ", diabetesCount.ToString(), TintTeal));
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
            r.RelativeItem().Element(x => RenderKpi(x, "MẶT HÀNG", itemCount.ToString(), TintTeal));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "HẾT HÀNG", outOfStock.ToString(), TintRed));
            r.ConstantItem(10);
            r.RelativeItem().Element(x => RenderKpi(x, "CẬN HẠN SỬ DỤNG (≤90 ngày)", nearExpiry.ToString(), TintAmber));
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
                HText(h.Cell(), "STT");
                HText(h.Cell(), "Số HĐ");
                HText(h.Cell(), "Bệnh nhân");
                HText(h.Cell(), "Dịch vụ");
                HeadCell(h.Cell()).AlignRight().Text("Thành tiền").FontColor("#FFFFFF").Bold().FontSize(9.5f);
            });

            var i = 0;
            foreach (var r in rows)
            {
                BodyCell(tbl.Cell(), i).Text(r.Stt.ToString()).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.InvoiceNo).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.PatientName).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.ServiceName).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).AlignRight().Text($"{r.Amount:#,##0}").FontSize(9.5f).SemiBold();
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
                HText(h.Cell(), "STT");
                HText(h.Cell(), "Bệnh nhân");
                HText(h.Cell(), "Bác sĩ");
                HText(h.Cell(), "Chẩn đoán (ICD-10)");
                HText(h.Cell(), "Ngày khám");
            });

            var i = 0;
            foreach (var r in rows)
            {
                BodyCell(tbl.Cell(), i).Text(r.Stt.ToString()).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.PatientName).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.DoctorName).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.Icd10Code).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.EncounterDate.ToString("dd/MM/yyyy")).FontSize(9.5f);
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
                HText(h.Cell(), "Mã");
                HText(h.Cell(), "Tên thuốc");
                HText(h.Cell(), "Lô");
                HText(h.Cell(), "HSD");
                HeadCell(h.Cell()).AlignRight().Text("Tồn").FontColor("#FFFFFF").Bold().FontSize(9.5f);
                HText(h.Cell(), "ĐVT");
                HText(h.Cell(), "Trạng thái");
            });

            var i = 0;
            foreach (var r in rows)
            {
                var (label, color) = GetStockStatus(r);
                BodyCell(tbl.Cell(), i).Text(r.DrugCode).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.DrugName).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.LotNumber ?? "-").FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(r.ExpiryDate?.ToString("dd/MM/yyyy") ?? "-").FontSize(9.5f);
                BodyCell(tbl.Cell(), i).AlignRight().Text($"{r.StockQuantity:#,##0}").FontSize(9.5f).SemiBold();
                BodyCell(tbl.Cell(), i).Text(r.Unit).FontSize(9.5f);
                BodyCell(tbl.Cell(), i).Text(t =>
                {
                    t.Span("● ").FontColor(color).FontSize(9);
                    t.Span(label).FontSize(9);
                });
                i++;
            }
        });
    }

    private async Task<byte[]?> TryFetchLogoAsync(string? logoUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) || _httpClientFactory == null)
            return null;

        // Validate URL scheme
        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri))
        {
            _logger?.LogWarning("Logo URL khong hop le (parse that bai): {LogoUrl}", logoUrl);
            return null;
        }

        bool isLocalhostDev = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                           || uri.Host.Equals("127.0.0.1");

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            && !(uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && isLocalhostDev))
        {
            _logger?.LogWarning("Logo URL bi chan: scheme khong cho phep: {Scheme} url={LogoUrl}", uri.Scheme, logoUrl);
            return null;
        }

        // Kiem tra whitelist host (neu co cau hinh)
        if (_allowedLogoHosts.Count > 0
            && !_allowedLogoHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            _logger?.LogWarning("Logo URL bi chan: host khong nam trong whitelist: {Host}", uri.Host);
            return null;
        }

        // SSRF: resolve DNS va kiem tra IP private/loopback
        if (!isLocalhostDev)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
                foreach (var addr in addresses)
                {
                    if (IsPrivateOrLoopbackAddress(addr))
                    {
                        _logger?.LogWarning(
                            "Logo URL bi chan SSRF: host {Host} resolve sang IP noi bo {Ip}",
                            uri.Host, addr);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Khong the resolve DNS cho logo host {Host}", uri.Host);
                return null;
            }
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ReportLogo");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var logoBytes = await client.GetByteArrayAsync(logoUrl, cts.Token);
            return logoBytes.Length > 0 ? logoBytes : null;
        }
        catch
        {
            // Bo qua loi tai logo, tiep tuc render PDF khong co logo
            return null;
        }
    }
}
