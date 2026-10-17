# Thiết kế SSO DiaB Portal → HIS + TimeSheet

- Ngày: 2026-10-17 — Tác giả: Lành (architect)
- PRD: `docs/prd/sso-portal-20261017.md`
- Trạng thái: Bản nháp, chưa implement

## 1. Hiện trạng IdentityServer (đọc từ source)

Nguồn: `D:\diab\dev\git\pro-diab\diab-dotnet-api\IdentityServer\DiaB.IdentityServer`

| Hạng mục | Hiện trạng |
|---------|-----------|
| Sản phẩm | **IdentityServer4** (EOL từ 12/2022) trên ASP.NET Core |
| Lưu Client/Resource | **DB SQL Server** qua `AddConfigurationStore` (bảng IS4 chuẩn: `Clients`, `ClientRedirectUris`, `ClientPostLogoutRedirectUris`, `ClientScopes`, `ClientSecrets`...). `AddInMemoryClients` đã bị comment → đăng ký client mới = INSERT vào DB cấu hình (hoặc qua công cụ admin nếu có) |
| Operational store | SQL Server (`PersistedGrants`, token cleanup) |
| User store | ASP.NET Identity `User` (`AddEntityFrameworkStores<MainDbContext>`, SQL Server); có thêm `DiabDbContext` (MySQL) |
| Login UI | `/Account/Login`, `/Account/Logout` (MVC) |
| External login | Google, Facebook, Apple, Zalo |
| Grant đặc biệt | ROPC, extension grant phone/username/external/apple |
| Claims (ProfileService) | Lấy toàn bộ claim từ `IUserClaimsPrincipalFactory<User>` (thường: `sub`, `name`, `email`, `email_verified`? (tùy), `role` nếu có, `phone_number`...) + claim custom: `MustChangePassword`, `IsLinkedGoogle/Facebook/Apple/Zalo`, `IsMobileAccount`, `FirstLinkedAccount`, `GoogleEmail`. Phát chung cho access token, id token và userinfo (không lọc theo scope requested) |
| IsActive | `user.IsActive` |
| Signing | X509 pfx (production) → JWKS công khai |
| Lỗi bảo mật phát hiện | ClientSecret Zalo hard-code trong `Startup.cs` (ngoài phạm vi, báo chủ hệ thống) |

Lưu ý: ProfileService **không phát `amr`/`sid`** — hai claim này IS4 tự thêm vào id_token từ phiên đăng nhập (`amr=pwd` hoặc `external`, `sid` = session id). Vì IdP hiện không có MFA, `amr` sẽ không bao giờ chứa `mfa` → admin HIS luôn phải 2FA ở HIS.

## 2. Lựa chọn kiến trúc: Backend-for-Frontend (confidential client)

**Chọn:** Backend HIS (.NET 8) là **confidential client**, Authorization Code + **PKCE** + client secret. FE Next.js không chạm token IdP.

| Tiêu chí | BFF confidential (chọn) | FE public client (oidc-client-ts) |
|---------|------------------------|-----------------------------------|
| Token IdP ở trình duyệt | Không | Có (localStorage/memory) → rủi ro XSS |
| Tái dùng phiên HIS | Backend phát JWT HIS + refresh cookie httpOnly như hiện tại | Phải đổi id_token lấy JWT HIS qua endpoint mới — thực chất vẫn cần backend |
| Ánh xạ email → user/tenant, 2FA, audit | Tập trung một chỗ | Vẫn phải ở backend |
| Back-channel logout | Backend nhận trực tiếp | Không có server nhận |
| Client secret | Lưu env server | Không có |
| Độ phức tạp | Thêm 4 endpoint backend | Thêm lib FE + endpoint exchange |

Kết luận: BFF vừa an toàn hơn vừa khớp kiến trúc phiên hiện tại của HIS. TimeSheet làm tương tự (backend OperationsService là confidential client). App mobile TimeSheet (pha 2) mới dùng public client + PKCE.

## 3. Sequence — đăng nhập

