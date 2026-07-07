using ProDiabHis.Application.Pharmacy.Prescriptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh PDF don thuoc that bang QuestPDF (thay stub cu). Bo cuc A5 doc,
/// tai dung tong mau nhan dien diaB (HeaderBg #01645A) giong QuestPdfReportExporter.
/// </summary>
public class PrescriptionPdfBuilder : IPrescriptionPdfBuilder
{
    private static readonly string Brand = "#01645A";
    private static readonly string Ink = "#0F172A";
    private static readonly string Muted = "#64748B";
    private static readonly string LineColor = "#E2E8F0";
    private static readonly string ZebraBg = "#F3F8F7";

    private static readonly byte[]? DefaultLogo = LoadDefaultLogo();
    private static byte[]? LoadDefaultLogo()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "brand", "diab-logo.png");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch { return null; }
    }

    static PrescriptionPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(PrescriptionPdfData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(10, Unit.Millimetre);
                page.MarginBottom(10, Unit.Millimetre);
                page.MarginHorizontal(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Ink));

                page.Header().Element(c => RenderLetterhead(c, data));

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("ĐƠN THUỐC").FontSize(15).Bold().FontColor(Brand);
                    col.Item().AlignCenter().Text($"Số: {data.PrescriptionCode}   •   Ngày kê: {data.PrescribedAt:dd/MM/yyyy}")
                        .FontSize(9).FontColor(Muted);

                    col.Item().PaddingTop(8).Element(c => RenderPatientInfo(c, data));
                    col.Item().PaddingTop(10).Element(c => RenderItemsTable(c, data.Items));

                    if (!string.IsNullOrWhiteSpace(data.Note))
                    {
                        col.Item().PaddingTop(8).Text(txt =>
                        {
                            txt.Span("Lời dặn: ").FontColor(Muted).SemiBold().FontSize(9.5f);
                            txt.Span(data.Note).FontSize(9.5f);
                        });
                    }

                    col.Item().PaddingTop(8).Element(c => RenderFooter(c, data));
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ").FontSize(8).FontColor(Muted);
                    txt.CurrentPageNumber().FontSize(8).FontColor(Muted);
                    txt.Span("/").FontSize(8).FontColor(Muted);
                    txt.TotalPages().FontSize(8).FontColor(Muted);
                });
            });
        }).GeneratePdf();
    }

    private static void RenderLetterhead(IContainer container, PrescriptionPdfData data)
    {
        container
            .Background(Brand)
            .Padding(8)
            .Row(row =>
            {
                var logo = (data.ClinicLogo != null && data.ClinicLogo.Length > 0) ? data.ClinicLogo : DefaultLogo;
                if (logo != null && logo.Length > 0)
                {
                    row.ConstantItem(48)
                        .AlignMiddle()
                        .Background("#FFFFFF")
                        .Padding(3)
                        .Image(logo)
                        .FitArea();
                }
                else
                {
                    row.ConstantItem(44)
                        .AlignMiddle()
                        .AlignCenter()
                        .Text("diaB")
                        .FontColor("#FFFFFF")
                        .Bold()
                        .FontSize(12);
                }

                row.RelativeItem()
                    .PaddingLeft(8)
                    .AlignMiddle()
                    .Column(col =>
                    {
                        col.Item().Text(data.ClinicName).FontColor("#FFFFFF").Bold().FontSize(12);
                        if (!string.IsNullOrWhiteSpace(data.ClinicAddress))
                            col.Item().Text(data.ClinicAddress).FontColor("#D7EBE7").FontSize(8);

                        var contactParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(data.ClinicPhone))
                            contactParts.Add($"ĐT: {data.ClinicPhone}");
                        if (!string.IsNullOrWhiteSpace(data.CskcbCode))
                            contactParts.Add($"Mã CSKCB: {data.CskcbCode}");
                        if (contactParts.Count > 0)
                            col.Item().Text(string.Join("   •   ", contactParts)).FontColor("#D7EBE7").FontSize(8);
                    });
            });
    }

    private static void RenderPatientInfo(IContainer container, PrescriptionPdfData data)
    {
        container
            .Border(1).BorderColor(LineColor).Background("#F8FAFC")
            .Padding(9)
            .Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(txt =>
                {
                    txt.Span("Họ tên: ").FontColor(Muted).FontSize(9);
                    txt.Span(data.PatientFullName).Bold().FontSize(10);
                });
                row.RelativeItem(1).Text(txt =>
                {
                    txt.Span("Giới tính: ").FontColor(Muted).FontSize(9);
                    txt.Span(GenderLabel(data.PatientGender)).FontSize(9);
                });
            });

            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem(2).Text(txt =>
                {
                    txt.Span("Năm sinh: ").FontColor(Muted).FontSize(9);
                    txt.Span(FormatBirthYearAndAge(data.PatientDateOfBirth)).FontSize(9);
                });
                row.RelativeItem(1).Text(txt =>
                {
                    txt.Span("Ngày kê: ").FontColor(Muted).FontSize(9);
                    txt.Span($"{data.PrescribedAt:dd/MM/yyyy}").FontSize(9);
                });
            });

            if (!string.IsNullOrWhiteSpace(data.PatientAddress))
            {
                col.Item().PaddingTop(2).Text(txt =>
                {
                    txt.Span("Địa chỉ: ").FontColor(Muted).FontSize(9);
                    txt.Span(data.PatientAddress).FontSize(9);
                });
            }

            if (!string.IsNullOrWhiteSpace(data.DiagnosisCode) || !string.IsNullOrWhiteSpace(data.DiagnosisName))
            {
                col.Item().PaddingTop(4).Text(txt =>
                {
                    txt.Span("Chẩn đoán: ").FontColor(Muted).FontSize(9);
                    var diagnosisText = string.Join(" - ",
                        new[] { data.DiagnosisCode, data.DiagnosisName }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                    txt.Span(diagnosisText).FontColor(Brand).SemiBold().FontSize(9);
                });
            }
        });
    }

    /// <summary>Format "1980 (46 tuổi)" tu ngay sinh that; tra "—" neu khong co du lieu.</summary>
    private static string FormatBirthYearAndAge(DateOnly? dob)
    {
        if (!dob.HasValue) return "—";
        var today = DateOnly.FromDateTime(DateTime.Now);
        var age = today.Year - dob.Value.Year;
        if (dob.Value > today.AddYears(-age)) age--;
        return $"{dob.Value.Year} ({age} tuổi)";
    }

    private static string GenderLabel(string? gender) => gender switch
    {
        "MALE" => "Nam",
        "FEMALE" => "Nữ",
        "OTHER" => "Khác",
        _ => "—"
    };

    private static void RenderItemsTable(IContainer container, IReadOnlyList<PrescriptionPdfItem> items)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(20);
                cols.RelativeColumn(4);
                cols.RelativeColumn(5);
                cols.RelativeColumn(1.6f);
            });

            tbl.Header(h =>
            {
                HText(h.Cell(), "#");
                HText(h.Cell(), "Tên thuốc");
                HText(h.Cell(), "Cách dùng");
                HeadCell(h.Cell()).AlignRight().Text("SL").FontColor("#FFFFFF").Bold().FontSize(9);
            });

            var i = 0;
            foreach (var it in items)
            {
                var drugLabel = string.IsNullOrWhiteSpace(it.Strength) ? it.DrugName : $"{it.DrugName} ({it.Strength})";
                var qtyLabel = string.IsNullOrWhiteSpace(it.Unit) ? $"{it.Quantity:0.##}" : $"{it.Quantity:0.##} {it.Unit}";
                var usageParts = new List<string> { it.Dosage, it.Frequency, RouteLabel(it.Route) };
                if (it.DurationDays > 0)
                    usageParts.Add($"{it.DurationDays} ngày");
                if (!string.IsNullOrWhiteSpace(it.Instructions))
                    usageParts.Add(it.Instructions);
                var usageLabel = string.Join(", ", usageParts.Where(s => !string.IsNullOrWhiteSpace(s)));

                BodyCell(tbl.Cell(), i).Text(it.Stt.ToString()).FontSize(9);
                BodyCell(tbl.Cell(), i).Text(t => t.Span(drugLabel).SemiBold().FontSize(9.5f));
                BodyCell(tbl.Cell(), i).Text(usageLabel).FontSize(9);
                BodyCell(tbl.Cell(), i).AlignRight().Text(qtyLabel).FontSize(9);
                i++;
            }
        });
    }

    private static IContainer HeadCell(IContainer c) => c.Background(Brand).PaddingVertical(5).PaddingHorizontal(4);
    private static void HText(IContainer c, string s) => HeadCell(c).Text(s).FontColor("#FFFFFF").Bold().FontSize(9);
    private static IContainer BodyCell(IContainer c, int i) => c.Background(i % 2 == 1 ? ZebraBg : "#FFFFFF")
        .PaddingVertical(4).PaddingHorizontal(4).BorderBottom(0.5f).BorderColor(LineColor);

    private static string RouteLabel(string? route) => route switch
    {
        "ORAL" => "Uống",
        "IV" => "Tiêm tĩnh mạch",
        "IM" => "Tiêm bắp",
        "SC" => "Tiêm dưới da",
        "TOPICAL" => "Bôi ngoài da",
        "INHALE" => "Hít",
        _ => route ?? ""
    };

    private static void RenderFooter(IContainer container, PrescriptionPdfData data)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item().AlignCenter().Text($"Ngày {data.PrescribedAt:dd} tháng {data.PrescribedAt:MM} năm {data.PrescribedAt:yyyy}").Italic().FontSize(8.5f).FontColor(Muted);
                    col.Item().AlignCenter().Text("BÁC SĨ KÊ ĐƠN").Bold().FontSize(9.5f);
                    col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(Muted);
                    col.Item().PaddingTop(22).AlignCenter().Text(data.DoctorFullName ?? "").SemiBold().FontSize(10);
                });
        });
    }
}
