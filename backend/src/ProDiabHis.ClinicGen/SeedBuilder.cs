using System.Security.Cryptography;
using System.Text;

namespace ProDiabHis.ClinicGen;

/// <summary>
/// Sinh seed.sql tu answers (schema HIEN CO): tenant row, users (admin+staff)+roles,
/// dich vu, feature flags. Idempotent: UUID tat dinh tu (code+email/code) + INSERT IGNORE
/// / ON DUPLICATE KEY UPDATE nen chay lai an toan moi lan `docker compose up`.
/// (Provider profile / gio lam viec / finance / kho -> can migration moi, lam sau.)
/// </summary>
public static class SeedBuilder
{
    private const int TenantId = 1; // 1 tenant / DB

    // Role UUID co dinh tu 9007_seed_master_data.sql
    private static readonly Dictionary<string, string> RoleId = new()
    {
        ["admin"] = "00000000-0000-0000-0000-000000000001",
        ["bac_si"] = "00000000-0000-0000-0000-000000000002",
        ["le_tan"] = "00000000-0000-0000-0000-000000000003",
        ["duoc_si"] = "00000000-0000-0000-0000-000000000004",
        ["ke_toan"] = "00000000-0000-0000-0000-000000000005",
        ["ky_thuat_vien"] = "00000000-0000-0000-0000-000000000006",
    };

    public static string Build(Answers a)
    {
        var sb = new StringBuilder();
        var code = a.Clinic.Code;
        sb.AppendLine("-- ============================================================");
        sb.AppendLine($"-- seed.sql — phong kham {a.Clinic.Name} ({code}) — SINH TU DONG");
        sb.AppendLine("-- Idempotent. Chay sau khi da apply schema 9xxx.");
        sb.AppendLine("-- ============================================================");
        sb.AppendLine("SET NAMES utf8mb4;");
        sb.AppendLine();

        // 1) Tenant row
        var logo = a.Branding?.Logo;
        sb.AppendLine("-- Tenant / phong kham");
        sb.AppendLine(
            "INSERT INTO `diab_his_sys_tenants` (id, code, name, company_name, cskcb_code, tax_code, address, phone, email, website, slogan, logo_url, subdomain, status, storage_quota_gb)");
        sb.AppendLine($"VALUES ({TenantId}, {S(code)}, {S(a.Clinic.Name)}, {SN(a.Clinic.CompanyName)}, {SN(a.Clinic.CskcbCode)}, {SN(a.Clinic.TaxCode)}, {SN(a.Clinic.Address)}, {SN(a.Clinic.Phone)}, {SN(a.Clinic.Email)}, {SN(a.Clinic.Website)}, {SN(a.Clinic.Slogan)}, {SN(logo)}, {S(code)}, 'ACTIVE', 20)");
        sb.AppendLine(
            "ON DUPLICATE KEY UPDATE name=VALUES(name), company_name=VALUES(company_name), cskcb_code=VALUES(cskcb_code), tax_code=VALUES(tax_code), address=VALUES(address), phone=VALUES(phone), email=VALUES(email), website=VALUES(website), slogan=VALUES(slogan), logo_url=VALUES(logo_url);");
        sb.AppendLine();

        // 2) Users = admin + staff
        sb.AppendLine("-- Nguoi dung (admin + nhan su)");
        AppendUser(sb, code, a.Admin.FullName, a.Admin.Email, null, new List<string> { "admin" },
            a.Admin.PasswordMode, a.Admin.Password);
        foreach (var st in a.Staff ?? new())
            AppendUser(sb, code, st.FullName, st.Email, st.Phone, st.Roles, st.PasswordMode, st.Password);
        sb.AppendLine();

        // 3) Dich vu
        if (a.Services is { Count: > 0 })
        {
            sb.AppendLine("-- Danh muc dich vu + gia");
            foreach (var s in a.Services)
            {
                var id = DetGuid($"svc:{code}:{s.Code}");
                sb.AppendLine(
                    $"INSERT IGNORE INTO `diab_his_bil_services` (id, tenant_id, code, name, category, price, vat_rate, bhyt_code, is_active) VALUES ({S(id)}, {TenantId}, {S(s.Code)}, {S(s.Name)}, 'CONSULTATION', {s.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {s.Vat ?? 0}, {SN(s.BhytCode)}, 1);");
            }
            sb.AppendLine();
        }

        // 4) Feature flags (module on/off)
        var m = a.Modules;
        if (m is not null)
        {
            sb.AppendLine("-- Feature flags (module bat/tat)");
            Flag(sb, "module.bhyt", m.Bhyt ?? false);
            Flag(sb, "module.dtqg", m.Dtqg ?? false);
            Flag(sb, "module.pharmacy", m.Pharmacy ?? true);
            Flag(sb, "module.einvoice", m.Einvoice ?? false);
            Flag(sb, "module.cdss", m.Cdss ?? true);
            Flag(sb, "module.patient_portal", m.PatientPortal ?? false);
            Flag(sb, "module.lab_integration", m.LabIntegration ?? false);
            Flag(sb, "module.online_booking", m.OnlineBooking ?? false);
            Flag(sb, "module.sms_reminder", m.SmsReminder ?? false);
            Flag(sb, "module.zalo_oa", m.ZaloOa ?? false);
        }

        return sb.ToString();
    }