```mermaid
sequenceDiagram
    autonumber
    actor U as Người dùng
    participant FE as HIS FE (Next.js)
    participant BE as HIS API (.NET 8)
    participant R as Redis
    participant IS as is.diab.com.vn
    participant DB as MySQL HIS

    U->>FE: Bấm "Đăng nhập bằng tài khoản DiaB"
    FE->>BE: GET /api/v1/auth/sso/login?returnUrl=/dashboard
    BE->>BE: Sinh state, nonce, code_verifier; kiểm returnUrl (chỉ path nội bộ)
    BE->>R: SET sso:state:{state} = {nonce, verifier, returnUrl} TTL 10'
    BE-->>U: 302 authorize?client_id=prodiab-his&response_type=code&scope=openid profile email&code_challenge=S256&state&nonce&redirect_uri=.../callback
    U->>IS: Đăng nhập (hoặc đã có phiên portal)
    IS-->>U: 302 /api/v1/auth/sso/callback?code&state
    U->>BE: GET callback
    BE->>R: GETDEL sso:state:{state} (không có → SSO_INVALID_STATE)
    BE->>IS: POST /connect/token (code, verifier, client_secret)
    IS-->>BE: id_token, access_token
    BE->>BE: Validate id_token qua JWKS: iss, aud=client_id, exp, nonce, alg RS256
    BE->>DB: Tìm liên kết (provider='diab-is', subject=sub)
    alt Chưa có liên kết
        BE->>DB: Tìm user active theo email (blind index / lower(email)), email_verified=true
        BE->>DB: INSERT diab_his_sec_user_external_logins
    end
    alt Không tìm thấy / bị khóa
        BE->>DB: Audit AUTH_SSO_LOGIN_DENIED
        BE-->>U: 302 /login?error=SSO_USER_NOT_PROVISIONED
    else Admin và không có amr mfa tin cậy
        BE-->>U: 302 /login/2fa (pending 2FA ticket như luồng mật khẩu, kèm sso_sid)
    else Thành công
        BE->>DB: Lưu refresh token HIS (kèm sso_sid, sso_sub) + audit AUTH_SSO_LOGIN_SUCCESS
        BE-->>U: Set-Cookie access/refresh (httpOnly, Secure, SameSite=Lax) + 302 returnUrl
    end
```

## 4. Sequence — đăng xuất

```mermaid
sequenceDiagram
    autonumber
    actor U as Người dùng
    participant BE as HIS API
    participant IS as is.diab.com.vn
    participant TS as TimeSheet API
    U->>BE: POST /api/v1/auth/sso/logout
    BE->>BE: Thu hồi refresh token, xóa cookie, audit AUTH_SSO_LOGOUT
    BE-->>U: 200 (chỉ kết thúc phiên HIS)
    U->>U: Chuyển về https://his.../login — KHÔNG gọi /connect/endsession (giữ phiên portal + app)
    Note over IS,BE: Đăng xuất từ portal: IS gọi POST /api/v1/auth/sso/backchannel-logout tới HIS
    IS->>BE: POST logout_token (JWT: iss, aud, sub, sid, events, jti)
    BE->>BE: Validate chữ ký + iss + aud + events + không có nonce + jti chưa dùng
    BE->>BE: Thu hồi refresh token theo sid/sub; Redis SET sso:revoked-sid:{sid} TTL = access token lifetime
```

## 5. Client cần đăng ký trên IdentityServer

Chung: `AllowedGrantTypes=authorization_code`, `RequirePkce=true`, `RequireClientSecret=true`, `AllowOfflineAccess=false` (HIS tự quản refresh), `RequireConsent=false`, `AlwaysIncludeUserClaimsInIdToken=true` (để có `email` trong id_token, hoặc backend gọi userinfo), `BackChannelLogoutSessionRequired=true`, `IdentityTokenLifetime=300`, `AuthorizationCodeLifetime=60`, scopes `openid profile email`. Không cần scope `DiaB` (HIS không gọi API DiaB).

| client_id | Môi trường | redirect_uris | post_logout_redirect_uris | backchannel_logout_uri | front-channel |
|-----------|-----------|---------------|---------------------------|------------------------|---------------|
| `prodiab-his-staging` | staging his.diab.vn | `https://his.diab.vn/api/v1/auth/sso/callback` | `https://his.diab.vn/login` | `https://his.diab.vn/api/v1/auth/sso/backchannel-logout` | không dùng |
| `prodiab-his` | production his.diab.com.vn | `https://his.diab.com.vn/api/v1/auth/sso/callback` | `https://his.diab.com.vn/login` | `https://his.diab.com.vn/api/v1/auth/sso/backchannel-logout` | không dùng |
| `diab-timesheet` | production timesheet.diab.com.vn | `https://timesheet.diab.com.vn/api/auth/sso/callback` (*) | `https://timesheet.diab.com.vn/login` | `https://timesheet.diab.com.vn/api/auth/sso/backchannel-logout` | không dùng |
| `diab-timesheet-mobile` (pha 2) | app mobile | `vn.diab.timesheet:/oauth/callback` | `vn.diab.timesheet:/logout` | — | — (public client, không secret) |
| `prodiab-his-dev` (tùy chọn) | dev local | `http://localhost:5000/api/v1/auth/sso/callback` | `http://localhost:3000/login` | — | — |

(*) Prefix route TimeSheet giả định `/api/...` qua nginx; xác nhận khi triển khai. Nếu cần staging TimeSheet, thêm `diab-timesheet-staging` tương tự.

Client secret: sinh ngẫu nhiên >= 32 byte, IS lưu dạng SHA256, gửi cho DevOps qua kênh bí mật; xoay vòng 12 tháng.

## 6. Thay đổi backend HIS

### 6.1 Endpoint (`AuthController` hoặc `SsoController`, `[AllowAnonymous]`)

