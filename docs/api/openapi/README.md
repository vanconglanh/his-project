# Pro-Diab HIS — OpenAPI Contract (Sprint 1, EPIC 1)

> **Scope:** Tenant + Users/RBAC + Roles/Permissions + Audit Log.
> **Source of truth** cho backend (Thảo) và frontend (Nam) triển khai song song.
> **Phiên bản:** 1.0.0 — 2026-05-23.

## Cấu trúc file

| File | Mô tả | #Endpoints | #Schemas |
|---|---|---|---|
| `_common.yaml` | Component dùng chung (security, error, paging, responses) | 0 | 3 (Error, PageMeta, PagedResult) |
| `tenants.yaml` | Quản lý phòng khám (SUPER_ADMIN + ADMIN tự quản trị) | 9 | 5 (TenantStatus, TenantResponse, CreateTenantRequest, UpdateTenantRequest, UpdateTenantProfileRequest) |
| `users.yaml` | Quản lý user, auth, 2FA, forgot/reset password | 18 | 8 (UserStatus, RoleRef, UserResponse, InviteUserRequest, AcceptInviteRequest, UpdateUserRequest, UpdateMeRequest, ChangePasswordRequest) |
| `roles.yaml` | Roles, Permissions, Audit Log | 7 | 7 (RoleType, RoleResponse, CreateRoleRequest, UpdateRoleRequest, PermissionResponse, AuditLogResponse, +ref) |

Tổng: **34 endpoint**, **23 schema** + 3 schema common.

## Convention chung

### Response envelope
```jsonc
// Success - single
{ "data": { ... } }

// Success - list
{ "data": [ ... ], "meta": { "page": 1, "page_size": 20, "total": 134, "total_pages": 7 } }

// Error
{ "error": { "code": "PATIENT_NOT_FOUND", "message": "Không tìm thấy bệnh nhân", "details": {} } }
```

### Naming
- Path: `kebab-case` (`/audit-logs`, `/accept-invite`).
- Schema: `PascalCase`.
- JSON field: `snake_case`.
- Enum: `SCREAMING_SNAKE_CASE`.

### Auth
- Mọi endpoint mặc định yêu cầu `bearerAuth` (JWT).
- Endpoint public override `security: []`: `accept-invite`, `forgot-password`, `reset-password`.
- JWT claims: `sub`, `tenant_id`, `clinic_id`, `roles[]`, `permissions[]`.
- Extension OpenAPI `x-required-role` và `x-required-permission` đánh dấu quyền cần thiết.

### Error codes chuẩn

| Code | Message |
|---|---|
| `AUTH_INVALID_CREDENTIALS` | Email hoặc mật khẩu không đúng |
| `AUTH_TOKEN_INVALID` | Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại |
| `PERMISSION_DENIED` | Bạn không có quyền thực hiện thao tác này |
| `TENANT_NOT_FOUND` | Không tìm thấy phòng khám |
| `TENANT_SUBDOMAIN_TAKEN` | Subdomain đã được sử dụng |
| `USER_EMAIL_EXISTS` | Email đã được đăng ký |
| `USER_INVITE_EXPIRED` | Liên kết mời đã hết hạn |
| `PASSWORD_TOO_WEAK` | Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt |
| `ROLE_SYSTEM_PROTECTED` | Không thể xóa vai trò hệ thống |
| `TWO_FA_INVALID_CODE` | Mã xác thực 2 lớp không đúng |
| `TWO_FA_ALREADY_ENABLED` | Xác thực 2 lớp đã được kích hoạt |

## Mapping story → endpoint

### Tenant (US-T01..T08)

