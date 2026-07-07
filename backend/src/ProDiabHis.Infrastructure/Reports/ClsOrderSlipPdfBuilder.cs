using ProDiabHis.Application.CLS;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh PDF Phieu chi dinh Xet nghiem / CDHA (QuestPDF), kho A5, tai dung khung thuong hieu
/// diaB tu ReportPdfCommon (letterhead, barcode, footer chu ky).
/// </summary>
public class ClsOrderSlipPdfBuilder : IClsOrderSlipPdfBuilder
{
    static ClsOrderSlipPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] BuildLabOrderSlip(ClsOrderSlipData data) => Build(data);

    public byte[] BuildRadOrderSlip(ClsOrderSlipData data) => Build(data);

    private static byte[] Build(ClsOrderSlipData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(8, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginHorizontal(9, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(ReportPdfCommon.Ink));

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, data.Letterhead, null));

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, data.DocTitle));
                    col.Item().AlignCenter().Text(data.DocSubtitle).Italic().FontSize(9).FontColor(ReportPdfCommon.Muted);
                    col.Item().PaddingTop(3).AlignRight().Text($"Mã phiếu: {data.SlipCode}  •  {data.IssuedAt:HH:mm dd/MM/yyyy}")
                        .FontSize(8.5f).FontColor(ReportPdfCommon.Muted);

                    col.Item().PaddingTop(8).Element(c => RenderPatientInfo(c, data));
                    col.Item().PaddingTop(10).Element(c => RenderItemsTable(c, data.Items));
                    col.Item().PaddingTop(14).Element(c => RenderSignature(c, data));
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Trang ").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.CurrentPageNumber().FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.Span("/").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    txt.TotalPages().FontSize(8).FontColor(ReportPdfCommon.Muted);
                });
            });
        }).GeneratePdf();
    }

    private static void RenderPatientInfo(IContainer container, ClsOrderSlipData data)
    {
        container.Border(1).BorderColor(ReportPdfCommon.LineColor).Background("#F8FAFC").Padding(8).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(t =>
                {
                    t.Span("Họ tên: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(data.PatientFullName).Bold().FontSize(10);
                });
                row.RelativeItem(1).Text(t =>
                {
                    t.Span("Giới tính: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(GenderLabel(data.PatientGender)).FontSize(9);
                });
            });
            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem(2).Text(t =>
                {
                    t.Span("Mã BN: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(string.IsNullOrWhiteSpace(data.PatientCode) ? "—" : data.PatientCode).FontSize(9);
                });
                row.RelativeItem(1).Text(t =>
                {
                    t.Span("Năm sinh: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(data.PatientDob.HasValue ? FormatBirthYearAndAge(data.PatientDob.Value) : "—").FontSize(9);
                });
            });
            if (!string.IsNullOrWhiteSpace(data.DiagnosisCode) || !string.IsNullOrWhiteSpace(data.DiagnosisName))
            {
                col.Item().PaddingTop(4).Text(t =>
                {
                    t.Span("Chẩn đoán sơ bộ: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    var text = string.Join(" – ", new[] { data.DiagnosisCode, data.DiagnosisName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    t.Span(text).FontColor(ReportPdfCommon.Brand).SemiBold().FontSize(9);
                });
            }
        });
    }

    private static void RenderItemsTable(IContainer container, IReadOnlyList<ClsOrderSlipItemDto> items)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28);
                cols.RelativeColumn(4);
                cols.RelativeColumn(4);
                cols.RelativeColumn(1.6f);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "STT");
                ReportPdfCommon.HText(h.Cell(), "Tên chỉ định");
                ReportPdfCommon.HText(h.Cell(), "Chi tiết");
                ReportPdfCommon.HText(h.Cell(), "Ưu tiên");
            });

            var i = 0;
            foreach (var it in items)
            {
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(it.Stt.ToString()).FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(t => t.Span(it.Name).SemiBold().FontSize(9.5f));
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(txt =>
                {
                    if (!string.IsNullOrWhiteSpace(it.Detail)) txt.Line(it.Detail).FontSize(8.5f);
                    if (!string.IsNullOrWhiteSpace(it.Note)) txt.Line($"Ghi chú: {it.Note}").FontSize(8.5f).FontColor(ReportPdfCommon.Muted).Italic();
                });
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(PriorityLabel(it.Priority)).FontSize(9);
                i++;
            }
        });
    }

    private static void RenderSignature(IContainer container, ClsOrderSlipData data)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text($"Ngày {data.IssuedAt:dd} tháng {data.IssuedAt:MM} năm {data.IssuedAt:yyyy}")
                    .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                col.Item().AlignCenter().Text("BÁC SĨ CHỈ ĐỊNH").Bold().FontSize(9.5f);
                col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                col.Item().PaddingTop(22).AlignCenter().Text(data.DoctorFullName ?? "").SemiBold().FontSize(10);
            });
        });
    }

    private static string PriorityLabel(string priority) => priority switch
    {
        "URGENT" => "Khẩn",
        "STAT" => "Cấp cứu",
        _ => "Thường"
    };

    private static string GenderLabel(string? gender) => gender switch
    {
        "MALE" => "Nam",
        "FEMALE" => "Nữ",
        "OTHER" => "Khác",
        _ => "—"
    };

    private static string FormatBirthYearAndAge(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return $"{dob.Year} ({age} tuổi)";
    }
}
