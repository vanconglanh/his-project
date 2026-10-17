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
    /// <summary>Issuer hop le bo sung ngoai issuer lay tu discovery (de xu ly truong hop host IdP khac
    /// voi gia tri iss, vd https://is.staging.diab.com.vn vs https://is.stg.savvycom.asia). Cau hinh env
    /// Sso__ValidIssuers__0, Sso__ValidIssuers__1...</summary>
    public string[] ValidIssuers { get; set; } = Array.Empty<string>();
    /// <summary>
    /// Bo kiem HAN (expiry) cua X509 signing cert khi validate token (mac dinh TRUE).
    /// LY DO: signing cert cua IdentityServer DiaB da het han tu 2020 nhung IS van dung de ky,
    /// portal/app van chap nhan. Khong the thay cert IS (phai restart IS -> mat phien moi client).
    /// Khi TRUE: van VERIFY chu ky bang khoa lay tu JWKS qua HTTPS (RequireSignedTokens=true,
    /// IssuerSigningKeys = JWKS) nhung bo qua rang buoc han/chain cua cert (ValidateIssuerSigningKey=false).
    /// Token van phai duoc ky bang dung khoa trong JWKS cua IS -> khong giam do an toan chong gia mao.
    /// RUI RO: neu IS bi lo private key cu, khong co co che het-han-tu-nhien phia cert; giam thieu bang
    /// chi tin JWKS tu Authority HTTPS + kid. Khi IS xoay cert moi (co overlap key) nen dat lai FALSE.
    /// </summary>
    public bool IgnoreSigningCertExpiry { get; set; } = true;
    public bool RequireEmailVerified { get; set; } = true;
    public bool TrustIdpMfa { get; set; } = false;
    public string[] TrustedMfaAmrValues { get; set; } = new[] { "mfa", "otp", "hwk" };
    public bool EnforceSidRevocation { get; set; } = true;
    public int StateTtlSeconds { get; set; } = 600;
}
