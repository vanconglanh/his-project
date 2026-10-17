/* =============================================================================
   ĐĂNG KÝ CLIENT SSO — PRODUCTION  (⚠️ CHỜ USER DUYỆT — CHƯA ĐƯỢC CHẠY)
   IS production: is.diab.com.vn / is.diab.vn  (container pro-diab-is)
   DB ConfigurationStore production: XÁC NHẬN TÊN DB + SERVER trước khi chạy
       (KHÔNG phải DiaB_Identity_Server trên dev-db-mssql — đó là staging/dev).
       Production dùng pro-db-mssql; tên catalog cần DevOps xác nhận.
   -----------------------------------------------------------------------------
   RÀNG BUỘC CỨNG: CHỈ chạy sau khi user duyệt. CHỈ INSERT client mới.
   KHÔNG restart IS production (DataProtection key có thể ephemeral → mất phiên).
   Backup bảng cấu hình trước. Secret plaintext giao DevOps qua kênh bí mật,
   lưu env 600 trên server HIS prod; file này chỉ chứa HASH.
   -----------------------------------------------------------------------------
   Q6 cần chốt: domain production HIS là his.diab.com.vn hay his.diab.vn?
   Dưới đây dùng his.diab.com.vn theo thiết kế. Nếu prod chạy his.diab.vn,
   đổi toàn bộ URI tương ứng TRƯỚC khi chạy (hoặc đăng ký cả hai redirect).
   ============================================================================= */
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @cid INT;

/* ======================= prodiab-his (production) ========================== */
IF NOT EXISTS (SELECT 1 FROM Clients WHERE ClientId = N'prodiab-his')
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
      (1, N'prodiab-his', N'oidc', 1, N'Pro-Diab HIS (production)', N'SSO BFF client cho HIS production',
       0, 0, 1,
       1, 0, 0, 0,
       0, N'https://his.diab.com.vn/api/v1/auth/sso/backchannel-logout', 1,
       0, 300, 3600, 60,
       2592000, 1296000, 1,
       0, 1, 0,
       1, 1, 0, @now, 300, 0);
    SET @cid = SCOPE_IDENTITY();
    INSERT INTO ClientGrantTypes (GrantType, ClientId) VALUES (N'authorization_code', @cid);
    INSERT INTO ClientScopes (Scope, ClientId) VALUES (N'openid', @cid), (N'profile', @cid), (N'email', @cid);
    INSERT INTO ClientRedirectUris (RedirectUri, ClientId) VALUES (N'https://his.diab.com.vn/api/v1/auth/sso/callback', @cid);
    INSERT INTO ClientPostLogoutRedirectUris (PostLogoutRedirectUri, ClientId) VALUES (N'https://his.diab.com.vn/login', @cid);
    INSERT INTO ClientSecrets (Description, Type, Value, Created, ClientId)
      VALUES (N'SSO prod secret', N'SharedSecret', N'2NHq34iol5DbUXuA+gXmJ910pPJau2qfsjSZlCHVyYs=', @now, @cid);
    PRINT 'Da tao client prodiab-his (Id=' + CAST(@cid AS VARCHAR) + ')';
END
ELSE PRINT 'Bo qua: prodiab-his da ton tai';

/* ======================= diab-timesheet (production) ======================= */
IF NOT EXISTS (SELECT 1 FROM Clients WHERE ClientId = N'diab-timesheet')
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
      (1, N'diab-timesheet', N'oidc', 1, N'DiaB TimeSheet (production)', N'SSO BFF client cho TimeSheet production',
       0, 0, 1,
       1, 0, 0, 0,
       0, N'https://timesheet.diab.com.vn/api/auth/sso/backchannel-logout', 1,
       0, 300, 3600, 60,
       2592000, 1296000, 1,
       0, 1, 0,
       1, 1, 0, @now, 300, 0);
    SET @cid = SCOPE_IDENTITY();
    INSERT INTO ClientGrantTypes (GrantType, ClientId) VALUES (N'authorization_code', @cid);
    INSERT INTO ClientScopes (Scope, ClientId) VALUES (N'openid', @cid), (N'profile', @cid), (N'email', @cid);
    INSERT INTO ClientRedirectUris (RedirectUri, ClientId) VALUES (N'https://timesheet.diab.com.vn/api/auth/sso/callback', @cid);
    INSERT INTO ClientPostLogoutRedirectUris (PostLogoutRedirectUri, ClientId) VALUES (N'https://timesheet.diab.com.vn/login', @cid);
    INSERT INTO ClientSecrets (Description, Type, Value, Created, ClientId)
      VALUES (N'SSO timesheet prod secret', N'SharedSecret', N'7XaA2xPLay0XT9Lg9fTVlGUP5UpAiDqug6DE+e5hTEI=', @now, @cid);
    PRINT 'Da tao client diab-timesheet (Id=' + CAST(@cid AS VARCHAR) + ')';
END
ELSE PRINT 'Bo qua: diab-timesheet da ton tai';

COMMIT TRANSACTION;
PRINT 'HOAN TAT dang ky client SSO production.';
