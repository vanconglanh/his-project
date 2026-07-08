using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh PDF don thuoc that bang QuestPDF (thay stub cu). Bo cuc A5 doc,
/// tai dung header trang thanh lich dung chung voi 30 bao cao + 6 giay to
/// (xem ReportPdfCommon.RenderLetterhead) de dong bo thiet ke toan he thong.
/// </summary>
public class PrescriptionPdfBuilder : IPrescriptionPdfBuilder
{
    private const string Brand = ReportPdfCommon.Brand;
    private const string Ink = ReportPdfCommon.Ink;
    private const string Muted = ReportPdfCommon.Muted;
    private const string LineColor = ReportPdfCommon.LineColor;

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

    /// <summary>
    /// Dung chung header trang voi 30 bao cao + 6 giay to (ReportPdfCommon.RenderLetterhead).
    /// Map PrescriptionPdfData sang LetterheadDto de tai su dung dung 1 diem thiet ke duy nhat.
    /// </summary>
    private static void RenderLetterhead(IContainer container, PrescriptionPdfData data)
    {
        var lh = new LetterheadDto(
            ClinicName: data.ClinicName,
            CskcbCode: data.CskcbCode,
            CompanyName: data.ClinicCompanyName,
            Address: data.ClinicAddress,
            Phone: data.ClinicPhone,
            Email: data.ClinicEmail,
            EmailSupport: null,
            LogoUrl: null,
            Slogan: data.ClinicSlogan,
            Website: data.ClinicWebsite);

        ReportPdfCommon.RenderLetterhead(container, lh, data.ClinicLogo);
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
                ReportPdfCommon.HText(h.Cell(), "#");
                ReportPdfCommon.HText(h.Cell(), "Tên thuốc");
                ReportPdfCommon.HText(h.Cell(), "Cách dùng");
                ReportPdfCommon.HeadCell(h.Cell()).AlignRight().Text("SL").FontColor("#FFFFFF").Bold().FontSize(9);
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

                i++;
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(it.Stt.ToString()).FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(t => t.Span(drugLabel).SemiBold().FontSize(9.5f));
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(usageLabel).FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).AlignRight().Text(qtyLabel).FontSize(9);
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
                    col.Item().AlignCenter().Text($"Ngày {data.PrescribedAt:dd} tháng {data.PrescribedAt:MM} năm {data.PrescribedAt:yyyy}").Italic().FontSize(8.5f).FontColor(Muted);
                    col.Item().AlignCenter().Text("BÁC SĨ KÊ ĐƠN").Bold().FontSize(9.5f);
                    col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(Muted);
                    col.Item().PaddingTop(22).AlignCenter().Text(data.DoctorFullName ?? "").SemiBold().FontSize(10);
                });
        });
    }
}