| Method | Route | Mô tả |
|--------|-------|-------|
| GET | `/api/v1/auth/sso/config` | `{ enabled, providerName: "DiaB" }` để FE ẩn/hiện nút |
| GET | `/api/v1/auth/sso/login?returnUrl=` | Tạo state/nonce/PKCE, lưu Redis, 302 tới authorize |
| GET | `/api/v1/auth/sso/callback?code&state` (hoặc `error`) | Đổi code, validate, ánh xạ user, phát phiên HIS hoặc chuyển 2FA; luôn kết thúc bằng 302 về FE |
| POST | `/api/v1/auth/sso/logout` | Chỉ thu hồi phiên HIS. KHÔNG trả/gọi end_session_endpoint |
| POST | `/api/v1/auth/sso/backchannel-logout` | `application/x-www-form-urlencoded` `logout_token`; trả 200 / 400; không cần cookie; loại khỏi CSRF/rate-limit user |
| DELETE | `/api/v1/users/{id}/external-logins/{provider}` | Admin gỡ liên kết (`RequirePermission("users.update")`) |

Mã lỗi (redirect `/login?error=CODE`, message tiếng Việt ở FE `vi.json`):
`SSO_DISABLED`, `SSO_INVALID_STATE`, `SSO_IDP_ERROR`, `SSO_TOKEN_INVALID`, `SSO_EMAIL_NOT_VERIFIED`, `SSO_USER_NOT_PROVISIONED` ("Tài khoản chưa được cấp quyền HIS"), `SSO_USER_DISABLED`, `SSO_AMBIGUOUS_ACCOUNT`, `SSO_IDP_UNAVAILABLE`.

### 6.2 Tầng Application / Infrastructure

- `Application/Auth/Sso/`: `StartSsoLoginCommand`, `CompleteSsoLoginCommand` (trả `Result<LoginResult>` dùng lại DTO login hiện có, gồm nhánh `RequiresTwoFactor`), `SsoBackchannelLogoutCommand`.
- `Infrastructure/Sso/OidcClient`: dùng `Microsoft.IdentityModel.Protocols.OpenIdConnect` `ConfigurationManager<OpenIdConnectConfiguration>` (cache discovery + JWKS, tự refresh khi gặp `kid` lạ), `JsonWebTokenHandler` validate. Không dùng middleware `AddOpenIdConnect` cookie-based để giữ mô hình JWT hiện tại.
- Validate id_token: `ValidIssuer=Sso__Authority` (khớp chính xác `issuer` discovery), `ValidAudience=ClientId`, `ValidAlgorithms=[RS256]`, lifetime + ClockSkew 2 phút, `nonce` == giá trị Redis, `azp` (nếu có) == ClientId, có `sub`.
- Ánh xạ email: chuẩn hóa `trim().ToLowerInvariant()`; tra user theo email qua Dapper **không có tenant filter** (ngoại lệ có kiểm soát, giống luồng login mật khẩu — ghi rõ trong code review) rồi chỉ lấy user `deleted_at IS NULL AND is_active`.
- Phát JWT HIS: tái dùng `JwtService` (lưu ý giữ quy tắc không nhét permission của admin vào JWT). Thêm claim `amr=sso`, `sso_sid` (không bắt buộc).
- Refresh token HIS: thêm cột `sso_sid`, `sso_sub` để thu hồi theo back-channel.
- Access token lifetime HIS: giữ nguyên giá trị hiện tại (không đổi, xem mục 12.2); middleware kiểm `sso:revoked-sid:{sid}` trong Redis (O(1)) nếu bật `Sso__EnforceSidRevocation`.

### 6.3 Database — migration `db/migrations/9221_create_sec_user_external_logins.sql`

Skeleton (idempotent, theo quy ước MySQL 8.0.23):

```sql
CREATE TABLE IF NOT EXISTS diab_his_sec_user_external_logins (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  uuid            CHAR(36) NOT NULL DEFAULT (UUID()),
  tenant_id       INT NOT NULL,
  user_id         INT NOT NULL,
  provider        VARCHAR(50)  NOT NULL COMMENT 'diab-is',
  subject         VARCHAR(200) NOT NULL COMMENT 'claim sub tu IdP',
  email_at_link   VARCHAR(255) NULL COMMENT 'email luc lien ket (tham khao)',
  last_login_at   DATETIME NULL,
  last_sid        VARCHAR(200) NULL,
  created_at      DATETIME DEFAULT CURRENT_TIMESTAMP,
  created_by      INT NULL,
  updated_at      DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
  updated_by      INT NULL,
  deleted_at      DATETIME NULL,
  -- MySQL khong co partial index: dung cot sinh de unique chi khi chua xoa
  active_key      VARCHAR(260) GENERATED ALWAYS AS (IF(deleted_at IS NULL, CONCAT(provider,'|',subject,'|',tenant_id), NULL)) STORED,
  UNIQUE KEY uq_ext_login_uuid (uuid),
  UNIQUE KEY uq_ext_login_active (active_key),
  KEY ix_ext_login_tenant_user (tenant_id, user_id),
  KEY ix_ext_login_provider_subject (provider, subject)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- refresh token: them sso_sid, sso_sub (dung add_col_if_missing)
-- CALL add_col_if_missing('<bang refresh token hien tai>', 'sso_sid', 'VARCHAR(200) NULL');
-- CALL add_col_if_missing('<bang refresh token hien tai>', 'sso_sub', 'VARCHAR(200) NULL');
-- + index (sso_sid)
```

