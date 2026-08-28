namespace ProDiabHis.Application.Common;

/// <summary>
/// Interface chuẩn hoá cho luồng ký số remote-signing (VNPT SmartCA / Viettel-CA style),
/// độc lập nhà cung cấp CA. Theo mục 5.1 SRS — ký số bệnh án / đơn thuốc.
/// Implementation cụ thể: MockDigitalSignatureProvider (dev/test) hoặc
/// VnptSmartCaSignatureProvider (production, khi có hợp đồng/sandbox thật).
/// </summary>
public interface IDigitalSignatureProvider
{
    /// <summary>
    /// Lấy thông tin chứng thư số (certificate) đã đăng ký của bác sĩ/nhân sự.
    /// </summary>
    Task<SignCertificateInfo> GetCertificateAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Ký lên hash của tài liệu (bệnh án / đơn thuốc). Provider thật sẽ gọi API remote-signing
    /// của CA (OTP/PIN xác nhận trên app SmartCA...), trả về chữ ký số + timestamp + serial.
    /// </summary>
    Task<SignResult> SignDocumentHashAsync(
        string userId,
        byte[] documentHash,
        string documentType,
        string documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Xác thực lại chữ ký đã ký trên hash tài liệu — phục vụ audit / thanh kiểm tra (mục 5.1 SRS).
    /// </summary>
    Task<VerifyResult> VerifySignatureAsync(
        byte[] documentHash,
        byte[] signature,
        string? certificateSerial,
        CancellationToken ct = default);
}

/// <summary>Thông tin chứng thư số (certificate) của người ký.</summary>
public record SignCertificateInfo(
    string Serial,
    string Issuer,
    DateTime ValidFrom,
    DateTime ValidTo,
    string SubjectName,
    bool IsActive = true);

/// <summary>Kết quả ký số lên hash tài liệu.</summary>
public record SignResult(
    bool Success,
    byte[]? Signature,
    DateTime? Timestamp,
    string? CertificateSerial,
    string? ErrorCode = null,
    string? ErrorMessage = null);

/// <summary>Kết quả xác thực chữ ký số.</summary>
public record VerifyResult(
    bool IsValid,
    string? Reason,
    string? CertificateSerial = null,
    string? CertificateSubject = null,
    string? Algorithm = null);
