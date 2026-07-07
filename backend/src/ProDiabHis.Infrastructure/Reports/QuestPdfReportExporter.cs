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
    // Mau nhan dien diaB chinh thuc
    private static readonly string HeaderBg = "#01645A";
    private static readonly string TableHeaderBg = "#F0FDFA"; // teal-50
    private static readonly string BorderColor = "#D1D5DB";   // gray-300

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
                    col.Item().PaddingTop(8).Element(c => RenderFinancialBody(c, rowList));
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
                    col.Item().PaddingTop(8).Element(c => RenderClinicalBody(c, rowList));
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
                    col.Item().PaddingTop(8).Element(c => RenderPharmacyBody(c, rowList));
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
        page.DefaultTextStyle(x => x.FontSize(11));
    }

    private static void RenderLetterhead(IContainer container, LetterheadDto lh, byte[]? logoBytes)
    {
        container
            .Background(HeaderBg)
            .Padding(10)
            .Row(row =>
            {
                // Logo ben trai: uu tien LogoUrl cua tenant, sau do logo diaB bundled
                var logo = (logoBytes != null && logoBytes.Length > 0) ? logoBytes : DefaultLogo;
                if (logo != null && logo.Length > 0)
                {
                    // Chip trang de logo mau noi tren nen header xanh
                    row.ConstantItem(64)
                        .AlignMiddle()
                        .Background("#FFFFFF")
                        .Padding(4)
                        .Image(logo)
                        .FitArea();
                }
                else
                {
                    row.ConstantItem(60)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text("diaB")
                        .FontColor("#FFFFFF")
                        .Bold()
                        .FontSize(14);
                }

                // Thong tin phong kham
                row.RelativeItem()
                    .PaddingLeft(10)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text(lh.ClinicName).FontColor("#FFFFFF").Bold().FontSize(16);
                        if (!string.IsNullOrWhiteSpace(lh.CompanyName))
                            col.Item().Text(lh.CompanyName).FontColor("#FFFFFF").FontSize(10);
                        if (!string.IsNullOrWhiteSpace(lh.Address))
                            col.Item().Text(lh.Address).FontColor("#FFFFFF").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(lh.CskcbCode))
                            col.Item().Text($"Mã CSKCB: {lh.CskcbCode}").FontColor("#FFFFFF").FontSize(9);

                        var contactParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(lh.Phone))
                            contactParts.Add($"ĐT: {lh.Phone}");
                        if (!string.IsNullOrWhiteSpace(lh.EmailSupport ?? lh.Email))
                            contactParts.Add($"Email: {lh.EmailSupport ?? lh.Email}");
                        if (contactParts.Count > 0)
                            col.Item().Text(string.Join("  |  ", contactParts)).FontColor("#FFFFFF").FontSize(9);
                    });
            });
    }

    private static void RenderTitle(IContainer container, string title)
    {
        container
            .PaddingTop(10)
            .AlignCenter()
            .Text(title)
            .FontSize(14)
            .Bold();
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
                        .PaddingTop(2)
                        .AlignCenter()
                        .Text(code)
                        .FontSize(9)
                        .FontFamily("Courier New");
                });
        }
        else
        {
            // Fallback: monospace box khi ZXing khong sinh duoc barcode
            System.Diagnostics.Debug.WriteLine($"[QuestPdfReportExporter] ZXing khong the sinh barcode CODE128 cho ma {code}, dung fallback text");
            container
                .PaddingTop(6)
                .AlignCenter()
                .Border(0.5f)
                .BorderColor(BorderColor)
                .Padding(6)
                .Text(code)
                .FontSize(11)
                .FontFamily("Courier New");
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

        container.PaddingTop(8).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"Kỳ báo cáo: {req.FromDate:dd/MM/yyyy} - {req.ToDate:dd/MM/yyyy}").FontSize(10);
                row.RelativeItem().Text($"Người xuất: {exportedByLabel}").FontSize(10);
            });
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                row.RelativeItem().Text($"Mã báo cáo: {req.ReportCode}").FontSize(10);
            });
        });
    }

    private static void RenderFooter(IContainer container, string? exportedByFullName)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(BorderColor);
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem()
                    .AlignCenter()
                    .Column(c =>
                    {
                        c.Item().Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                            .FontSize(10).Italic();
                        c.Item().Text("NGƯỜI LẬP BÁO CÁO").FontSize(10).Bold();
                        c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(9).Italic();
                    });

                row.RelativeItem()
                    .AlignRight()
                    .AlignBottom()
                    .Text(txt =>
                    {
                        txt.Span("Trang ").FontSize(9);
                        txt.CurrentPageNumber().FontSize(9);
                        txt.Span("/").FontSize(9);
                        txt.TotalPages().FontSize(9);
                    });
            });
        });
    }

    private static void RenderFinancialBody(IContainer container, List<FinancialRowDto> rows)
    {
        // Financial: STT / So HD / Benh nhan / Dich vu / Thanh tien
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(5);
                cols.RelativeColumn(15);
                cols.RelativeColumn(25);
                cols.RelativeColumn(35);
                cols.RelativeColumn(20);
            });

            tbl.Header(h =>
            {
                foreach (var label in new[] { "STT", "Số HĐ", "Bệnh nhân", "Dịch vụ", "Thành tiền" })
                {
                    h.Cell()
                        .Background(TableHeaderBg)
                        .Border(0.5f).BorderColor(BorderColor)
                        .Padding(4)
                        .Text(label).Bold().FontSize(11);
                }
            });

            foreach (var r in rows)
            {
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.Stt.ToString());
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.InvoiceNo);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.PatientName);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.ServiceName);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).AlignRight().Text($"{r.Amount:#,##0}");
            }

            // Summary row
            tbl.Cell().ColumnSpan(4)
                .Border(0.5f).BorderColor(BorderColor)
                .Padding(3).AlignRight()
                .Text("Tổng cộng:").Bold();
            tbl.Cell()
                .Border(0.5f).BorderColor(BorderColor)
                .Padding(3).AlignRight()
                .Text($"{rows.Sum(r => r.Amount):#,##0}").Bold();
        });
    }

    private static void RenderClinicalBody(IContainer container, List<ClinicalRowDto> rows)
    {
        // Clinical: STT / Benh nhan / Bac si / ICD-10 / Ngay kham
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(5);
                cols.RelativeColumn(25);
                cols.RelativeColumn(20);
                cols.RelativeColumn(20);
                cols.RelativeColumn(15);
            });

            tbl.Header(h =>
            {
                foreach (var label in new[] { "STT", "Bệnh nhân", "Bác sĩ", "ICD-10", "Ngày khám" })
                {
                    h.Cell()
                        .Background(TableHeaderBg)
                        .Border(0.5f).BorderColor(BorderColor)
                        .Padding(4)
                        .Text(label).Bold().FontSize(11);
                }
            });

            foreach (var r in rows)
            {
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.Stt.ToString());
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.PatientName);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.DoctorName);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.Icd10Code);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.EncounterDate.ToString("dd/MM/yyyy"));
            }

            // Summary row
            tbl.Cell().ColumnSpan(5)
                .Border(0.5f).BorderColor(BorderColor)
                .Padding(3).AlignRight()
                .Text($"Tổng lượt: {rows.Count}").Bold();
        });
    }

    private static void RenderPharmacyBody(IContainer container, List<PharmacyRowDto> rows)
    {
        // Pharmacy: Ma thuoc / Ten thuoc / Lo / HSD / Ton / Don vi
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(12);
                cols.RelativeColumn(30);
                cols.RelativeColumn(12);
                cols.RelativeColumn(14);
                cols.RelativeColumn(10);
                cols.RelativeColumn(12);
            });

            tbl.Header(h =>
            {
                foreach (var label in new[] { "Mã thuốc", "Tên thuốc", "Lô", "HSD", "Tồn", "Đơn vị" })
                {
                    h.Cell()
                        .Background(TableHeaderBg)
                        .Border(0.5f).BorderColor(BorderColor)
                        .Padding(4)
                        .Text(label).Bold().FontSize(11);
                }
            });

            foreach (var r in rows)
            {
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.DrugCode);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.DrugName);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.LotNumber ?? "-");
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.ExpiryDate?.ToString("dd/MM/yyyy") ?? "-");
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).AlignRight().Text($"{r.StockQuantity:#,##0}");
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(r.Unit);
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