- FK tới bảng user: theo schema hiện có (khai báo FK nếu bảng user là InnoDB và cột `id INT`; nếu không thì ràng buộc ở application).
- Một `sub` có thể liên kết nhiều tenant (một người làm ở nhiều phòng khám) → unique theo `(provider, subject, tenant_id)`.
- Nhạy cảm: `subject`, `email_at_link` không thuộc nhóm bắt buộc AES-256-GCM (không phải CMND/BHYT/bệnh án); không mã hóa để tra cứu được. Không lưu id_token/access_token IdP vào DB; id_token dùng cho `id_token_hint` lưu trong Redis theo session (TTL = refresh lifetime) hoặc bỏ `id_token_hint` và dùng `client_id` (IS4 hỗ trợ hạn chế — cần xác nhận).
- Bảng là bảng hệ thống bảo mật, EF entity vẫn có `TenantId` + global query filter; luồng callback dùng truy vấn bỏ filter có chủ đích.
- Thêm vào `db/migrations/APPLY_ORDER.md`.

### 6.4 Audit (`diab_his_sec_audit_logs`)

`AUTH_SSO_LOGIN_START` (tùy chọn), `AUTH_SSO_LOGIN_SUCCESS`, `AUTH_SSO_LOGIN_DENIED` (reason code), `AUTH_SSO_LINK_CREATED`, `AUTH_SSO_LINK_REMOVED`, `AUTH_SSO_LOGOUT`, `AUTH_SSO_BACKCHANNEL_LOGOUT`, `AUTH_SSO_2FA_BYPASSED_BY_AMR`. Ghi `sub`, `sid`, `amr`, IP, UA; không ghi token.

### 6.5 Cấu hình env (`appsettings` / `.env`, KHÔNG commit secret)

```
Sso__Enabled=true
Sso__ProviderKey=diab-is
Sso__DisplayName=DiaB
Sso__Authority=https://is.diab.com.vn
Sso__ClientId=prodiab-his-staging            # prod: prodiab-his
Sso__ClientSecret=<secret>                     # từ secret store
Sso__RedirectUri=https://his.diab.vn/api/v1/auth/sso/callback
Sso__PostLogoutRedirectUri=https://his.diab.vn/login
Sso__FrontendBaseUrl=https://his.diab.vn
Sso__Scopes=openid profile email
Sso__RequireEmailVerified=true
Sso__TrustIdpMfa=false
Sso__TrustedMfaAmrValues=mfa,otp,hwk
Sso__EnforceSidRevocation=true
Sso__StateTtlSeconds=600
```

Nếu IdP không phát `email_verified` (Identity mặc định chỉ phát khi `EmailConfirmed`), cho phép tạm `Sso__RequireEmailVerified=false` **chỉ khi** chủ IdP xác nhận mọi email nhân viên đã được xác minh — xem câu hỏi mở Q1.

## 7. Thay đổi frontend HIS (Next.js 16)

- `app/(auth)/login/page.tsx`: nút "Đăng nhập bằng tài khoản DiaB" (gọi `/api/v1/auth/sso/config`; ẩn nếu disabled). Bấm → `window.location.href = ${API}/api/v1/auth/sso/login?returnUrl=...` (điều hướng toàn trang, không fetch).
- Hiển thị lỗi theo `?error=` từ `messages/vi.json` (`auth.sso.errors.SSO_USER_NOT_PROVISIONED`...).
- Không cần trang callback FE riêng vì callback nằm ở backend và 302 về `returnUrl`; tùy chọn `app/(auth)/sso/complete/page.tsx` để hiện trạng thái "Đang đăng nhập..." rồi gọi `/auth/me` khởi tạo store.
- Bước 2FA: tái dùng màn 2FA hiện có; backend truyền ticket qua cookie httpOnly ngắn hạn, không qua query string.
- Đăng xuất: gọi `POST /auth/sso/logout` rồi về `/login`. TUYỆT ĐỐI không chuyển tới `end_session_endpoint` của IS.
- Trang quản lý user: cột "Liên kết DiaB" + nút gỡ liên kết.
- Lưu ý middleware/proxy Next.js 16: route `/login?error=` và `/sso/complete` phải public.

## 8. Bảo mật

