namespace ProDiabHis.Application.Auth.Sso;

/// <summary>Cau hinh SSO OIDC (section "Sso", env Sso__Key). Mac dinh TAT.</summary>
public class SsoOptions
{
    public const string SectionName = "Sso";

    public bool Enabled { get; set; } = false;
    public string ProviderKey { get; set; } = "diab-is";
    public string DisplayName { get; set; } = "DiaB";
    /// <summary>URL goc IdP, dung de lay discovery. KHONG dung de so khop iss (iss lay tu discovery).</summary>
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
    public string PostLogoutRedirectUri { get; set; } = "";
    public string FrontendBaseUrl { get; set; } = "";
    public string Scopes { get; set; } = "openid profile email";
    public bool RequireEmailVerified { get; set; } = true;
    public bool TrustIdpMfa { get; set; } = false;
    public string[] TrustedMfaAmrValues { get; set; } = new[] { "mfa", "otp", "hwk" };
    public bool EnforceSidRevocation { get; set; } = true;
    public int StateTtlSeconds { get; set; } = 600;
}
