namespace ProDiabHis.Application.Pharmacy;

/// <summary>
/// Cung cấp credential ĐTQG theo tenant hiện tại (token đã giải mã + cskcb_id + partner_code),
/// đọc từ bảng <c>diab_his_int_dtqg_credentials</c>. Dùng bởi <c>HttpDtqgClient</c> để xác thực
/// per-tenant khi gọi cổng donthuocquocgia.vn.
/// </summary>
public interface IDtqgCredentialProvider
{
    /// <summary>
    /// Lấy credential ĐTQG của tenant đang đăng nhập. Trả <c>null</c> nếu không có tenant hoặc
    /// tenant chưa đăng ký tích hợp ĐTQG. Token trong kết quả đã được giải mã (AES-256-GCM).
    /// </summary>
    Task<DtqgTenantCredentials?> GetForCurrentTenantAsync(CancellationToken ct = default);
}

/// <summary>Credential ĐTQG của một tenant. <see cref="Token"/> là token đã giải mã (có thể null nếu chưa cấu hình).</summary>
public record DtqgTenantCredentials(string? CskcbId, string? PartnerCode, string? Token);
