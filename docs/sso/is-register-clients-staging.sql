/* =============================================================================
   Đăng ký client SSO trên IdentityServer STAGING/DEV
   DB: DiaB_Identity_Server  (SQL Server dev-db-mssql,1433 — DÙNG CHUNG cho
       container stg-uat-is [is.staging.diab.com.vn] và dev-is [is.dev.diab.vn])
   -----------------------------------------------------------------------------
   AN TOÀN:
   - CHỈ INSERT client MỚI. KHÔNG UPDATE/DELETE client cũ.
   - KHÔNG đụng signing pfx, IssuerUri, cookie, DataProtection, PersistedGrants.
   - ConfigurationStore KHÔNG bật client cache (AddClientStoreCache comment) =>
     client mới có hiệu lực NGAY, KHÔNG cần restart IS.
   - Idempotent: chạy lại nhiều lần không tạo trùng (kiểm theo ClientId).
   - Chạy trong transaction; backup bảng cấu hình trước (xem README).
   -----------------------------------------------------------------------------
   Secret (plaintext) KHÔNG nằm trong file này — chỉ lưu HASH SHA256-base64
   (chuẩn IS4 ClientSecret). Plaintext lưu ở file env quyền 600 trên server.
   ============================================================================= */
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @cid INT;

/* -------- Helper: tạo 1 confidential auth-code + PKCE client ---------------- */
/* Không dùng proc (tránh để lại object); lặp bằng khối IF cho từng client.    */

/* ======================= 1) prodiab-his-staging ============================ */
IF NOT EXISTS (SELECT 1 FROM Clients WHERE ClientId = N'prodiab-his-staging')
BEGIN
    INSERT INTO Clients
      (Enabled, ClientId, ProtocolType, RequireClientSecret, ClientName, Description,
       RequireConsent, AllowRememberConsent, AlwaysIncludeUserClaimsInIdToken,
       RequirePkce, AllowPlainTextPkce, RequireRequestObject, AllowAccessTokensViaBrowser,
       FrontChannelLogoutSessionRequired, BackChannelLogoutUri, BackChannelLogoutSessionRequired,
       AllowOfflineAccess, IdentityTokenLifetime, AccessTokenLifetime, AuthorizationCodeLifetime,
       AbsoluteRefreshTokenLifetime, SlidingRefreshTokenLifetime, RefreshTokenUsage,
       UpdateAccessTokenClaimsOnRefresh, RefreshTokenExpiration, AccessTokenType,
       EnableLocalLogin, IncludeJwtId, AlwaysSendClientClaims, Created, DeviceCodeLifetime, NonEditable)
    VALUES
      (1, N'prodiab-his-staging', N'oidc', 1, N'Pro-Diab HIS (staging)', N'SSO BFF client cho HIS staging his.diab.vn',
       0, 0, 1,
       1, 0, 0, 0,
       0, N'https://his.diab.vn/api/v1/auth/sso/backchannel-logout', 1,
       0, 300, 3600, 60,
       2592000, 1296000, 1,
       0, 1, 0,
       1, 1, 0, @now, 300, 0);
    SET @cid = SCOPE_IDENTITY();

    INSERT INTO ClientGrantTypes (GrantType, ClientId) VALUES (N'authorization_code', @cid);
    INSERT INTO ClientScopes (Scope, ClientId) VALUES (N'openid', @cid), (N'profile', @cid), (N'email', @cid);
    INSERT INTO ClientRedirectUris (RedirectUri, ClientId) VALUES (N'https://his.diab.vn/api/v1/auth/sso/callback', @cid);
    INSERT INTO ClientPostLogoutRedirectUris (PostLogoutRedirectUri, ClientId) VALUES (N'https://his.diab.vn/login', @cid);
    /* HASH SHA256-base64 của secret prodiab-his-staging (plaintext ở env 600 trên server) */
    INSERT INTO ClientSecrets (Description, Type, Value, Created, ClientId)
      VALUES (N'SSO staging secret', N'SharedSecret', N'BTVCmrw1EbY6nFaWqCTFZiUyr4zDcEEUnvBu5V/FzYQ=', @now, @cid);
    PRINT 'Da tao client prodiab-his-staging (Id=' + CAST(@cid AS VARCHAR) + ')';
END
ELSE PRINT 'Bo qua: prodiab-his-staging da ton tai';