| Story | Endpoint | File |
|---|---|---|
| US-T01 Tạo tenant mới | `POST /api/v1/tenants` | tenants.yaml |
| US-T02 Cấu hình BHYT/CSKCB | `PUT /api/v1/tenants/me` (bhyt_token, cskcb_code) | tenants.yaml |
| US-T03 Suspend tenant | `POST /api/v1/tenants/{id}/suspend` | tenants.yaml |
| US-T04 Terminate tenant | `DELETE /api/v1/tenants/{id}` | tenants.yaml |
| US-T05 List tenant | `GET /api/v1/tenants` | tenants.yaml |
| US-T06 Get detail | `GET /api/v1/tenants/{id}`, `GET /api/v1/tenants/me` | tenants.yaml |
| US-T07 Update profile | `PUT /api/v1/tenants/{id}`, `PUT /api/v1/tenants/me` | tenants.yaml |
| US-T08 Storage quota / subdomain | bao trong `CreateTenantRequest` + `UpdateTenantRequest` | tenants.yaml |
| (bonus) Activate lại | `POST /api/v1/tenants/{id}/activate` | tenants.yaml |

### Users & RBAC (US-U01..U10)

| Story | Endpoint | File |
|---|---|---|
| US-U01 Invite user qua email | `POST /api/v1/users/invite` | users.yaml |
| US-U02 Accept invite | `POST /api/v1/users/accept-invite` | users.yaml |
| US-U03 List user | `GET /api/v1/users` | users.yaml |
| US-U04 Get/Update profile | `GET /api/v1/users/{id}`, `PUT /api/v1/users/{id}`, `GET/PUT /api/v1/users/me` | users.yaml |
| US-U05 Đổi password | `POST /api/v1/users/me/change-password` | users.yaml |
| US-U06 Reset password | `POST /api/v1/auth/forgot-password`, `POST /api/v1/auth/reset-password` | users.yaml |
| US-U07 Gán role | `POST /api/v1/users/{id}/roles` | users.yaml |
| US-U08 Revoke role | `DELETE /api/v1/users/{id}/roles/{roleCode}` | users.yaml |
| US-U09 List role | `GET /api/v1/roles`, `GET /api/v1/roles/{code}` | roles.yaml |
| US-U10 Permission matrix | `GET /api/v1/permissions` | roles.yaml |
| (bonus) Lock/Unlock user | `POST /api/v1/users/{id}/disable\|enable` | users.yaml |
| (bonus) Soft delete user | `DELETE /api/v1/users/{id}` | users.yaml |
| (bonus) 2FA setup/enable/disable | `POST /api/v1/users/me/2fa/*` | users.yaml |
| (bonus) Audit log viewer | `GET /api/v1/audit-logs` | roles.yaml |
| (bonus) Tenant tự tạo custom role | `POST/PUT/DELETE /api/v1/roles[/{code}]` | roles.yaml |

### Placeholder
- **US-SUNS-22** Portal account (super-admin portal riêng) — chưa cover trong sprint này, sẽ tách module `portal-admin` ở sprint sau (suy nghĩ: SSO riêng, không chung bảng `sec_users`).

## Permission matrix (system roles)

| Role | Permissions chính |
|---|---|
| `SUPER_ADMIN` | `*` (toàn hệ thống, cross-tenant; bypass RLS qua claim `is_super_admin=true`) |
| `ADMIN` | `tenant.read/write` (own), `user.*`, `role.read/write`, `audit.read` |
| `BACSI` | `patient.read`, `encounter.*`, `prescription.create/sign`, `lab.read`, `emr.*` |
| `DIEUDUONG` | `patient.read`, `encounter.read`, `vital_sign.*` |
| `LETAN` | `patient.*`, `encounter.create`, `appointment.*`, `cls_upload.create` |
| `DUOCSI` | `prescription.read`, `pharmacy.*`, `drug.*` |
| `KETOAN` | `billing.*`, `report.read` |
| `KYTHUATVIEN` | `lab.*`, `rad.*` |

Danh sách permission đầy đủ sẽ được seed ở migration 0021 (xem ghi chú backend).

## Mapping DB

