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
    private static readonly string HeaderBg = "#01645A";
    private static readonly string TableHeaderBg = "#F0FDFA";
    private static readonly string BorderColor = "#D1D5DB";

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
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => RenderLetterhead(c, data));

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(6).AlignCenter().Text("ĐƠN THUỐC").FontSize(14).Bold();
                    col.Item().AlignCenter().Text($"Mã đơn: {data.PrescriptionCode}").FontSize(9);

                    col.Item().PaddingTop(6).Element(c => RenderPatientInfo(c, data));
                    col.Item().PaddingTop(6).Element(c => RenderItemsTable(c, data.Items));

                    if (!string.IsNullOrWhiteSpace(data.Note))
                    {
                        col.Item().PaddingTop(8).Text(txt =>
                        {
                            txt.Span("Lời dặn: ").Bold();
                            txt.Span(data.Note);
                        });
                    }

                    col.Item().PaddingTop(14).Element(c => RenderFooter(c, data));
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ").FontSize(8);
                    txt.CurrentPageNumber().FontSize(8);
                    txt.Span("/").FontSize(8);
                    txt.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static void RenderLetterhead(IContainer container, PrescriptionPdfData data)
    {
        container
            .Background(HeaderBg)
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
                            col.Item().Text(data.ClinicAddress).FontColor("#FFFFFF").FontSize(8);

                        var contactParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(data.ClinicPhone))
                            contactParts.Add($"ĐT: {data.ClinicPhone}");
                        if (!string.IsNullOrWhiteSpace(data.CskcbCode))
                            contactParts.Add($"Mã CSKCB: {data.CskcbCode}");
                        if (contactParts.Count > 0)
                            col.Item().Text(string.Join("  |  ", contactParts)).FontColor("#FFFFFF").FontSize(8);
                    });
            });
    }

    private static void RenderPatientInfo(IContainer container, PrescriptionPdfData data)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(txt =>
                {
                    txt.Span("Họ tên: ").Bold();
                    txt.Span(data.PatientFullName);
                });
                row.RelativeItem(1).Text(txt =>
                {
                    txt.Span("Giới tính: ").Bold();
                    txt.Span(GenderLabel(data.PatientGender));
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(txt =>
                {
                    txt.Span("Năm sinh: ").Bold();
                    txt.Span(data.PatientDateOfBirth.HasValue ? data.PatientDateOfBirth.Value.Year.ToString() : "—");
                });
                row.RelativeItem(1).Text(txt =>
                {
                    txt.Span("Ngày kê: ").Bold();
                    txt.Span($"{data.PrescribedAt:dd/MM/yyyy}");
                });
            });

            if (!string.IsNullOrWhiteSpace(data.PatientAddress))
            {
                col.Item().Text(txt =>
                {
                    txt.Span("Địa chỉ: ").Bold();
                    txt.Span(data.PatientAddress);
                });
            }

            if (!string.IsNullOrWhiteSpace(data.DiagnosisCode) || !string.IsNullOrWhiteSpace(data.DiagnosisName))
            {
                col.Item().Text(txt =>
                {
                    txt.Span("Chẩn đoán: ").Bold();
                    var diagnosisText = string.Join(" - ",
                        new[] { data.DiagnosisCode, data.DiagnosisName }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                    txt.Span(diagnosisText);
                });
            }
        });
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
                cols.RelativeColumn(1);
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
                cols.RelativeColumn(5);
            });

            tbl.Header(h =>
            {
                foreach (var label in new[] { "STT", "Tên thuốc (hàm lượng)", "SL", "Cách dùng" })
                {
                    h.Cell()
                        .Background(TableHeaderBg)
                        .Border(0.5f).BorderColor(BorderColor)
                        .Padding(3)
                        .Text(label).Bold().FontSize(9);
                }
            });

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

                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(it.Stt.ToString());
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(drugLabel);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(qtyLabel);
                tbl.Cell().Border(0.5f).BorderColor(BorderColor).Padding(3).Text(usageLabel);
            }
        });
    }

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
                    col.Item().AlignCenter().Text($"Ngày {data.PrescribedAt:dd} tháng {data.PrescribedAt:MM} năm {data.PrescribedAt:yyyy}").Italic().FontSize(9);
                    col.Item().AlignCenter().Text("BÁC SĨ KÊ ĐƠN").Bold().FontSize(9);
                    col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8);
                    col.Item().PaddingTop(28).AlignCenter().Text(data.DoctorFullName ?? "").Bold().FontSize(9);
                });
        });
    }
}
