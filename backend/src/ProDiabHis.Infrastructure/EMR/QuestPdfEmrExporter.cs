using ProDiabHis.Application.EMR;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.EMR;

/// <summary>
/// Export EMR (Phieu kham benh) as PDF using QuestPDF. Nang cap: tai dung khung thuong hieu
/// diaB (letterhead, footer chu ky) tu ReportPdfCommon thay vi header cung "PHONG KHAM PRO-DIAB HIS".
/// </summary>
public class QuestPdfEmrExporter : IEmrPdfExporter
{
    static QuestPdfEmrExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportAsync(EmrPdfContext context, CancellationToken ct = default)
    {
        var letterhead = context.Letterhead ?? new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, letterhead, null));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU KHÁM BỆNH"));
                    col.Item().AlignCenter().Text("(Bệnh án ngoại trú)").Italic().FontSize(9).FontColor(ReportPdfCommon.Muted);
                    col.Item().PaddingTop(3).AlignRight().Text(
                        $"Mã lượt khám: {(string.IsNullOrWhiteSpace(context.EncounterNo) ? context.EncounterId.ToString("N")[..8].ToUpper() : context.EncounterNo)}  •  {context.EncounterDate:HH:mm dd/MM/yyyy}")
                        .FontSize(8.5f).FontColor(ReportPdfCommon.Muted);

                    // I. Hanh chinh
                    col.Item().PaddingTop(10).Element(c => SectionHeader(c, "I. Hành chính"));
                    col.Item().PaddingTop(3).Element(c => RenderAdminInfo(c, context));

                    // II. Ly do kham
                    if (!string.IsNullOrWhiteSpace(context.ReasonForVisit) || !string.IsNullOrWhiteSpace(context.ChiefComplaint))
                    {
                        col.Item().PaddingTop(8).Element(c => SectionHeader(c, "II. Lý do khám"));
                        col.Item().PaddingTop(2).Text(context.ChiefComplaint ?? context.ReasonForVisit ?? "").FontSize(9.5f);
                    }

                    // III. Kham benh - sinh hieu
                    if (context.Vitals is not null)
                    {
                        col.Item().PaddingTop(8).Element(c => SectionHeader(c, "III. Khám bệnh — Sinh hiệu"));
                        col.Item().PaddingTop(3).Element(c => RenderVitalsTable(c, context.Vitals));
                    }

                    // IV. Chan doan
                    if (context.PrimaryDiagnosis is not null || (context.SecondaryDiagnoses?.Count ?? 0) > 0)
                    {
                        col.Item().PaddingTop(8).Element(c => SectionHeader(c, "IV. Chẩn đoán"));
                        if (context.PrimaryDiagnosis is not null)
                            col.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("Bệnh chính: ").Bold().FontSize(9.5f);
                                t.Span($"{context.PrimaryDiagnosis.Icd10Code} – {context.PrimaryDiagnosis.Name}").FontSize(9.5f);
                            });
                        if (context.SecondaryDiagnoses is { Count: > 0 })
                            col.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("Bệnh kèm theo: ").Bold().FontSize(9.5f);
                                t.Span(string.Join("; ", context.SecondaryDiagnoses.Select(d => $"{d.Icd10Code} – {d.Name}"))).FontSize(9.5f);
                            });
                    }

                    // V. Noi dung kham / xu tri (tu content_html cua EMR)
                    col.Item().PaddingTop(8).Element(c => SectionHeader(c, "V. Nội dung khám — Xử trí"));
                    var plainText = StripHtml(context.ContentHtml);
                    col.Item().PaddingTop(2).Text(plainText).FontSize(9.5f);

                    if (context.IsSigned)
                    {
                        col.Item().PaddingTop(14).LineHorizontal(0.5f).LineColor(ReportPdfCommon.LineColor);
                        col.Item().PaddingTop(6).Column(sig =>
                        {
                            sig.Item().Text("CHỮ KÝ SỐ").Bold().FontSize(9.5f);
                            if (context.SignedAt.HasValue)
                                sig.Item().Text($"Ký lúc: {context.SignedAt.Value:dd/MM/yyyy HH:mm}").FontSize(9);
                            if (context.SignerName is not null)
                                sig.Item().Text($"Người ký: {context.SignerName}").FontSize(9);
                            if (context.CertSerial is not null)
                                sig.Item().Text($"Serial chứng thư: {context.CertSerial}").FontSize(8).FontColor(ReportPdfCommon.Muted);
                        });
                    }
                    else
                    {
                        col.Item().PaddingTop(16).Element(c => RenderSignatureBlock(c, context));
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Trang ").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    text.CurrentPageNumber().FontSize(8).FontColor(ReportPdfCommon.Muted);
                    text.Span(" / ").FontSize(8).FontColor(ReportPdfCommon.Muted);
                    text.TotalPages().FontSize(8).FontColor(ReportPdfCommon.Muted);
                });
            });
        });

        var bytes = doc.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static void SectionHeader(IContainer container, string text) =>
        container.Text(text).Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Brand);

    private static void RenderAdminInfo(IContainer container, EmrPdfContext ctx)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Text(t =>
                {
                    t.Span("Họ tên: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(ctx.PatientName).Bold().FontSize(10);
                });
                row.RelativeItem(1).Text(t =>
                {
                    t.Span("Giới tính: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(GenderLabel(ctx.PatientGender)).FontSize(9);
                });
            });
            col.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem(2).Text(t =>
                {
                    t.Span("Năm sinh: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(ctx.PatientDob.HasValue ? FormatBirthYearAndAge(ctx.PatientDob.Value) : "—").FontSize(9);
                });
                row.RelativeItem(1).Text(t =>
                {
                    t.Span("Mã BN: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(string.IsNullOrWhiteSpace(ctx.PatientCode) ? "—" : ctx.PatientCode).FontSize(9);
                });
            });
            if (!string.IsNullOrWhiteSpace(ctx.PatientAddress))
                col.Item().PaddingTop(2).Text(t =>
                {
                    t.Span("Địa chỉ: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(ctx.PatientAddress).FontSize(9);
                });
            if (!string.IsNullOrWhiteSpace(ctx.DoctorName))
                col.Item().PaddingTop(2).Text(t =>
                {
                    t.Span("Bác sĩ khám: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                    t.Span(ctx.DoctorName).FontSize(9);
                });
        });
    }

    private static void RenderVitalsTable(IContainer container, EmrVitalsDto v)
    {
        container.Column(outer =>
        {
        outer.Item().Table(tbl =>
        {
            tbl.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < 7; i++) cols.RelativeColumn();
            });

            tbl.Header(h =>
            {
                ReportPdfCommon.HText(h.Cell(), "Mạch (l/ph)");
                ReportPdfCommon.HText(h.Cell(), "Nhiệt độ (°C)");
                ReportPdfCommon.HText(h.Cell(), "Huyết áp (mmHg)");
                ReportPdfCommon.HText(h.Cell(), "Nhịp thở (l/ph)");
                ReportPdfCommon.HText(h.Cell(), "SpO2 (%)");
                ReportPdfCommon.HText(h.Cell(), "Cân nặng (kg)");
                ReportPdfCommon.HText(h.Cell(), "Chiều cao (cm)");
            });

            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.HeartRateBpm?.ToString() ?? "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.TemperatureC?.ToString("0.0") ?? "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter()
                .Text(v.BpSystolic.HasValue && v.BpDiastolic.HasValue ? $"{v.BpSystolic}/{v.BpDiastolic}" : "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.RespiratoryRate?.ToString() ?? "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.Spo2Percent?.ToString() ?? "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.WeightKg?.ToString("0.##") ?? "—").FontSize(9);
            ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(v.HeightCm?.ToString("0.#") ?? "—").FontSize(9);
        });

        if (v.GlucoseMgDl.HasValue)
        {
            outer.Item().PaddingTop(3).Text(t =>
            {
                t.Span("Glucose mao mạch: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                t.Span($"{v.GlucoseMgDl.Value:0.##} mg/dL").SemiBold().FontSize(9);
            });
        }
        });
    }

    private static void RenderSignatureBlock(IContainer container, EmrPdfContext ctx)
    {
        container.Row(row =>
        {
            row.RelativeItem();
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Text($"Ngày {ctx.EncounterDate:dd} tháng {ctx.EncounterDate:MM} năm {ctx.EncounterDate:yyyy}")
                    .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                col.Item().AlignCenter().Text("BÁC SĨ KHÁM BỆNH").Bold().FontSize(9.5f);
                col.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                col.Item().PaddingTop(22).AlignCenter().Text(ctx.DoctorName ?? "").SemiBold().FontSize(10);
            });
        });
    }

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

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
    }
}
