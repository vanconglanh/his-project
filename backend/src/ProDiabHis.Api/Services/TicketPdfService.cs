using ProDiabHis.Application.Reception;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>QuestPDF implementation tao PDF phieu tiep don kho A5, theo chuan format chung.</summary>
public class TicketPdfService : ITicketPdfService
{
    static TicketPdfService()
    {
        // QuestPDF community license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateTicketPdfAsync(ReceptionTicketResponse ticket, CancellationToken ct = default, LetterheadDto? letterhead = null)
    {
        var lh = letterhead ?? new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(8, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginHorizontal(9, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(ReportPdfCommon.Ink));

                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU TIẾP ĐÓN"));

                    // So thu tu — in to nhu block gio hen AppointmentSlip
                    col.Item().PaddingTop(10).AlignCenter().Column(dt =>
                    {
                        dt.Item().AlignCenter().Text("Số thứ tự").FontSize(9.5f).FontColor(ReportPdfCommon.Muted);
                        dt.Item().AlignCenter().Text(ticket.TicketNo)
                            .Bold().FontSize(46).FontColor(ReportPdfCommon.Brand);
                    });

                    col.Item().PaddingTop(12).Border(1).BorderColor(ReportPdfCommon.LineColor)
                        .Background("#F8FAFC").Padding(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Row(string label, string? value)
                        {
                            table.Cell().PaddingBottom(3).Text(label + ":").FontColor(ReportPdfCommon.Muted).SemiBold().FontSize(9.5f);
                            table.Cell().PaddingBottom(3).Text(value ?? "—").FontSize(9.5f);
                        }

                        Row("Mã BN", ticket.PatientSummary?.Code);
                        Row("Họ tên", ticket.PatientSummary?.FullName);
                        Row("Phòng", ticket.RoomName);
                        Row("Bác sĩ", ticket.DoctorName);
                        Row("Ưu tiên", ticket.Priority);
                        Row("Ngày", ticket.CheckedInAt.ToString("dd/MM/yyyy HH:mm"));
                        if (!string.IsNullOrEmpty(ticket.ReasonForVisit))
                            Row("Lý do khám", ticket.ReasonForVisit);
                    });

                    col.Item().PaddingTop(10).AlignCenter().Text(
                        $"Mã phiếu: {ticket.Id}").FontSize(8).FontColor(ReportPdfCommon.Muted);
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.75f).LineColor(ReportPdfCommon.LineColor);
                    col.Item().PaddingTop(3).AlignCenter().Text(
                        "Vui lòng giữ phiếu để theo dõi thứ tự. Xin cảm ơn!")
                        .FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
