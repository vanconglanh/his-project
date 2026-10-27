using ProDiabHis.Application.CLS;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Sinh PDF Phieu chi dinh can lam sang (Xet nghiem / CDHA) bang QuestPDF, kho A4 doc,
/// dung khung thuong hieu tu ReportPdfCommon (letterhead tenant, barcode).
/// </summary>
public class ClsOrderSlipPdfBuilder : IClsOrderSlipPdfBuilder
{
    static ClsOrderSlipPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] BuildLabOrderSlip(ClsOrderSlipData data) => Build(data, "XÉT NGHIỆM");

    public byte[] BuildRadOrderSlip(ClsOrderSlipData data) => Build(data, "CHẨN ĐOÁN HÌNH ẢNH");

    private static byte[] Build(ClsOrderSlipData data, string groupTitle)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, data.Letterhead, null));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Element(c => RenderTitleBlock(c, data));
                    col.Item().PaddingTop(10).Element(c => RenderPatientInfo(c, data));
                    col.Item().PaddingTop(10).Element(c => RenderItemsTable(c, data.Items, groupTitle));
                    col.Item().PaddingTop(6).Text(t =>
                    {
                        t.Span("Tổng số dịch vụ chỉ định: ").FontColor(ReportPdfCommon.Muted);
                        t.Span(data.Items.Count.ToString()).Bold();
                    });
                    col.Item().PaddingTop(14).Element(c => RenderSignature(c, data));
                    col.Item().PaddingTop(14).Element(RenderNotes);
                });

                page.Footer().Column(f =>
                {
                    f.Item().LineHorizontal(0.75f).LineColor(ReportPdfCommon.LineColor);
                    f.Item().PaddingTop(3).Row(r =>
                    {
                        var lh = data.Letterhead;
                        var email = lh.EmailSupport ?? lh.Email;
                        var parts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(email)) parts.Add($"Email: {email}");
                        if (!string.IsNullOrWhiteSpace(lh.Phone)) parts.Add($"Điện thoại: {lh.Phone}");
                        r.RelativeItem().Text(string.Join("   |   ", parts)).FontSize(8).FontColor(ReportPdfCommon.Muted);
                        r.AutoItem().Text(txt =>
                        {
                            txt.Span("Trang ").FontSize(8).FontColor(ReportPdfCommon.Muted);
                            txt.CurrentPageNumber().FontSize(8).FontColor(ReportPdfCommon.Muted);
                            txt.Span("/").FontSize(8).FontColor(ReportPdfCommon.Muted);
                            txt.TotalPages().FontSize(8).FontColor(ReportPdfCommon.Muted);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void RenderTitleBlock(IContainer container, ClsOrderSlipData data)
    {
        container.Row(row =>
        {
            row.RelativeItem(1).Text("");
            row.RelativeItem(3).Column(col =>
            {
                col.Item().Element(c => ReportPdfCommon.RenderTitle(c, data.DocTitle.ToUpperInvariant()));
                col.Item().AlignCenter().Text(t =>
                {
                    t.Span("Mã phiếu chỉ định: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(data.SlipCode).Bold().FontSize(10);
                });
                if (!string.IsNullOrWhiteSpace(data.SlipCode))
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, data.SlipCode));
            });
            row.RelativeItem(1).PaddingTop(14).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(t =>
                {
                    t.Span("Số thứ tự: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span("—").FontSize(9);
                });
                col.Item().AlignRight().Text(t =>
                {
                    t.Span("Ngày chỉ định: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span($"{data.IssuedAt:dd/MM/yyyy HH:mm}").SemiBold().FontSize(9);
                });
            });
        });
    }

    private static void KV(IContainer c, string label, string? value, bool bold = false)
    {
        c.Border(0.5f).BorderColor(ReportPdfCommon.LineColor).Padding(5).Text(t =>
        {
            t.Span(label + ": ").FontColor(ReportPdfCommon.Muted).FontSize(9);
            var sp = t.Span(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(9.5f);
            if (bold) sp.Bold();
        });
    }

    private static void RenderPatientInfo(IContainer container, ClsOrderSlipData data)
    {
        var dobGender = $"{(data.PatientDob.HasValue ? data.PatientDob.Value.ToString("dd/MM/yyyy") : "—")} / {GenderLabel(data.PatientGender)}";
        var diag = string.Join(" – ", new[] { data.DiagnosisCode, data.DiagnosisName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        container.Background("#F8FAFC").Table(tbl =>
        {
            tbl.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
            KV(tbl.Cell(), "Họ và tên", data.PatientFullName, true);
            KV(tbl.Cell(), "Mã KH", data.PatientCode);
            KV(tbl.Cell(), "Ngày sinh / Giới tính", dobGender);
            KV(tbl.Cell(), "Điện thoại", null);
            KV(tbl.Cell().ColumnSpan(2), "Địa chỉ", null);
            KV(tbl.Cell(), "Phòng khám", null);
            KV(tbl.Cell(), "Bác sĩ chỉ định", data.DoctorFullName);
            KV(tbl.Cell().ColumnSpan(2), "Chẩn đoán", diag);
        });
    }

    private static void RenderItemsTable(IContainer container, IReadOnlyList<ClsOrderSlipItemDto> items, string groupTitle)
    {
        container.Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(32);
                cols.RelativeColumn(5);
                cols.ConstantColumn(34);
                cols.RelativeColumn(4);
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "STT");
                ReportPdfCommon.HText(h.Cell(), "Dịch vụ chỉ định");
                ReportPdfCommon.HText(h.Cell(), "SL");
                ReportPdfCommon.HText(h.Cell(), "Chuẩn bị trước khi làm");
            });

            tbl.Cell().ColumnSpan(4).Background(ReportPdfCommon.TintTeal).PaddingVertical(5).PaddingHorizontal(6)
                .Text(groupTitle).Bold().FontColor(ReportPdfCommon.Brand).FontSize(10);

            var i = 0;
            foreach (var it in items)
            {
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(it.Stt.ToString()).FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(t =>
                {
                    t.Line(it.Name).SemiBold().FontSize(9.5f);
                    if (!string.IsNullOrWhiteSpace(it.Detail)) t.Line(it.Detail).FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                    if (it.Priority is "URGENT" or "STAT")
                        t.Line($"Ưu tiên: {PriorityLabel(it.Priority)}").FontSize(8.5f).Bold().FontColor("#B91C1C");
                });
                ReportPdfCommon.BodyCell(tbl.Cell(), i).AlignCenter().Text("1").FontSize(9);
                ReportPdfCommon.BodyCell(tbl.Cell(), i).Text(it.Note ?? "").FontSize(8.5f);
                i++;
            }
        });
    }

    private static void RenderSignature(IContainer container, ClsOrderSlipData data)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("NGƯỜI BỆNH / NGƯỜI NHÀ").Bold().FontSize(9.5f);
                col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                col.Item().Height(60);
            });
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text("BÁC SĨ CHỈ ĐỊNH").Bold().FontSize(9.5f);
                col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                col.Item().Height(36);
                col.Item().AlignCenter().Text(data.DoctorFullName ?? "").SemiBold().FontSize(10);
            });
        });
    }

    private static void RenderNotes(IContainer container)
    {
        container.Border(0.5f).BorderColor(ReportPdfCommon.LineColor).Padding(8).Column(col =>
        {
            col.Item().Text("Lưu ý").Bold().FontSize(9.5f).FontColor(ReportPdfCommon.Brand);
            foreach (var n in new[]
            {
                "Chỉ định có thể thay đổi theo tình trạng thực tế của người bệnh.",
                "Vui lòng mang theo phiếu chỉ định và các giấy tờ liên quan khi thực hiện dịch vụ.",
                "Cần hỗ trợ, vui lòng liên hệ nhân viên phòng khám."
            })
                col.Item().PaddingTop(2).Text($"•  {n}").FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
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
}
