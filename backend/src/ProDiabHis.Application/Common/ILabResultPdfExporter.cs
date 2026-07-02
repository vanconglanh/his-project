using ProDiabHis.Domain.Entities;

namespace ProDiabHis.Application.Common;

/// <summary>Xuat phieu ket qua XN/CDHA sang PDF (QuestPDF)</summary>
public interface ILabResultPdfExporter
{
    Task<byte[]> ExportLabResultAsync(LabResult entity, CancellationToken ct = default);
    Task<byte[]> ExportRadResultAsync(dynamic row, CancellationToken ct = default);
}