    private static void AppendUser(StringBuilder sb, string code, string fullName, string email,
        string? phone, List<string> roles, string passwordMode, string? password)
    {
        var userId = DetGuid($"user:{code}:{email.ToLowerInvariant()}");
        var setNow = string.Equals(passwordMode, "set_now", StringComparison.OrdinalIgnoreCase);
        var hash = setNow && !string.IsNullOrEmpty(password)
            ? BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)
            : "";
        var status = setNow ? "ACTIVE" : "PENDING";
        var token = setNow ? "NULL" : S(RandHex(32));
        var expires = setNow ? "NULL" : "DATE_ADD(NOW(), INTERVAL 7 DAY)";

        sb.AppendLine(
            $"INSERT IGNORE INTO `diab_his_sec_users` (id, tenant_id, email, password_hash, full_name, phone, user_status, is_active, invite_token, invite_token_expires_at, created_at) VALUES ({S(userId)}, {TenantId}, {S(email)}, {S(hash)}, {S(fullName)}, {SN(phone)}, '{status}', 1, {token}, {expires}, NOW());");
        foreach (var r in roles.Distinct())
        {
            if (!RoleId.TryGetValue(r, out var rid)) continue;
            sb.AppendLine(
                $"INSERT IGNORE INTO `diab_his_sec_user_roles` (user_id, role_id, tenant_id) VALUES ({S(userId)}, {S(rid)}, {TenantId});");
        }
    }

    private static void Flag(StringBuilder sb, string key, bool enabled)
        => sb.AppendLine(
            $"INSERT INTO `diab_his_sys_feature_flags` (`key`, enabled, description) VALUES ({S(key)}, {(enabled ? 1 : 0)}, {S(key)}) ON DUPLICATE KEY UPDATE enabled=VALUES(enabled);");

    // ── helpers ─────────────────────────────────────────────
    private static string S(string? v) => v is null ? "NULL" : "'" + v.Replace("\\", "\\\\").Replace("'", "''") + "'";
    private static string SN(string? v) => string.IsNullOrEmpty(v) ? "NULL" : S(v);

    /// <summary>UUID tat dinh (version-4-like) tu chuoi seed — de re-gen ra cung id (idempotent).</summary>
    private static string DetGuid(string seed)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40); // version 4
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // variant
        return new Guid(bytes).ToString();
    }

    private static string RandHex(int nBytes)
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(nBytes)).ToLowerInvariant();
}
