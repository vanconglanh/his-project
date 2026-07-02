using ProDiabHis.Application.Reception;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Api.Services;

/// <summary>QuestPDF implementation tao PDF phieu tiep don A6</summary>
public class TicketPdfService : ITicketPdfService
{
    static TicketPdfService()
    {
        // QuestPDF community license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateTicketPdfAsync(ReceptionTicketResponse ticket, CancellationToken ct = default)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(15, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("PRO-DIAB HIS").Bold().FontSize(14);
                    col.Item().AlignCenter().Text("PHIẾU TIẾP ĐÓN BỆNH NHÂN").Bold().FontSize(11);
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(5).Column(col =>
                {
                    // Ticket number - large
                    col.Item().AlignCenter().Text(ticket.TicketNo)
                        .Bold().FontSize(60).FontColor(Colors.Blue.Darken2);

                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void Row(string label, string? value)
                        {
                            table.Cell().Text(label + ":").Bold();
                            table.Cell().Text(value ?? "-");
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

                    // QR code placeholder (URL to queue position)
                    col.Item().PaddingTop(10).AlignCenter().Text(
                        $"Mã phiếu: {ticket.Id}").FontSize(7).FontColor(Colors.Grey.Darken2);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Vui lòng giữ phiếu để theo dõi thứ tự. Xin cảm ơn!").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
