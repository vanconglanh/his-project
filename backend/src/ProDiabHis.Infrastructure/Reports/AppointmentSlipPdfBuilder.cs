using ProDiabHis.Application.Appointments;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>Sinh PDF Giay hen tai kham (QuestPDF), kho A5, tai dung khung thuong hieu diaB.</summary>
public class AppointmentSlipPdfBuilder : IAppointmentSlipPdfBuilder
{
    static AppointmentSlipPdfBuilder()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Build(AppointmentSlipData data)
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

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "GIẤY HẸN TÁI KHÁM"));
                    col.Item().PaddingTop(3).AlignRight()
                        .Text($"Mã lịch hẹn: LH{data.AppointmentId:D6}")
                        .FontSize(8.5f).FontColor(ReportPdfCommon.Muted);

                    col.Item().PaddingTop(10).Border(1).BorderColor(ReportPdfCommon.LineColor)
                        .Background("#F8FAFC").Padding(10).Column(info =>
                    {
                        info.Item().Text(t =>
                        {
                            t.Span("Họ tên bệnh nhân: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(data.PatientName).Bold().FontSize(11);
                        });
                        if (!string.IsNullOrWhiteSpace(data.PatientPhone))
                            info.Item().PaddingTop(3).Text(t =>
                            {
                                t.Span("Điện thoại: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                                t.Span(data.PatientPhone).FontSize(9.5f);
                            });
                    });

                    col.Item().PaddingTop(14).AlignCenter().Column(dt =>
                    {
                        dt.Item().AlignCenter().Text("Thời gian tái khám").FontSize(9.5f).FontColor(ReportPdfCommon.Muted);
                        dt.Item().AlignCenter().Text($"{data.AppointmentAt:HH:mm} ngày {data.AppointmentAt:dd/MM/yyyy}")
                            .Bold().FontSize(18).FontColor(ReportPdfCommon.Brand);
                        dt.Item().AlignCenter().PaddingTop(2)
                            .Text($"Thời lượng dự kiến: {data.DurationMinutes} phút")
                            .FontSize(9).FontColor(ReportPdfCommon.Muted);
                    });

                    if (!string.IsNullOrWhiteSpace(data.DoctorName))
                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Span("Bác sĩ hẹn khám: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(data.DoctorName).SemiBold().FontSize(9.5f);
                        });

                    if (!string.IsNullOrWhiteSpace(data.Note))
                        col.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(9.5f);
                            t.Span(data.Note).FontSize(9.5f);
                        });

                    col.Item().PaddingTop(6).Text(t =>
                    {
                        t.Span("Trạng thái: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                        t.Span(StatusLabel(data.Status)).SemiBold().FontSize(9);
                    });

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().AlignCenter().Column(sig =>
                        {
                            sig.Item().AlignCenter().Text($"Ngày {DateTime.Now:dd} tháng {DateTime.Now:MM} năm {DateTime.Now:yyyy}")
                                .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
                            sig.Item().AlignCenter().Text("NHÂN VIÊN LẬP PHIẾU").Bold().FontSize(9.5f);
                            sig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8f).FontColor(ReportPdfCommon.Muted);
                        });
                    });

                    col.Item().PaddingTop(12).Text(
                        "Đề nghị bệnh nhân đến đúng ngày giờ hẹn. Trường hợp cần thay đổi lịch hẹn, vui lòng liên hệ phòng khám trước ít nhất 24 giờ.")
                        .Italic().FontSize(8.5f).FontColor(ReportPdfCommon.Muted);
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

    private static string StatusLabel(string status) => status switch
    {
        "PENDING" => "Chờ xác nhận",
        "CONFIRMED" => "Đã xác nhận",
        "CHECKED_IN" => "Đã đến khám",
        "CANCELLED" => "Đã hủy",
        "NO_SHOW" => "Không đến",
        _ => status
    };
}
