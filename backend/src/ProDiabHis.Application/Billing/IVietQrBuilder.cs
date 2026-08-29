namespace ProDiabHis.Application.Billing;

public record VietQrBuildResult(string QrPayloadBase64, string QrPayloadString, string? QrUrl);

/// <summary>
/// Build chuoi QR thanh toan chuan VietQR (EMVCo) VOI SO TIEN DONG theo tai khoan cau hinh
/// cua tenant (FR-911 H-9). Tach interface khoi Infrastructure de Application khong phu thuoc
/// truc tiep thu vien QRCoder.
/// </summary>
public interface IVietQrBuilder
{
    VietQrBuildResult Build(decimal amount, string addInfo, string bankBin, string accountNo, string accountName);
}
