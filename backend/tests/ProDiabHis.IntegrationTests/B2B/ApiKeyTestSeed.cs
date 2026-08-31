using System.Security.Cryptography;
using System.Text;
using Dapper;
using MySqlConnector;

namespace ProDiabHis.IntegrationTests.B2B;

/// <summary>
/// Tien ich seed API key doi tac B2B (bang diab_his_api_partners) va tinh hash de leader tai dung.
///
/// Cach ApiKeyAuthFilter xac thuc:
///   header X-Api-Key -> SHA-256 (hex thuong) -> IApiKeyStore.FindByHashAsync tra ApiPartnerContext.
/// Vi vay khi seed ta luu SHA-256(raw key) vao cot api_key_hash; goi API thi gui raw key.
/// </summary>
public static class ApiKeyTestSeed
{
    /// <summary>SHA-256 hex thuong — dung y het ComputeSha256 trong ApiKeyAuthFilter.</summary>
    public static string Sha256Hex(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLower();

    /// <summary>
    /// Bo sung cot `ip_whitelist` (idempotent) — ApiKeyStoreImpl.FindByHashAsync SELECT thang
    /// cot nay (khac cot EF `ip_whitelist_json`), thieu se lam cau SQL khong parse duoc.
    /// Cot `scopes` da duoc TestSchemaSupplement them san. Chi them phan con thieu, khong sua
    /// file ha tang dung chung.
    /// </summary>
    public static async Task EnsureReadColumnsAsync(string connectionString)
    {
        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        var hasIpWhitelist = await conn.ExecuteScalarAsync<long>(
            @"SELECT COUNT(*) FROM information_schema.columns
              WHERE table_schema = DATABASE() AND table_name = 'diab_his_api_partners'
                AND column_name = 'ip_whitelist'");
        if (hasIpWhitelist == 0)
            await conn.ExecuteAsync("ALTER TABLE `diab_his_api_partners` ADD COLUMN `ip_whitelist` JSON NULL");
    }
}
