using ProDiabHis.Application.Reception;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF phieu tiep don benh nhan, kho A5, dung chuan letterhead teal chung he thong</summary>
public interface ITicketPdfService
{
    Task<byte[]> GenerateTicketPdfAsync(ReceptionTicketResponse ticket, CancellationToken ct = default, LetterheadDto? letterhead = null);
}