| Endpoint group | Bảng |
|---|---|
| Tenants | `diab_his_sys_tenants` (migration 0001 đã có) |
| Users | `sec_users` (cũ) — **cần migration 0020** thêm cột `avatar_url`, `two_fa_secret`, `two_fa_enabled`, `two_fa_recovery_codes`, `invite_token`, `invite_token_expires_at`, `status` (enum PENDING/ACTIVE/LOCKED/DISABLED) |
| Roles | `sec_roles` (seed 7 role ở migration 0018) |
| Permissions | `sec_permissions` — **cần seed migration 0021** với danh sách permission system |
| User ↔ Role | `sec_user_roles` |
| Role ↔ Permission | `sec_role_permissions` |
| Audit | `sec_audit_logs` |

## Ghi chú cho Backend (Thảo)

1. **Migration 0020** (idempotent với `add_col_if_missing`): bổ sung cột vào `sec_users`:
   - `avatar_url TEXT NULL`
   - `two_fa_secret TEXT NULL` (AES-256-GCM)
   - `two_fa_enabled BOOLEAN NOT NULL DEFAULT FALSE`
   - `two_fa_recovery_codes JSONB NULL` (mã hoá)
   - `invite_token TEXT NULL`
   - `invite_token_expires_at TIMESTAMPTZ NULL`
   - `status VARCHAR(16) NOT NULL DEFAULT 'PENDING'` + CHECK in ('PENDING','ACTIVE','LOCKED','DISABLED')
   - Index: `(tenant_id, status)`, unique `(invite_token)` partial WHERE invite_token IS NOT NULL.
2. **Migration 0021**: seed `sec_permissions` (~40 permission `resource.action`), seed `sec_role_permissions` cho 7 role hệ thống theo bảng matrix.
3. **Mã hoá AES-256-GCM** áp dụng cho: `two_fa_secret`, `two_fa_recovery_codes`, `bhyt_token` (trong `diab_his_sys_tenant_integration`).
4. **RLS**: mọi query `sec_users`, `sec_user_roles`, `sec_audit_logs`, custom `sec_roles` (CUSTOM) phải bị filter `tenant_id = current_setting('app.current_tenant')::uuid`. Role SYSTEM (`tenant_id IS NULL`) bypass filter (cho phép đọc cross-tenant).
5. **Audit hook**: middleware ghi log với action CREATE/UPDATE/DELETE/LOGIN/EXPORT/SIGN. Lưu `details` JSON ghi diff field.
6. **Endpoint forgot-password luôn 204** kể cả email không tồn tại (chống enumeration).
7. **Background job**: gửi email invite + reset password qua Hangfire/Quartz, queue `email`.

## Ghi chú cho Frontend (Nam)

1. **Token lifecycle**: access token 15 phút, refresh token 7 ngày. Interceptor auto refresh khi 401 + `code=AUTH_TOKEN_INVALID`.
2. **Permission gate**: dùng `permissions[]` trả từ `GET /users/me` để toggle menu/button (`<Can permission="user.invite">`).
3. **Invite flow**: link email `https://{subdomain}.prodiab.vn/accept-invite?token=...` → page form set password → call `POST /users/accept-invite` → auto login (nhận access/refresh token).
4. **2FA UI**: 3 bước — Setup (hiện QR base64 + secret để copy), Enable (nhập 6 số) → hiển thị 10 recovery code (yêu cầu user lưu/in), Disable (form password + code).
5. **Audit log table**: filter `user`, `action`, `resource_type`, date range; render `details` JSON dạng tree view.
6. **i18n**: tất cả message lỗi BE đã trả tiếng Việt, FE chỉ map theo `code` khi cần override.

## Validate spec

```powershell
# Cài redocly
npm i -g @redocly/cli
# Validate
redocly lint docs/api/openapi/tenants.yaml
redocly lint docs/api/openapi/users.yaml
redocly lint docs/api/openapi/roles.yaml
# Render preview
redocly preview-docs docs/api/openapi/users.yaml
```