/* ======================= 2) prodiab-his-dev (local dev) ==================== */
IF NOT EXISTS (SELECT 1 FROM Clients WHERE ClientId = N'prodiab-his-dev')
BEGIN
    INSERT INTO Clients
      (Enabled, ClientId, ProtocolType, RequireClientSecret, ClientName, Description,
       RequireConsent, AllowRememberConsent, AlwaysIncludeUserClaimsInIdToken,
       RequirePkce, AllowPlainTextPkce, RequireRequestObject, AllowAccessTokensViaBrowser,
       FrontChannelLogoutSessionRequired, BackChannelLogoutUri, BackChannelLogoutSessionRequired,
       AllowOfflineAccess, IdentityTokenLifetime, AccessTokenLifetime, AuthorizationCodeLifetime,
       AbsoluteRefreshTokenLifetime, SlidingRefreshTokenLifetime, RefreshTokenUsage,
       UpdateAccessTokenClaimsOnRefresh, RefreshTokenExpiration, AccessTokenType,
       EnableLocalLogin, IncludeJwtId, AlwaysSendClientClaims, Created, DeviceCodeLifetime, NonEditable)
    VALUES
      (1, N'prodiab-his-dev', N'oidc', 1, N'Pro-Diab HIS (dev local)', N'SSO BFF client cho dev local localhost:5000',
       0, 0, 1,
       1, 0, 0, 0,
       0, NULL, 1,
       0, 300, 3600, 60,
       2592000, 1296000, 1,
       0, 1, 0,
       1, 1, 0, @now, 300, 0);
    SET @cid = SCOPE_IDENTITY();
    INSERT INTO ClientGrantTypes (GrantType, ClientId) VALUES (N'authorization_code', @cid);
    INSERT INTO ClientScopes (Scope, ClientId) VALUES (N'openid', @cid), (N'profile', @cid), (N'email', @cid);
    INSERT INTO ClientRedirectUris (RedirectUri, ClientId) VALUES (N'http://localhost:5000/api/v1/auth/sso/callback', @cid);
    INSERT INTO ClientPostLogoutRedirectUris (PostLogoutRedirectUri, ClientId) VALUES (N'http://localhost:3000/login', @cid);
    INSERT INTO ClientSecrets (Description, Type, Value, Created, ClientId)
      VALUES (N'SSO dev secret', N'SharedSecret', N'KFRLKjISwnMJ1mKnqSMIxqUu7QIre1yflv0uCa84qHw=', @now, @cid);
    PRINT 'Da tao client prodiab-his-dev (Id=' + CAST(@cid AS VARCHAR) + ')';
END
ELSE PRINT 'Bo qua: prodiab-his-dev da ton tai';

/* ======================= 3) diab-timesheet-staging ========================= */
/* TimeSheet staging — domain CẦN XÁC NHẬN với DevOps (proxy có api.timesheet.diab.vn,
   portal.timesheet.diab.vn). Dưới đây dùng api.timesheet.diab.vn, route api/auth/sso.
   Nếu domain khác, sửa 3 URI trước khi chạy. TimeSheet pha này CHƯA deploy SSO. */
IF NOT EXISTS (SELECT 1 FROM Clients WHERE ClientId = N'diab-timesheet-staging')
BEGIN
    INSERT INTO Clients
      (Enabled, ClientId, ProtocolType, RequireClientSecret, ClientName, Description,
       RequireConsent, AllowRememberConsent, AlwaysIncludeUserClaimsInIdToken,
       RequirePkce, AllowPlainTextPkce, RequireRequestObject, AllowAccessTokensViaBrowser,
       FrontChannelLogoutSessionRequired, BackChannelLogoutUri, BackChannelLogoutSessionRequired,
       AllowOfflineAccess, IdentityTokenLifetime, AccessTokenLifetime, AuthorizationCodeLifetime,
       AbsoluteRefreshTokenLifetime, SlidingRefreshTokenLifetime, RefreshTokenUsage,
       UpdateAccessTokenClaimsOnRefresh, RefreshTokenExpiration, AccessTokenType,
       EnableLocalLogin, IncludeJwtId, AlwaysSendClientClaims, Created, DeviceCodeLifetime, NonEditable)
    VALUES
      (1, N'diab-timesheet-staging', N'oidc', 1, N'DiaB TimeSheet (staging)', N'SSO BFF client cho TimeSheet staging',
       0, 0, 1,
       1, 0, 0, 0,
       0, N'https://api.timesheet.diab.vn/api/auth/sso/backchannel-logout', 1,
       0, 300, 3600, 60,
       2592000, 1296000, 1,
       0, 1, 0,
       1, 1, 0, @now, 300, 0);
    SET @cid = SCOPE_IDENTITY();
    INSERT INTO ClientGrantTypes (GrantType, ClientId) VALUES (N'authorization_code', @cid);
    INSERT INTO ClientScopes (Scope, ClientId) VALUES (N'openid', @cid), (N'profile', @cid), (N'email', @cid);
    INSERT INTO ClientRedirectUris (RedirectUri, ClientId) VALUES (N'https://api.timesheet.diab.vn/api/auth/sso/callback', @cid);
    INSERT INTO ClientPostLogoutRedirectUris (PostLogoutRedirectUri, ClientId) VALUES (N'https://portal.timesheet.diab.vn/login', @cid);
    INSERT INTO ClientSecrets (Description, Type, Value, Created, ClientId)
      VALUES (N'SSO timesheet staging secret', N'SharedSecret', N'xGB1FjYgVfJuYFg0kTGF6NScKwFt3m61/SmkGcr0iuw=', @now, @cid);
    PRINT 'Da tao client diab-timesheet-staging (Id=' + CAST(@cid AS VARCHAR) + ')';
END
ELSE PRINT 'Bo qua: diab-timesheet-staging da ton tai';

COMMIT TRANSACTION;
PRINT 'HOAN TAT dang ky client SSO staging/dev.';

/* Kiểm tra sau khi chạy:
   SELECT c.ClientId, g.GrantType, s.Scope
   FROM Clients c
   LEFT JOIN ClientGrantTypes g ON g.ClientId=c.Id
   LEFT JOIN ClientScopes s ON s.ClientId=c.Id
   WHERE c.ClientId IN (N'prodiab-his-staging', N'prodiab-his-dev', N'diab-timesheet-staging')
   ORDER BY c.ClientId; */
