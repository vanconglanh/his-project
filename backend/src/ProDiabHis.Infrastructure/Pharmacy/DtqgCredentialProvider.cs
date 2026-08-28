using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Đọc credential ĐTQG per-tenant (R7/R8: nay ưu tiên theo branch, 1 credential/chi nhánh) từ
/// <c>diab_his_int_dtqg_credentials</c> và giải mã token (AES-256-GCM). Ưu tiên dòng khớp branch_id
/// hiện tại; nếu chưa có (giai đoạn migrate/chưa cấu hình riêng) fallback dòng branch_id IS NULL
/// (credential dùng chung toàn tenant kiểu cũ).
/// </summary>
public class DtqgCredentialProvider : IDtqgCredentialProvider
{
    private readonly IDapperConnectionFactory _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBranchProvider _branchProvider;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<DtqgCredentialProvider> _logger;

    public DtqgCredentialProvider(
        IDapperConnectionFactory db,
        ICurrentUser currentUser,
        IBranchProvider branchProvider,
        IEncryptionService encryption,
        ILogger<DtqgCredentialProvider> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _branchProvider = branchProvider;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<DtqgTenantCredentials?> GetForCurrentTenantAsync(CancellationToken ct = default)
    {
        var tenantId = _currentUser.TenantId;
        if (tenantId is null)
            return null;

        using var conn = _db.CreateConnection();
        // Uu tien credential rieng cua branch hien tai, fallback credential dung chung (branch_id NULL)
        var row = await conn.QueryFirstOrDefaultAsync<CredentialRow>(
            @"SELECT cskcb_id AS CskcbId, partner_code AS PartnerCode, token_encrypted AS TokenEncrypted
                FROM diab_his_int_dtqg_credentials
               WHERE tenant_id = @tenantId AND deleted_at IS NULL
                 AND (branch_id = @branchId OR branch_id IS NULL)
               ORDER BY (branch_id = @branchId) DESC
               LIMIT 1",
            new { tenantId = tenantId.Value, branchId = _branchProvider.BranchId });

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
