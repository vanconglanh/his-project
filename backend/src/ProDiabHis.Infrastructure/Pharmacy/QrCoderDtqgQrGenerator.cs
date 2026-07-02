using ProDiabHis.Application.Pharmacy;
using QRCoder;

namespace ProDiabHis.Infrastructure.Pharmacy;

public class QrCoderDtqgQrGenerator : IDtqgQrGenerator
{
    public byte[] GenerateQrPng(string maDonThuoc, string portalUrl)
    {
        var payload = $"{portalUrl}?ma={maDonThuoc}";
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(5);
    }
}