| Rủi ro | Biện pháp |
|--------|----------|
| CSRF login / code injection | `state` 32 byte ngẫu nhiên, lưu Redis, GETDEL một lần, TTL 10 phút; gắn với cookie `sso_bind` httpOnly (hash) để chống fixation |
| Replay id_token | `nonce` kiểm khớp; `jti`/exp ngắn |
| Đánh cắp code | PKCE S256 + client secret; redirect_uri khớp chính xác |
| Open redirect | `returnUrl` chỉ chấp nhận path tương đối bắt đầu bằng `/` và không bắt đầu `//` hoặc `/\`; mặc định `/dashboard` |
| Giả mạo issuer/khóa | Chỉ tin discovery của `Sso__Authority` qua HTTPS; validate `iss`, `aud`, alg RS256; cache JWKS và refresh khi lạ `kid` (giới hạn tần suất) |
| Chiếm tài khoản qua email | Bắt buộc `email_verified`; liên kết sau lần đầu theo `sub` (đổi email ở IdP không cướp được user khác); không ánh xạ nếu email trùng nhiều user cùng tenant |
| Leo thang quyền | Không dùng `role` từ IdP; admin vẫn 2FA |
| Logout không đồng bộ | RP-initiated + back-channel; access token HIS 15 phút + blacklist `sid` Redis |
| Back-channel giả | Validate logout_token: chữ ký, `iss`, `aud`, `iat` gần, `events` chứa `http://schemas.openid.net/event/backchannel-logout`, không có `nonce`, `jti` chưa dùng (Redis) |
| Token lifetime | id_token IdP 5 phút (chỉ dùng tại callback); access HIS 15 phút; refresh HIS giữ cấu hình hiện tại; IdP session theo portal |
| Rate limit | `/sso/login`, `/sso/callback` giới hạn theo IP (30/phút) |
| Cookie | `Secure`, `HttpOnly`, `SameSite=Lax` (callback là điều hướng top-level cross-site nên không dùng Strict cho cookie bind) |
| Lộ secret | `ClientSecret` chỉ trong `.env` server; không log |

## 9. Việc cần chủ hệ thống IdentityServer làm/duyệt

1. Tạo client `prodiab-his-staging`, `prodiab-his`, `diab-timesheet` (và tùy chọn `diab-timesheet-mobile`, `prodiab-his-dev`) theo bảng mục 5 trong ConfigurationStore SQL Server; gửi client secret qua kênh bí mật.
2. Xác nhận id_token/userinfo có `sub`, `email`, `email_verified`, `name`, `sid`, `amr`; nếu `email_verified` không phát hoặc sai, cập nhật ProfileService phát `email_verified` từ `EmailConfirmed`.
3. Xác nhận `IssuerUri` chính thức (`https://is.diab.com.vn`, không có dấu `/` cuối) khớp `issuer` trong discovery.
4. Bật back-channel logout cho client (IS4 hỗ trợ `BackChannelLogoutUri`) và kiểm tra IdP có outbound HTTPS tới his.diab.vn / his.diab.com.vn / timesheet.diab.com.vn.
5. Xác nhận CORS không cần (BFF gọi server-to-server); không cần thêm origin vào `AllowedHosts` CORS.
6. Cân nhắc lọc claim theo scope trong ProfileService (hiện phát mọi claim cho mọi client, gồm `GoogleEmail`, cờ liên kết) — giảm lộ dữ liệu.
7. (Tùy chọn) Bật MFA ở IdP và phát `amr=mfa` để admin HIS khỏi 2FA hai lần.
8. Thu hồi/xoay ClientSecret Zalo đang hard-code trong `Startup.cs`.
9. Lộ trình nâng cấp IdentityServer4 (EOL) — ngoài phạm vi nhưng ảnh hưởng an ninh dài hạn.

## 10. TimeSheet — thay đổi tương tự

Repo tìm được: `D:\diab\dev\git\pro-diab-ai\diab-timesheet` (gitlab `diab-website/diab-timesheet`). Stack: backend .NET 10 `src/Services/OperationsService` (MediatR, FluentValidation, Hangfire, JWT HMAC tự phát qua `JwtTokenService`, EF Core — code hiện dùng SQLite fallback, `MYSQL_CONN` chưa dùng), frontend React 19 + Vite, có `mobile/`. Giả định container `pro-docosan-ts-be/fe` trên VNG build từ repo này (chưa xác minh với DevOps). Không tìm thấy liên quan trong `pro-template/HR.API`.

- Backend: thêm `SsoController` `GET /api/auth/sso/login`, `GET /api/auth/sso/callback`, `POST /api/auth/sso/logout`, `POST /api/auth/sso/backchannel-logout`; cùng thư viện OIDC validate như HIS; ánh xạ email → user TimeSheet; phát JWT bằng `IJwtTokenService` hiện có.
- Bảng `user_external_logins (id, user_id, provider, subject, last_login_at, created_at, deleted_at)` — TimeSheet không multi-tenant (giả định) nên không có `tenant_id`; unique `(provider, subject)`.
- State/nonce: cần store chia sẻ (Redis hoặc bảng DB). Nếu chưa có Redis, dùng cookie mã hóa Data Protection chứa state/nonce/verifier (TTL 10 phút).
- Frontend React: nút "Đăng nhập bằng tài khoản DiaB" ở trang login, xử lý `?error=` ; nếu JWT TimeSheet hiện lưu ở localStorage, callback backend trả về qua cookie httpOnly hoặc một mã một-lần (`/sso/complete?ticket=`, đổi qua POST, TTL 60 giây) — không đưa JWT lên URL.
- CORS hiện `SetIsOriginAllowed(_ => true).AllowCredentials()` — nên siết lại trước khi bật SSO.
- Env: `Sso__Authority`, `Sso__ClientId=diab-timesheet`, `Sso__ClientSecret`, `Sso__RedirectUri`, `Sso__PostLogoutRedirectUri`, `Sso__FrontendBaseUrl`.
- Mobile (pha 2): AppAuth/expo-auth-session với client public `diab-timesheet-mobile` + PKCE, đổi id_token lấy JWT TimeSheet qua `POST /api/auth/sso/mobile-exchange` (validate như trên).

