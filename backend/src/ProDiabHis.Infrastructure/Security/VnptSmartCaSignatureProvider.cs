using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Security;

/// <summary>
/// Cấu hình kết nối VNPT SmartCA remote-signing API.
/// Đọc từ appsettings: "SignatureProvider:VnptSmartCa:*".
/// TODO: xác nhận endpoint chính xác với VNPT khi có hợp đồng/sandbox.
/// </summary>
public class VnptSmartCaOptions
{
    /// <summary>Base URL của gateway VNPT SmartCA (khi ký hợp đồng sẽ được cấp cụ thể).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>API key / client id cấp cho tenant khi đăng ký dịch vụ SmartCA.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Secret key dùng để ký request (HMAC) hoặc client_secret cho OAuth2, tuỳ chuẩn VNPT công bố.</summary>
    public string? SecretKey { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Adapter gọi API remote-signing của VNPT SmartCA (theo tài liệu công khai phổ biến của SmartCA:
/// đăng ký thuê bao → lấy chứng thư số → gửi yêu cầu ký (kèm hash tài liệu) → thuê bao xác nhận OTP/PIN
/// trên app SmartCA → nhận chữ ký số trả về).
///
/// CHƯA có sandbox/tài khoản CA thật tại thời điểm implement — code chỉ dựng đúng SHAPE
/// (request/response DTO, luồng gọi HttpClient) theo pattern remote-signing chuẩn, KHÔNG bịa
/// endpoint cụ thể. Mọi endpoint path dưới đây là placeholder và PHẢI được xác nhận lại với VNPT
/// trước khi go-live (xem TODO rải rác trong file).
///
/// Nếu BaseUrl chưa được cấu hình (SignatureProvider:VnptSmartCa:BaseUrl), mọi lời gọi sẽ throw
/// NotImplementedException để tránh gọi nhầm ra ngoài khi chưa sẵn sàng.
/// </summary>
public class VnptSmartCaSignatureProvider : IDigitalSignatureProvider
{
    public const string HttpClientName = "VnptSmartCaClient";

    private readonly HttpClient _httpClient;
    private readonly VnptSmartCaOptions _options;
    private readonly ILogger<VnptSmartCaSignatureProvider> _logger;

    public VnptSmartCaSignatureProvider(
        HttpClient httpClient, VnptSmartCaOptions options, ILogger<VnptSmartCaSignatureProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new NotImplementedException(
                "VnptSmartCaSignatureProvider chua duoc cau hinh (SignatureProvider:VnptSmartCa:BaseUrl trong "
                + "appsettings). Day la adapter cho CA that (VNPT SmartCA) - chua co sandbox/hop dong nen chua "
                + "the goi API thuc te. Neu dang o Development/Testing, hay dat SignatureProvider:Type=Mock.");
        }
    }

    public async Task<SignCertificateInfo> GetCertificateAsync(string userId, CancellationToken ct = default)
    {
        EnsureConfigured();

        // TODO: xac nhan endpoint chinh xac voi VNPT khi co hop dong/sandbox.
        // Pattern tham khao chung cua remote-signing CA: GET /credentials?user_id=...
        var path = "/api/v1/credentials";
        _logger.LogInformation("[VNPT_SMARTCA] GetCertificateAsync userId={UserId} path={Path}", userId, path);

        using var response = await _httpClient.GetAsync($"{path}?user_id={Uri.EscapeDataString(userId)}", ct);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<VnptCertificateResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("VNPT SmartCA tra ve response rong khi lay chung thu so.");

        return new SignCertificateInfo(
            Serial: dto.Serial,
            Issuer: dto.Issuer,
            ValidFrom: dto.ValidFrom,
            ValidTo: dto.ValidTo,
            SubjectName: dto.SubjectName,
            IsActive: dto.IsActive);
    }

    public async Task<SignResult> SignDocumentHashAsync(
        string userId, byte[] documentHash, string documentType, string documentId, CancellationToken ct = default)
    {
        EnsureConfigured();

        // TODO: xac nhan endpoint chinh xac voi VNPT khi co hop dong/sandbox.
        // Pattern remote-signing chuan (SmartCA style): POST /api/v1/signatures/sign
        // body: { user_id, hash (base64), hash_alg, transaction_desc, document_type, document_id }
        // Ket qua tra ve co the la synchronous (co OTP tu app) hoac transaction_id de poll trang thai.
        var path = "/api/v1/signatures/sign";
        var request = new VnptSignRequestDto(
            UserId: userId,
            HashBase64: Convert.ToBase64String(documentHash),
            HashAlg: "SHA-256",
            DocumentType: documentType,
            DocumentId: documentId);

        _logger.LogInformation(
            "[VNPT_SMARTCA] SignDocumentHashAsync userId={UserId} documentType={DocumentType} documentId={DocumentId} path={Path}",
            userId, documentType, documentId, path);

        using var response = await _httpClient.PostAsJsonAsync(path, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[VNPT_SMARTCA] Sign that bai. Status={Status} Body={Body}", response.StatusCode, body);
            return new SignResult(false, null, null, null, "VNPT_SMARTCA_ERROR", $"VNPT SmartCA tra ve loi: {response.StatusCode}");
        }

        var dto = await response.Content.ReadFromJsonAsync<VnptSignResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("VNPT SmartCA tra ve response rong khi ky.");

        return new SignResult(
            Success: dto.Success,
            Signature: string.IsNullOrEmpty(dto.SignatureBase64) ? null : Convert.FromBase64String(dto.SignatureBase64),
            Timestamp: dto.Timestamp,
            CertificateSerial: dto.CertificateSerial,
            ErrorCode: dto.ErrorCode,
            ErrorMessage: dto.ErrorMessage);
    }

    public async Task<VerifyResult> VerifySignatureAsync(
        byte[] documentHash, byte[] signature, string? certificateSerial, CancellationToken ct = default)
    {
        EnsureConfigured();

        // TODO: xac nhan endpoint chinh xac voi VNPT khi co hop dong/sandbox.
        var path = "/api/v1/signatures/verify";
        var request = new VnptVerifyRequestDto(
            HashBase64: Convert.ToBase64String(documentHash),
            SignatureBase64: Convert.ToBase64String(signature),
            CertificateSerial: certificateSerial);

        _logger.LogInformation("[VNPT_SMARTCA] VerifySignatureAsync certificateSerial={Serial} path={Path}", certificateSerial, path);

        using var response = await _httpClient.PostAsJsonAsync(path, request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[VNPT_SMARTCA] Verify that bai. Status={Status} Body={Body}", response.StatusCode, body);
            return new VerifyResult(false, $"VNPT SmartCA tra ve loi: {response.StatusCode}");
        }

        var dto = await response.Content.ReadFromJsonAsync<VnptVerifyResponseDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("VNPT SmartCA tra ve response rong khi xac thuc chu ky.");

        return new VerifyResult(dto.IsValid, dto.Reason, dto.CertificateSerial, dto.CertificateSubject, dto.Algorithm);
    }
}

// ── DTO shape theo pattern chuẩn remote-signing (chưa xác nhận field name thật với VNPT) ──
internal record VnptCertificateResponseDto(
    [property: JsonPropertyName("serial")] string Serial,
    [property: JsonPropertyName("issuer")] string Issuer,
    [property: JsonPropertyName("valid_from")] DateTime ValidFrom,
    [property: JsonPropertyName("valid_to")] DateTime ValidTo,
    [property: JsonPropertyName("subject_name")] string SubjectName,
    [property: JsonPropertyName("is_active")] bool IsActive);

internal record VnptSignRequestDto(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("hash")] string HashBase64,
    [property: JsonPropertyName("hash_alg")] string HashAlg,
    [property: JsonPropertyName("document_type")] string DocumentType,
    [property: JsonPropertyName("document_id")] string DocumentId);

internal record VnptSignResponseDto(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("signature")] string? SignatureBase64,
    [property: JsonPropertyName("timestamp")] DateTime? Timestamp,
    [property: JsonPropertyName("certificate_serial")] string? CertificateSerial,
    [property: JsonPropertyName("error_code")] string? ErrorCode,
    [property: JsonPropertyName("error_message")] string? ErrorMessage);

internal record VnptVerifyRequestDto(
    [property: JsonPropertyName("hash")] string HashBase64,
    [property: JsonPropertyName("signature")] string SignatureBase64,
    [property: JsonPropertyName("certificate_serial")] string? CertificateSerial);

internal record VnptVerifyResponseDto(
    [property: JsonPropertyName("is_valid")] bool IsValid,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("certificate_serial")] string? CertificateSerial,
    [property: JsonPropertyName("certificate_subject")] string? CertificateSubject,
    [property: JsonPropertyName("algorithm")] string? Algorithm);
