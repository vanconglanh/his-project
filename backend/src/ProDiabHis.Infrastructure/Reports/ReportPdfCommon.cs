using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Net;
using System.Net.Sockets;
using ZXing;
using ZXing.Common;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Helper dung chung cho moi PDF exporter (QuestPdfReportExporter 3 loai cu + GenericReportPdfExporter
/// cho Report Engine config-driven). Tach ra de tai su dung letterhead/brand/footer/barcode
/// ma khong lap lai code o nhieu class.
/// </summary>
public static class ReportPdfCommon
{
    // Mau nhan dien diaB chinh thuc (dong bo voi preview thiet ke da duyet)
    public const string Brand = "#01645A";
    public const string BrandDark = "#014A42";
    public const string Ink = "#0F172A";
    public const string Muted = "#64748B";
    public const string LineColor = "#E2E8F0";
    public const string ZebraBg = "#F3F8F7";
    public const string TintTeal = "#F0FDFA";
    public const string TintAmber = "#FFFBEB";
    public const string TintRed = "#FEF2F2";
    public const string TintNeutral = "#F1F5F9";

    // Logo diaB bundled (wwwroot/brand/diab-logo.png) — fallback khi tenant chua cau hinh LogoUrl
    public static readonly byte[]? DefaultLogo = LoadDefaultLogo();

    private static byte[]? LoadDefaultLogo()
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "brand", "diab-logo.png");
            return System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;
        }
        catch { return null; }
    }

    public static void ConfigureA4Page(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginTop(15, Unit.Millimetre);
        page.MarginBottom(15, Unit.Millimetre);
        page.MarginHorizontal(15, Unit.Millimetre);
        page.DefaultTextStyle(x => x.FontSize(10).FontColor(Ink));
    }

    public static void ConfigureA4LandscapePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4.Landscape());
        page.MarginTop(12, Unit.Millimetre);
        page.MarginBottom(12, Unit.Millimetre);
        page.MarginHorizontal(12, Unit.Millimetre);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink));
    }

    // ===== helper bang: header xanh chu trang + zebra rows (dong bo preview da duyet) ===== //
    public static IContainer HeadCell(IContainer c) => c.Background(Brand).PaddingVertical(7).PaddingHorizontal(6);
    public static void HText(IContainer c, string s) => HeadCell(c).Text(s).FontColor("#FFFFFF").Bold().FontSize(9.5f);
    public static IContainer BodyCell(IContainer c, int i) => c.Background(i % 2 == 1 ? ZebraBg : "#FFFFFF")
        .PaddingVertical(6).PaddingHorizontal(6).BorderBottom(0.5f).BorderColor(LineColor);

    public static void RenderKpi(IContainer c, string label, string value, string tint) => c
        .Background(tint).Border(1).BorderColor(LineColor)
        .PaddingVertical(9).PaddingHorizontal(11)
        .Column(col =>
        {
            col.Item().Text(label).FontColor(Muted).FontSize(8.5f);
            col.Item().PaddingTop(2).Text(value).FontColor(Brand).Bold().FontSize(14);
        });

    /// <summary>
    /// Header trang thanh lich, dong bo mau don thuoc that diaB: nen trang, logo ben trai,
    /// khoi thong tin phong kham canh giua ben phai, ket thuc bang 1 duong ke ngang mau Brand.
    /// </summary>
    public static void RenderLetterhead(IContainer container, LetterheadDto lh, byte[]? logoBytes)
    {
        container
            .Background(Brand)
            .Padding(12)
            .Row(row =>
            {
                // Logo ben trai: uu tien LogoUrl cua tenant, sau do logo diaB bundled.
                // Dat trong chip trang vi logo diaB mau teal se chim tren nen header teal.
                var logo = (logoBytes != null && logoBytes.Length > 0) ? logoBytes : DefaultLogo;
                if (logo != null && logo.Length > 0)
                {
                    row.ConstantItem(66)
                        .AlignMiddle()
                        .Background("#FFFFFF")
                        .Padding(4)
                        .Image(logo)
                        .FitArea();
                }
                else
                {
                    row.ConstantItem(66)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text("diaB")
                        .FontColor("#FFFFFF")
                        .Bold()
                        .FontSize(16);
                }

                // Thong tin phong kham — chu trang/nhat tren nen teal (dong bo mau don thuoc that)
                row.RelativeItem()
                    .PaddingLeft(12)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(lh.Slogan))
                            col.Item().Text(lh.Slogan!.ToUpperInvariant()).FontColor("#BFE0DB").FontSize(8).LetterSpacing(0.03f);

                        col.Item().PaddingTop(1).Text(lh.ClinicName).FontColor("#FFFFFF").Bold().FontSize(14);

                        if (!string.IsNullOrWhiteSpace(lh.CompanyName))
                            col.Item().Text(lh.CompanyName).FontColor("#FFFFFF").Bold().FontSize(11.5f);

                        if (!string.IsNullOrWhiteSpace(lh.Address))
                            col.Item().PaddingTop(2).Text(lh.Address).FontColor("#D7EBE7").FontSize(8.5f);

                        // Hang lien he voi icon SVG trang (dong bo mau don thuoc that: dien thoai / web / email)
                        var email = lh.EmailSupport ?? lh.Email;
                        var hasContact = !string.IsNullOrWhiteSpace(lh.Phone)
                            || !string.IsNullOrWhiteSpace(lh.Website)
                            || !string.IsNullOrWhiteSpace(email);
                        if (hasContact)
                        {
                            col.Item().PaddingTop(4).Row(r =>
                            {
                                if (!string.IsNullOrWhiteSpace(lh.Phone))
                                    ContactItem(r, IconPhone, lh.Phone!);
                                if (!string.IsNullOrWhiteSpace(lh.Website))
                                    ContactItem(r, IconGlobe, lh.Website!);
                                if (!string.IsNullOrWhiteSpace(email))
                                    ContactItem(r, IconEnvelope, email!);
                            });
                        }
                    });
            });
    }

    // Icon SVG (Material, fill trang) cho hang lien he tren header teal
    private const string IconPhone = "<svg viewBox='0 0 24 24'><path fill='#FFFFFF' d='M6.62 10.79c1.44 2.83 3.76 5.14 6.59 6.59l2.2-2.2c.27-.27.67-.36 1.02-.24 1.12.37 2.33.57 3.57.57.55 0 1 .45 1 1V20c0 .55-.45 1-1 1-9.39 0-17-7.61-17-17 0-.55.45-1 1-1h3.5c.55 0 1 .45 1 1 0 1.25.2 2.45.57 3.57.11.35.03.74-.25 1.02l-2.2 2.2z'/></svg>";
    private const string IconGlobe = "<svg viewBox='0 0 24 24'><path fill='#FFFFFF' d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z'/></svg>";
    private const string IconEnvelope = "<svg viewBox='0 0 24 24'><path fill='#FFFFFF' d='M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z'/></svg>";

    private static void ContactItem(QuestPDF.Fluent.RowDescriptor row, string svg, string value)
    {
        row.AutoItem().PaddingRight(16).Row(ci =>
        {
            ci.AutoItem().AlignMiddle().Width(10).Height(10).Svg(svg);
            ci.AutoItem().PaddingLeft(4).AlignMiddle().Text(value).FontColor("#EAF6F3").FontSize(8.5f);
        });
    }

    public static void RenderTitle(IContainer container, string title)
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

    public static void RenderBarcode(IContainer container, string code)
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
            System.Diagnostics.Debug.WriteLine($"[ReportPdfCommon] ZXing khong the sinh barcode CODE128 cho ma {code}, dung fallback text");
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
    public static byte[]? TryGenerateCode128Png(string code)
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

    public static void RenderMeta(IContainer container, ReportPdfRequest req)
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

    public static void RenderFooter(IContainer container, string? exportedByFullName)
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

    /// <summary>
    /// Kiem tra IP co phai la private/loopback theo RFC1918 / RFC4193.
    /// Tra ve true neu host bi chan (SSRF risk).
    /// </summary>
    public static bool IsPrivateOrLoopbackAddress(IPAddress address)
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

    /// <summary>Tai logo tu URL cua tenant voi kiem tra SSRF (scheme/host whitelist/DNS resolve). Tra null neu loi/bi chan.</summary>
    public static async Task<byte[]?> TryFetchLogoAsync(
        IHttpClientFactory? httpClientFactory,
        ILogger? logger,
        IReadOnlyList<string> allowedLogoHosts,
        string? logoUrl,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) || httpClientFactory == null)
            return null;

        // Validate URL scheme
        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri))
        {
            logger?.LogWarning("Logo URL khong hop le (parse that bai): {LogoUrl}", logoUrl);
            return null;
        }

        bool isLocalhostDev = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                           || uri.Host.Equals("127.0.0.1");

        if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            && !(uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && isLocalhostDev))
        {
            logger?.LogWarning("Logo URL bi chan: scheme khong cho phep: {Scheme} url={LogoUrl}", uri.Scheme, logoUrl);
            return null;
        }

        // Kiem tra whitelist host (neu co cau hinh)
        if (allowedLogoHosts.Count > 0
            && !allowedLogoHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            logger?.LogWarning("Logo URL bi chan: host khong nam trong whitelist: {Host}", uri.Host);
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
                        logger?.LogWarning(
                            "Logo URL bi chan SSRF: host {Host} resolve sang IP noi bo {Ip}",
                            uri.Host, addr);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Khong the resolve DNS cho logo host {Host}", uri.Host);
                return null;
            }
        }

        try
        {
            var client = httpClientFactory.CreateClient("ReportLogo");
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