## 11. Test plan

Đơn vị (HIS `ProDiabHis.UnitTests`):
- Validate id_token: sai iss/aud/alg/nonce/hết hạn/sai chữ ký → từ chối.
- `returnUrl`: `https://evil.com`, `//evil.com`, `/\evil.com`, `javascript:` → về `/dashboard`.
- Ánh xạ: có liên kết theo sub; email khớp 1 user; khớp 0; khớp nhiều tenant; user khóa; email chưa verify.
- Admin: amr=pwd → yêu cầu 2FA; amr=mfa + TrustIdpMfa=true → bỏ qua; TrustIdpMfa=false → vẫn 2FA.
- logout_token: thiếu events, có nonce, jti lặp → 400.

Tích hợp (`ProDiabHis.IntegrationTests`, IdP giả lập bằng mock OIDC server/ WireMock với JWKS tự sinh):
- Luồng login đầy đủ → cookie HIS hợp lệ, `/auth/me` đúng tenant/role.
- State dùng lại lần 2 → `SSO_INVALID_STATE`.
- Back-channel logout → refresh token bị thu hồi, access token bị chặn khi bật sid revocation.
- Cross-tenant: user tenant A không thể vào tenant B dù cùng email ở IdP nếu không có user ở B.

E2E (Playwright, staging với `prodiab-his-staging` + tài khoản test trên IS):
- Đăng nhập SSO thành công (bác sĩ), bị chặn (email chưa cấp), admin qua 2FA, đăng xuất đồng bộ portal ↔ HIS, IdP sập → form mật khẩu vẫn chạy.
- Bổ sung vào `docs/qc/regression/regression.js` một bước kiểm `/api/v1/auth/sso/config`.

Bảo mật: thử open redirect, CSRF login (callback không có cookie bind), phát lại code, quét header cookie.

TimeSheet: bộ test tương đương ở mức đơn vị + E2E.

## 12. Bảo toàn phiên đăng nhập (yêu cầu bắt buộc)

### 12.1 IdentityServer / portal
- Client lưu trong **ConfigurationStore SQL Server** (đã xác minh trong `Startup.cs`: `AddConfigurationStore`, `AddInMemoryClients` bị comment). `AddClientStoreCache` cũng đang comment → IS đọc client từ DB mỗi request, **client mới có hiệu lực ngay, không cần restart**. Nếu sau này bật client store cache thì client mới có hiệu lực sau khi cache hết hạn — vẫn không cần restart.
- Cách đăng ký: script SQL chỉ `INSERT` vào `Clients`, `ClientGrantTypes`, `ClientRedirectUris`, `ClientPostLogoutRedirectUris`, `ClientScopes`, `ClientSecrets` trong một transaction; **không UPDATE/DELETE** dòng của client cũ; không sửa `IdentityResources`/`ApiScopes` dùng chung (scope `openid profile email` đã có sẵn). Backup các bảng cấu hình trước khi chạy.
- Không đụng: signing pfx, `IssuerUri`, cookie scheme, Data Protection key, `appsettings` của IS, `PersistedGrants`. Như vậy cookie phiên IS và token portal đã phát vẫn hợp lệ.
- Hạng mục 9.2 (sửa ProfileService phát `email_verified`) và 9.6 (lọc claim) **cần deploy lại IS** → tách thành đợt riêng, chỉ làm khi đã xác nhận IS lưu Data Protection key bền vững (DB/thư mục mount/Redis) và chạy cùng pfx; nếu key Data Protection đang nằm trong container ephemeral thì restart sẽ làm mất mọi phiên portal → **không làm**; thay vào đó HIS dùng phương án dự phòng (Q1). Pha 1 không yêu cầu deploy IS.
- Rollback: xóa đúng các dòng của client mới theo `ClientId` (client cũ không bị ảnh hưởng).

