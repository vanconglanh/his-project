namespace ProDiabHis.Application.Common;

/// <summary>
/// Sinh anh QR dang PNG tu mot chuoi noi dung bat ky (vd otpauth:// URL cho 2FA).
/// Tra ve mang byte PNG that (header 89 50 4E 47), KHONG phai base64 cua chuoi.
/// </summary>
public interface IQrCodeImageGenerator
{
    /// <summary>Sinh anh QR PNG tu <paramref name="content"/>.</summary>
    /// <param name="content">Noi dung ma hoa trong QR.</param>
    /// <param name="pixelsPerModule">Do lon moi o (pixel), mac dinh 5.</param>
    byte[] GeneratePng(string content, int pixelsPerModule = 5);
}
