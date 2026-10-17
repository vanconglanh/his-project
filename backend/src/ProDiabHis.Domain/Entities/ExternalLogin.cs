using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities;

/// <summary>Lien ket tai khoan ngoai (SSO OIDC) -> user HIS. Map bang diab_his_sec_user_external_logins.
/// Mot (Provider, Subject) co the lien ket nhieu tenant (mot nguoi lam o nhieu phong kham).</summary>
public class ExternalLogin : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Khoa nha cung cap, vd "diab-is"</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Claim sub tu IdP</summary>
    public string Subject { get; set; } = string.Empty;
    public string? EmailAtLink { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastSid { get; set; }
}