### 12.2 HIS
- Không đổi `Jwt__Secret/Issuer/Audience`, thời hạn access/refresh token, tên + thuộc tính cookie, thuật toán ký, định dạng refresh token. Khuyến nghị "access token 15 phút" ở mục 6.2 chỉ áp dụng nếu bằng giá trị hiện tại; **không đổi trong đợt này**.
- Claim mới (`amr=sso`, `sso_sid`) chỉ thêm vào token SSO; validator không bắt buộc claim mới → token cũ vẫn qua.
- Middleware kiểm `sso:revoked-sid` chỉ chạy khi token **có** claim `sso_sid`; token không có claim này bỏ qua.
- Migration 9221: chỉ `CREATE TABLE IF NOT EXISTS` + `ADD COLUMN ... NULL` (không default bắt buộc, không rebuild dữ liệu); MySQL 8 thêm cột nullable là INSTANT/INPLACE, không khóa bảng refresh token lâu. Không truncate/xóa refresh token.
- Back-channel logout: `UPDATE ... SET revoked_at=NOW() WHERE sso_sid=@sid` (hoặc `sso_sub=@sub AND sso_sid IS NOT NULL` khi logout_token không có sid). Tuyệt đối không thu hồi theo `user_id` → phiên mật khẩu cũ (`sso_sid IS NULL`) giữ nguyên.
- Logout HIS do người dùng bấm: chỉ thu hồi refresh token của phiên hiện tại như hiện nay.
- Deploy theo quy trình hiện có (`--no-deps --force-recreate` backend, restart nginx). Refresh token lưu DB nên recreate container không mất phiên. Kiểm tra Data Protection key của ASP.NET HIS (nếu có dùng cho cookie/antiforgery) đã persist qua volume; nếu chưa thì **không** thêm phụ thuộc mới vào Data Protection trong đợt này (state SSO để trong Redis, không trong cookie mã hóa).
- Feature flag `Sso__Enabled=false` mặc định khi deploy → bật sau khi test giữ session đạt.

### 12.3 TimeSheet
- Không đổi `JwtTokenService` (signing key/issuer/audience); SSO gọi lại đúng service đó.
- Bảng mới `user_external_logins` chỉ thêm; không đổi bảng user/token.
- Nếu TimeSheet thật sự chạy SQLite trong container (`operations.db` trong `AppContext.BaseDirectory`) thì recreate container có thể mất dữ liệu/phiên → **trước khi deploy phải xác minh volume**; nếu không có volume thì đây là rủi ro sẵn có, cần DevOps xử lý trước, không gộp vào đợt SSO.
- State/nonce: ưu tiên Redis/DB; nếu dùng cookie Data Protection thì key phải persist, nếu không chỉ làm hỏng luồng SSO đang dở (không ảnh hưởng phiên đăng nhập).
- Back-channel chỉ thu hồi phiên có liên kết SSO; JWT stateless cũ hết hạn tự nhiên.

### 12.4 Test "giữ session"
Chuẩn bị trước deploy (staging rồi production): đăng nhập sẵn các phiên — portal (trình duyệt A), HIS bằng mật khẩu (bác sĩ + admin 2FA, trình duyệt B), TimeSheet (trình duyệt C); lưu lại thời điểm và token hết hạn.

| # | Bước | Kỳ vọng |
|---|------|---------|
| GS1 | Chạy script đăng ký client trên IS | Trình duyệt A reload portal vẫn đăng nhập; mở `is.diab.com.vn` không bị hỏi lại mật khẩu; discovery/JWKS không đổi `kid` |
| GS2 | Áp migration 9221 + deploy backend/FE HIS (SSO tắt) | Trình duyệt B gọi API không 401; sau khi access token hết hạn, refresh thành công; không bị về `/login` |
| GS3 | Bật `Sso__Enabled=true` | Như GS2; form mật khẩu đăng nhập mới vẫn chạy |
| GS4 | Đăng nhập SSO ở trình duyệt D, rồi đăng xuất portal | D bị đăng xuất HIS; B (phiên mật khẩu cùng user) **vẫn** dùng được |
| GS5 | Deploy TimeSheet | Trình duyệt C không bị đá ra, refresh/gọi API bình thường |
| GS6 | Rollback (mục 12.5) | Phiên B, C, A vẫn dùng được; phiên SSO D (nếu có) có thể phải đăng nhập lại — chấp nhận |
| GS7 | Kiểm `jwks_uri` trước/sau | Tập khóa giống hệt |

Tự động hóa: thêm vào `docs/qc/regression/` kịch bản lưu cookie trước deploy (Playwright `storageState`) và chạy lại sau deploy.

### 12.5 Kế hoạch rollback
1. Mức 1 (nhanh nhất): `Sso__Enabled=false` + recreate backend → ẩn nút SSO, các endpoint SSO trả `SSO_DISABLED`; phiên cũ không ảnh hưởng.
2. Mức 2: quay lại image `prev-<ts>`. Bảng/cột mới để nguyên (nullable, code cũ bỏ qua) → **không rollback migration** để tránh động vào bảng refresh token.
3. Mức 3 (IS): xóa các client mới theo `ClientId` trong transaction; không đụng client cũ, không restart IS.
4. TimeSheet: tương tự mức 1–2.
Trước mỗi bước: backup DB HIS, backup bảng cấu hình IS, gắn tag image.

## 13. Rủi ro + câu hỏi mở

