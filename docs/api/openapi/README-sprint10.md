# Sprint 10 — EPIC 8: Public API + Push Notification + Patient Portal

Architect: Lành | DB: **MySQL 8** | Stories: US-SUNS-08..12, 16..18, 22

## 1. OpenAPI files

| File | Base path | Auth | Story |
|------|-----------|------|-------|
| `public-api.yaml` | `/api/public/v1/*` | `X-Api-Key` (hashed, scope, rate-limit) | US-SUNS-08, 09, 10 |
| `push-notifications.yaml` | `/api/v1/notifications/*` | JWT user | US-SUNS-16, 17 |
| `patient-portal.yaml` | `/api/portal/v1/*` | OTP -> JWT TTL 24h (`aud=patient-portal`) | US-SUNS-11, 12, 22 |
| `api-partners-mgmt.yaml` | `/api/v1/api-partners/*` | JWT admin | US-SUNS-18 |

## 2. Permission matrix

| Permission | Endpoint scope | Vai trò mặc định |
|------------|----------------|------------------|
| `api_partner.read` | GET /api-partners, /{id}, /usage-stats, /request-logs | Admin, KeToan |
| `api_partner.write` | POST/PUT/DELETE /api-partners | Admin |
| `api_partner.admin` | regenerate-key, test-call | Admin |
| `notification.read` | GET inbox, unread-count, GET/PUT preferences, web-push subscribe/unsubscribe | tất cả user nội bộ |
| `notification.admin` | (future) gửi thông báo thủ công, xem audit | Admin |
| `vapid.admin` | Quản lý VAPID key của tenant | Admin |

Portal endpoints không dùng permission RBAC nội bộ — gắn với `patient_id` từ JWT portal.

## 3. Public API scopes (gán cho từng API key)

- `public.patient.read`, `public.patient.write`
- `public.appointment.read`, `public.appointment.write`
- `public.catalog.read`
- `public.visit.lookup` (cần thêm OTP bệnh nhân)

## 4. Migration list (gửi Thảo)

| File | Mô tả |
|------|-------|
| `0048_create_appointments_extensions.sql` | ALTER `diab_his_sch_appointments` ADD `source_partner_id BINARY(16) NULL`, `partner_reference VARCHAR(100) NULL`, INDEX `idx_appt_partner (source_partner_id)` |
| `0049_api_partners_seed.sql` | Seed bảng `diab_his_api_scope_dict` (scope code + mô tả VN); seed permission `api_partner.*` |
| `0050_create_vapid_keys.sql` | CREATE `diab_his_nti_vapid_keys` (id, tenant_id BINARY(16) UNIQUE, public_key VARCHAR(255), private_key_encrypted VARBINARY(512), created_at, updated_at). AES-256-GCM cho `private_key_encrypted`. |
| `0051_portal_otp_extensions.sql` | ALTER `pat_portal_accounts` ADD `failed_attempts INT DEFAULT 0`, `locked_until DATETIME NULL`, `last_otp_sent_at DATETIME NULL` |
| `0052_seed_permissions_sprint10.sql` | INSERT 6 permission: api_partner.read/write/admin, notification.read/admin, vapid.admin |

### Bảng mới đề xuất (Thảo cần tạo migration 0046-0047 trước đó nếu chưa có)
- `diab_his_api_partners` (id, tenant_id, name, contact_email, api_key_hash, api_key_prefix, scopes JSON, rate_limit_per_min, daily_quota, status, expires_at, ip_whitelist JSON, audit cols)
- `diab_his_api_request_logs` (id, tenant_id, partner_id, method, path, status_code, duration_ms, ip, error_code, called_at) — partition theo tháng
- `diab_his_nti_notifications` (id, tenant_id, user_id, type, title, body, data_json JSON, read_at, created_at)
- `diab_his_nti_web_push_subs` (id, tenant_id, user_id, endpoint UNIQUE, p256dh_key, auth_key, user_agent, created_at)
- `diab_his_nti_preferences` (id, tenant_id, user_id UNIQUE, position, sound_enabled, sound_name, browser_push_enabled, types_disabled JSON)
- `diab_his_nti_vapid_keys` (xem 0050)
- `pat_portal_accounts` (đã có ở sprint trước — chỉ extend)
- `pat_portal_otp_log` (id, tenant_id, phone, otp_hash, purpose ENUM[LOGIN,LOOKUP], sent_at, verified_at, attempts)
- `pat_portal_sessions` (id, tenant_id, patient_id, jti, issued_at, expires_at, revoked_at)

