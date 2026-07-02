using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Đọc credential ĐTQG per-tenant từ <c>diab_his_int_dtqg_credentials</c> và giải mã token (AES-256-GCM).
/// Query khớp với <c>SubmitDtqgHandler</c> (chỉ 3 cột, lọc theo tenant + deleted_at) để tránh phụ thuộc
/// các cột biến thể schema (is_active/id) giữa các migration.
/// </summary>
public class DtqgCredentialProvider : IDtqgCredentialProvider
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<DtqgCredentialProvider> _logger;

    public DtqgCredentialProvider(
        IDapperConnectionFactory db,
        ICurrentUser currentUser,
        IEncryptionService encryption,
        ILogger<DtqgCredentialProvider> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<DtqgTenantCredentials?> GetForCurrentTenantAsync(CancellationToken ct = default)
    {
        var tenantId = _currentUser.TenantId;
        if (tenantId is null)
            return null;

        using var conn = _db.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync<CredentialRow>(
            "SELECT cskcb_id AS CskcbId, partner_code AS PartnerCode, token_encrypted AS TokenEncrypted " +
            "FROM diab_his_int_dtqg_credentials WHERE tenant_id = @tenantId AND deleted_at IS NULL",
            new { tenantId = tenantId.Value });

        if (row is null)
            return null;

        string? token = null;
        if (!string.IsNullOrWhiteSpace(row.TokenEncrypted))
        {
            try
            {
                token = _encryption.Decrypt(row.TokenEncrypted);
            }
            catch (Exception ex)
            {
                // Token hỏng/không giải mã được -> log và trả token null (fallback config sẽ dùng)
                _logger.LogError(ex, "DTQG: giai ma token that bai cho tenant {TenantId}", tenantId.Value);
            }
        }

        return new DtqgTenantCredentials(row.CskcbId, row.PartnerCode, token);
    }

    private sealed class CredentialRow
    {
        public string? CskcbId { get; set; }
        public string? PartnerCode { get; set; }
        public string? TokenEncrypted { get; set; }
    }
}
