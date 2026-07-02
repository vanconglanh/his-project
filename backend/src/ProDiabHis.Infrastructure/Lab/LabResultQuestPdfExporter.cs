using ProDiabHis.Application.Common;
using ProDiabHis.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>QuestPDF implementation cho ILabResultPdfExporter</summary>
public class LabResultQuestPdfExporter : ILabResultPdfExporter
{
    static LabResultQuestPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportLabResultAsync(LabResult entity, CancellationToken ct = default)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30, Unit.Point);
                page.Content().Column(col =>
                {
                    col.Item().Text("PHIẾU KẾT QUẢ XÉT NGHIỆM").Bold().FontSize(16);
                    col.Item().Text($"Mã XN: {entity.TestCode} — {entity.TestName}");
                    col.Item().Text($"Giá trị: {entity.Value} {entity.Unit ?? ""}");
                    col.Item().Text($"Flag: {entity.Flag}");
                    col.Item().Text($"Phương pháp: {entity.Method ?? "-"}");
                    col.Item().Text($"Trạng thái: {entity.Status}");
                    col.Item().Text($"Ngày thực hiện: {entity.PerformedAt:dd/MM/yyyy HH:mm}");
                    if (entity.VerifiedAt.HasValue)
                        col.Item().Text($"Xác thực lúc: {entity.VerifiedAt.Value:dd/MM/yyyy HH:mm}");
                    if (entity.Note is not null)
                        col.Item().Text($"Ghi chú: {entity.Note}");
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }

    public Task<byte[]> ExportRadResultAsync(dynamic row, CancellationToken ct = default)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30, Unit.Point);
                page.Content().Column(col =>
                {
                    col.Item().Text("PHIẾU KẾT QUẢ CHẨN ĐOÁN HÌNH ẢNH").Bold().FontSize(16);
                    col.Item().Text($"Phương thức: {(string)row.modality}");
                    col.Item().Text($"Mô tả hình ảnh: {(string)row.findings}");
                    if (row.impression != null) col.Item().Text($"Ấn tượng: {(string)row.impression}");
                    col.Item().Text($"Kết luận: {(string)row.conclusion}");
                    if (row.recommendations != null) col.Item().Text($"Khuyến nghị: {(string)row.recommendations}");
                    col.Item().Text($"Ngày thực hiện: {((DateTime)row.performed_at):dd/MM/yyyy HH:mm}");
                    if (row.verified_at != null)
                        col.Item().Text($"Ký phát hành lúc: {((DateTime)row.verified_at):dd/MM/yyyy HH:mm}");
                });
            });
        }).GeneratePdf();

        return Task.FromResult(pdf);
    }
}