Rủi ro:
- R1: IdentityServer4 EOL, không còn bản vá bảo mật.
- R2: `email_verified` có thể không được phát/không đáng tin → nguy cơ ánh xạ sai; giảm thiểu bằng xác nhận từ chủ IdP và liên kết theo `sub`.
- R3: Email user HIS hiện có thể trống/trùng/không khớp email portal → tỷ lệ bị chặn cao lúc đầu; cần đối soát danh sách trước go-live.
- R4: Email HIS nếu được mã hóa (chỉ có blind index) → tra cứu phải qua blind index HMAC email; cần kiểm cột email có thuộc diện mã hóa không.
- R5: Back-channel logout phụ thuộc mạng IdP → HIS; nếu lỗi, phiên HIS tồn tại tới hết refresh token.
- R6: TimeSheet chưa rõ DB thực tế (SQLite fallback trong code) và chưa xác minh repo khớp container trên VNG.
- R8: Chưa biết Data Protection key của IS có lưu bền vững không → mọi việc cần restart IS (sửa ProfileService) có thể làm mất phiên portal; pha 1 tránh hoàn toàn.
- R9: TimeSheet có thể đang dùng SQLite không có volume → recreate container làm mất dữ liệu/phiên (rủi ro sẵn có).
- R7: Admin phải 2FA hai lớp (IdP không có MFA) → trải nghiệm kém nhưng an toàn.

Câu hỏi mở:
- Q1: IdP có phát `email_verified` không? Email nhân viên trên IS đã được xác minh hết chưa?
- Q2: Một người có thể có tài khoản ở nhiều tenant HIS — pha 1 chặn (`SSO_AMBIGUOUS_ACCOUNT`) hay làm luôn trang chọn phòng khám?
- Q3: ĐÃ CHỐT (user 17/10/2026): đăng xuất HIS/TimeSheet KHÔNG đăng xuất portal và app DiaB.
- Q4: Có kế hoạch bật MFA trên IdP không (để dùng `Sso__TrustIdpMfa`)?
- Q5: SSO áp dụng cho mọi tenant hay chỉ tenant nội bộ DiaB? Cần cờ `sso_enabled` theo tenant ngay pha 1?
- Q6: Domain chính thức production: `his.diab.com.vn` (tài liệu) vs `his.diab.vn` (staging hiện tại) — xác nhận.
- Q7: TimeSheet: repo `pro-diab-ai/diab-timesheet` có đúng là nguồn của container `pro-docosan-ts-be/fe`? Có multi-tenant không? Đang dùng DB nào?
- Q8: Ai giữ quyền ghi ConfigurationStore SQL Server của IS để đăng ký client (có UI admin hay phải chạy SQL)?


## 13. Ràng buộc cứng: tuyệt đối không đăng xuất người dùng portal và app DiaB (user chốt 17/10/2026)

- HIS và TimeSheet KHÔNG dùng RP-initiated logout: không gọi/redirect `end_session_endpoint`, không gọi `revocation_endpoint` với token của IS, không xoá grant/consent/persisted grant của người dùng trên IS.
- Không bật front-channel logout từ HIS/TimeSheet về phía IS; client mới đăng ký với `FrontChannelLogoutUri = null`.
- Back-channel logout chỉ là chiều IS → HIS/TimeSheet (portal đăng xuất thì HIS huỷ phiên SSO tương ứng); không có chiều ngược lại.
- Không restart, không redeploy, không đổi cấu hình dùng chung (signing key, Data Protection key, cookie, CORS, client cũ của portal/app mobile) của IdentityServer trong bất kỳ pha nào nếu chưa xác nhận Data Protection key lưu bền vững và chưa có phê duyệt riêng của user.
- Đăng ký client = INSERT bản ghi mới trong transaction; xoá client mới khi rollback cũng không đụng bản ghi khác.
- Test bắt buộc GS8: người dùng đang đăng nhập portal web + app mobile DiaB → đăng nhập rồi đăng xuất HIS qua SSO → portal và app vẫn đăng nhập, gọi API bình thường.


## 13. Ràng buộc cứng khi triển khai (user chốt 17/10/2026)

- **Hành vi mong muốn:** Single Sign-On + Single Logout. Đăng xuất HIS → RP-initiated logout tới `end_session_endpoint` → IS back-channel/front-channel logout các client còn lại (portal, TimeSheet). Đăng xuất portal → IS back-channel tới HIS, TimeSheet. Đây là hành vi người dùng chủ động, được phép.
- **Không được:** việc deploy/code/đăng ký client làm phát sinh TOÀN BỘ người dùng (portal, app mobile DiaB, HIS, TimeSheet) phải đăng nhập lại. Cụ thể: không restart/redeploy IS khi chưa xác nhận Data Protection key lưu bền vững; không đổi signing key, cookie scheme, client cũ của portal/app; HIS/TimeSheet giữ JWT secret/issuer/audience/refresh token; SSO deploy với `Sso__Enabled=false` rồi mới bật; migration chỉ thêm bảng/cột.
- Đăng ký client = INSERT bản ghi mới; rollback chỉ xoá đúng bản ghi đó.
- Test bắt buộc: GS1–GS7 (giữ phiên qua mỗi bước deploy) + SLO1 (đăng xuất HIS → portal và TimeSheet cùng phiên SSO bị đăng xuất) + SLO2 (đăng xuất portal → HIS/TimeSheet phiên SSO bị đăng xuất; phiên đăng nhập bằng mật khẩu riêng ở thiết bị khác không bị ảnh hưởng).
