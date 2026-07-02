using ProDiabHis.Application.Reception;

namespace ProDiabHis.Api.Services;

/// <summary>Tao PDF phieu tiep don benh nhan</summary>
public interface ITicketPdfService
{
    Task<byte[]> GenerateTicketPdfAsync(ReceptionTicketResponse ticket, CancellationToken ct = default);
}