**Multi-tenant:** mọi bảng có `tenant_id` + index theo `(tenant_id, ...)`. MySQL 8 không có RLS native -> dùng filter ở repository layer + middleware kiểm tra.

## 5. Services interface (cho Thảo implement)

- `IApiKeyAuthMiddleware` — extract `X-Api-Key`, hash SHA-256, lookup `diab_his_api_partners` by `api_key_hash`, validate scope/IP/expiry, gọi `IRateLimiter` (Redis sliding window: `apikey:{partner_id}:{minute}` và `apikey:{partner_id}:daily:{yyyymmdd}`)
- `ISmsGateway` — abstraction, impl: `SpeedSmsGateway`, `ViettelSmsGateway`, `EsmsGateway`, `MockSmsGateway` (dev). Config theo tenant.
- `IWebPushSender` — dùng `WebPush.NET`, lấy VAPID từ `diab_his_nti_vapid_keys`, gửi async qua background queue. Xử lý 404/410 -> remove subscription.
- `IPortalAuthService` — request OTP (6 digit, hash bcrypt, TTL 5 phút, throttle 5 req/giờ/phone), verify OTP (max 5 attempts), issue JWT (`aud=patient-portal`, claim `patient_id`, `tenant_id`, TTL 24h), logout (revoke jti vào Redis blacklist)
- `IRateLimiter` — Redis sliding window, INCR + EXPIRE pattern hoặc Lua script

## 6. Error codes (message tiếng Việt — đã embed trong từng spec)

API auth: `API_KEY_INVALID`, `API_KEY_EXPIRED`, `API_SCOPE_DENIED`, `API_RATE_LIMITED`, `API_QUOTA_EXCEEDED`
Appointment: `APPOINTMENT_SLOT_TAKEN`, `APPOINTMENT_NOT_FOUND`, `APPOINTMENT_CANCEL_TOO_LATE`
Web Push: `WEB_PUSH_INVALID_SUBSCRIPTION`, `VAPID_KEY_NOT_CONFIGURED`
Portal OTP: `PORTAL_OTP_INVALID`, `PORTAL_OTP_EXPIRED`, `PORTAL_OTP_TOO_MANY_ATTEMPTS`, `PORTAL_PHONE_NOT_REGISTERED`, `PORTAL_SESSION_EXPIRED`

## 7. Trường nhạy cảm cần mã hóa AES-256-GCM

- `diab_his_api_partners.api_key_hash` — SHA-256 (one-way, không mã hóa)
- `diab_his_nti_vapid_keys.private_key_encrypted` — AES-256-GCM
- `pat_portal_otp_log.otp_hash` — bcrypt
- `pat_portal_accounts.phone` — có thể giữ plain để query, nhưng audit log không log
- Integration tokens đối tác lưu plain trong `diab_his_api_partners` KHÔNG có (chỉ giữ hash) — partner tự giữ key

## 8. FHIR mapping

- Portal `PortalEncounterResponse` <-> `Encounter` + `Condition` (diagnosis)
- Portal `PortalPrescriptionResponse` <-> `MedicationRequest`
- Portal lab result <-> `Observation` (category=laboratory) / `DiagnosticReport`
- Public appointment <-> `Appointment` resource

## 9. ADR cần thiết (Lành sẽ viết tiếp nếu cần)

- ADR-010: Chọn JWT vs opaque token cho Patient Portal -> chọn JWT (stateless, có revoke list Redis)
- ADR-011: Rate limit Redis sliding window vs token bucket -> chọn sliding window cho fairness
- ADR-012: VAPID key per-tenant vs global -> chọn per-tenant (tách rủi ro, branding riêng)
