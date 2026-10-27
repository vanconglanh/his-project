using ProDiabHis.Application.Pharmacy.Prescriptions;
using ProDiabHis.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh PDF don thuoc bang QuestPDF, A5 doc, khop mau Figma.
/// Tai dung letterhead dung chung (ReportPdfCommon.RenderLetterhead) lay tu tenant.
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

                // Letterhead day du be ngang (banner co hang lien he AutoItem, khong ep vao cot hep)
                page.Header().Element(c => RenderLetterhead(c, data));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().AlignRight().Text(t =>
                    {
                        t.Span("Số đơn: ").FontColor(Muted).FontSize(8.5f);
                        t.Span(data.PrescriptionCode).Bold().FontSize(9);
                        t.Span($"   Ngày: {data.PrescribedAt:dd/MM/yyyy}").FontSize(8.5f);
                    });
                    col.Item().AlignCenter().Text("ĐƠN THUỐC").FontSize(17).Bold().FontColor(Brand);
                    col.Item().AlignCenter().Text("Prescription · Đơn thuốc").FontSize(8).FontColor(Muted);

                    col.Item().PaddingTop(6).Element(c => RenderPatientInfo(c, data));
                    col.Item().PaddingTop(8).Element(c => RenderItemsTable(c, data.Items));
                    col.Item().PaddingTop(3).Text("Số trong cột Sáng/Trưa/Chiều/Tối là số lượng mỗi lần dùng.")
                        .Italic().FontSize(7.5f).FontColor(Muted);

                    if (!string.IsNullOrWhiteSpace(data.Note))
                    {
                        col.Item().PaddingTop(8).Border(1).BorderColor(LineColor).Padding(6).Column(n =>
                        {
                            n.Item().Text("LỜI DẶN").Bold().FontSize(8.5f).FontColor(Brand);
                            n.Item().PaddingTop(2).Text(data.Note).FontSize(9);
                        });
                    }

                    col.Item().PaddingTop(10).Element(c => RenderSignature(c, data));
                });

                page.Footer().Column(f =>
                {
                    f.Item().AlignCenter().Text("Khám lại xin mang theo đơn này").Italic().FontSize(8).FontColor(Muted);
                    var contact = string.Join(" · ", new[] { data.ClinicEmail, data.ClinicPhone }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (contact.Length > 0)
                        f.Item().AlignCenter().Text(contact).FontSize(8).FontColor(Muted);
                });
            });
        }).GeneratePdf();
    }

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
        static void Cell(IContainer c, string label, string value, bool bold = false) =>
            c.Text(t =>
            {
                t.Span(label + ": ").FontColor(Muted).FontSize(9);
                var sp = t.Span(value).FontSize(9.5f);
                if (bold) sp.Bold();
            });

        var birth = data.PatientDateOfBirth.HasValue ? data.PatientDateOfBirth.Value.Year.ToString() : "—";
        var diagnosis = string.Join(" - ", new[] { data.DiagnosisName, data.DiagnosisCode }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        container.Border(1).BorderColor(LineColor).Background("#F8FAFC").Padding(7).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem(3).Element(c => Cell(c, "Họ tên", data.PatientFullName, true));
                r.RelativeItem(2).Element(c => Cell(c, "Năm sinh", birth));
            });
            col.Item().PaddingTop(2).Row(r =>
            {
                // PrescriptionPdfData chua co can nang / ma KH -> hien "—"
                r.RelativeItem(3).Element(c => Cell(c, "Giới · Cân nặng", GenderLabel(data.PatientGender) + " · —"));
                r.RelativeItem(2).Element(c => Cell(c, "Mã KH", "—"));
            });
            col.Item().PaddingTop(2).Element(c => Cell(c, "Chẩn đoán", string.IsNullOrEmpty(diagnosis) ? "—" : diagnosis));
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
                cols.ConstantColumn(24);
                cols.RelativeColumn(5);
                cols.RelativeColumn(1.8f);
                cols.ConstantColumn(30);
                cols.ConstantColumn(30);
                cols.ConstantColumn(30);
                cols.ConstantColumn(30);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HeadCell(h.Cell()).Text("STT").FontColor("#FFFFFF").Bold().FontSize(8);
                ReportPdfCommon.HeadCell(h.Cell()).Text("Tên thuốc, hàm lượng").FontColor("#FFFFFF").Bold().FontSize(8.5f);
                ReportPdfCommon.HeadCell(h.Cell()).AlignCenter().Text("Số lượng").FontColor("#FFFFFF").Bold().FontSize(8);
                foreach (var d in new[] { "Sáng", "Trưa", "Chiều", "Tối" })
                    ReportPdfCommon.HeadCell(h.Cell()).AlignCenter().Text(d).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            var i = 0;
            foreach (var it in items)
            {
                var drugLabel = string.IsNullOrWhiteSpace(it.Strength) ? it.DrugName : $"{it.DrugName} {it.Strength}";
                var qtyLabel = string.IsNullOrWhiteSpace(it.Unit) ? $"{it.Quantity:0.##}" : $"{it.Quantity:0.##} {it.Unit}";
                // Data khong tach sang/trua/chieu/toi -> gop lieu + tan suat vao dong cach dung, 4 cot de trong
                var usageParts = new List<string> { it.Dosage, it.Frequency, RouteLabel(it.Route) };
                if (it.DurationDays > 0)
                    usageParts.Add($"{it.DurationDays} ngày");
                if (!string.IsNullOrWhiteSpace(it.Instructions))
                    usageParts.Add(it.Instructions);
                var usageLabel = string.Join(", ", usageParts.Where(s => !string.IsNullOrWhiteSpace(s)));

                i++;
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(it.Stt.ToString()).FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Column(c =>
                {
                    c.Item().Text(drugLabel).SemiBold().FontSize(9.5f);
                    if (usageLabel.Length > 0)
                        c.Item().Text(usageLabel).FontSize(8).FontColor(Muted);
                });
                ReportPdfCommon.BodyCell(tbl.Cell(), i).AlignCenter().Text(qtyLabel).FontSize(9);
                for (var k = 0; k < 4; k++)
                    ReportPdfCommon.BodyCell(tbl.Cell(), i).Text("");
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

    private static void RenderSignature(IContainer container, PrescriptionPdfData data)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter().Text("Bác sĩ kê đơn").Bold().FontSize(9.5f);
                col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(Muted);
                col.Item().PaddingTop(24).AlignCenter().Text(data.DoctorFullName ?? "").SemiBold().FontSize(10);
            });
        });
    }
}
