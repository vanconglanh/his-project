using ProDiabHis.Application.Common;
using QRCoder;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>Impl QR PNG bang thu vien QRCoder (da co san trong project).</summary>
public class QrCoderImageGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 5)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(pixelsPerModule);
    }
}
