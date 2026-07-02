# Architect Review — 2026-05-31

> Reviewer: Lành (architect) · Stack: .NET 8 / MySQL 8 / Next.js 15
> Input: `crud-evidence.md` (22/42 PASS), `all-routes-evidence.md` (29/29), `patient-journey-evidence.md` (9/9)
> Scope: API contract vs DTO vs FE, schema 4 module FAIL, FK consistency, deploy readiness

## 1. API / DTO / FE shape check (5 endpoint chính)

| Endpoint | BE response (controller) | FE interface (`lib/api/types.ts`) | Case | Pagination meta | Verdict |
|---|---|---|---|---|---|
| `POST /api/v1/auth/login` | `{data: LoginResponse, meta:{}}`; `LoginResponse` dùng `[JsonPropertyName]` camelCase | `LoginResponse {accessToken, refreshToken, expiresIn, user, permissions}` | camelCase (cố ý) | n/a | OK — khớp |
| `GET /api/v1/patients` | `{data: Items, meta:{page, page_size, total, total_pages}}` | `ApiMeta {page,page_size,total,total_pages}` + `PatientResponse` snake_case | snake_case | đầy đủ 4 field | OK |
| `GET /api/v1/encounters` | `{data, meta:{page, page_size, total}}` — thiếu `total_pages` (controller line 39) | `ApiMeta` yêu cầu `total_pages` | snake_case | **THIẾU `total_pages`** | LỆCH |
| `GET /api/v1/prescriptions` | (assumed pattern, không kiểm tra chi tiết) | `unknown[]` (FE chưa định nghĩa interface mạnh) | snake_case | cần verify | WARN |
| `GET /api/v1/billings` | snake_case `{data, meta}` | n/a (FE dùng dynamic) | snake_case | cần verify `total_pages` | WARN |

**Khẳng định:** quy ước chung — `/auth/*` giữ camelCase qua `JsonPropertyName`; mọi controller khác trả snake_case nhờ default `JsonNamingPolicy.SnakeCaseLower` (Program.cs).

## 2. Issues (file:line + đề xuất)

### ISSUE-ARCH-01 (minor) — Pagination meta không đồng nhất
- Controllers thiếu `total_pages` trong response meta:
  - `backend/src/ProDiabHis.Api/Controllers/EncountersController.cs:39`
  - `backend/src/ProDiabHis.Api/Controllers/PatientsController.cs:116` (sub-resource encounters)
  - `backend/src/ProDiabHis.Api/Controllers/CashierController.cs:80,121`
  - `backend/src/ProDiabHis.Api/Controllers/ServicesController.cs:31,126`
  - `backend/src/ProDiabHis.Api/Controllers/BhytExportController.cs:48,167`
- FE `ApiMeta` (lib/api/types.ts:3-8) khai báo `total_pages` là **required**.
- Fix: extension method `PagedResult.ToMeta()` đảm bảo luôn có 4 field. Hoặc `total_pages: (int)Math.Ceiling((double)total/page_size)` ở mọi chỗ. SuppliersController:30 đã làm đúng — copy pattern này.

### ISSUE-ARCH-02 (major) — `GET /api/v1/cashier/shift` trả stub `null`
- `CashierController.cs:95-102` trả `{data: null}` — TODO chưa implement.
- Hệ quả: FE `/cashier` page có thể hang/timeout nếu hardcode `useQuery` với select trên `data.xxx`.
- Fix: implement `GetCurrentShiftQuery` trả về shape `{ shift_id, status, opened_at, opening_balance }` hoặc `{ status: "NONE" }`. Đồng thời FE phải handle `null` an toàn.

### ISSUE-ARCH-03 (info) — BUG-CRUD-01 BHYT/Supplier/ServiceCatalog FAIL = navigation timeout, không phải lỗi backend
- `all-routes-evidence.md` xác nhận 4 route đều render OK 200 trong walker. Tức endpoint backend không 5xx.
- Nguyên nhân thực: Next.js cold start (BUG-CRUD-02, 40s) + test timeout 20s. Backend không cần sửa gấp.
- Fix: tăng timeout login/page.goto lên 60s trong CRUD spec; warm-up bằng `playwright global-setup`.

### ISSUE-ARCH-04 (minor) — Schema 4 module FAIL: index đủ cho query hot
- `diab_his_bil_billing` (0041_billing_extensions.sql:31-33): có `idx_billing_tenant_patient`, `idx_billing_encounter`, `idx_billing_status`. **BHYT export query** `WHERE tenant_id AND finalized_at BETWEEN ? AND ? AND payer IN ('BHYT','MIXED')` cần thêm:
  - `INDEX idx_billing_period_payer (tenant_id, finalized_at, payer)` — đề xuất migration `9021_perf_bhyt_indexes.sql`.
- `diab_his_bil_cashier_shifts` (0043:31-33): có 3 index cần thiết, OK.
- `diab_his_bil_services` (9006b/0040): cần verify có `INDEX (tenant_id, is_active, category)` cho ServiceCatalog list — chưa thấy trong 9016. Đề xuất bổ sung.
- `diab_his_pha_suppliers` (9005:185): cần `INDEX (tenant_id, status)` cho list filter. Verify.

### ISSUE-ARCH-05 (info) — FK type consistency: PASS
- `diab_his_pat_patients.id` = `CHAR(36)` (UUID)
- `diab_his_enc_encounters.patient_id` = `CHAR(36)`
- `diab_his_bil_billing.patient_id` = `CHAR(36)`, `encounter_id` = `CHAR(36)`
- `diab_his_int_bhyt_export_items` (0012): cần spot-check nhưng pattern UUID đồng bộ.
- `tenant_id` toàn bộ là `INT` — chính xác theo CLAUDE.md §3.
- **Mismatch duy nhất:** `UserInfo.TenantId` trong `LoginResponse.cs:17` là `int` nhưng FE `UserProfile.tenantId: number` + `clinicId: number` — khớp INT, OK. Tuy nhiên các DTO khác (`UserResponse.tenant_id: string` lib/api/types.ts:144) đang khai báo string — cần thống nhất: tenant_id luôn **number** (INT), không phải UUID. **Fix FE:** đổi `tenant_id: string` → `tenant_id: number` trong UserResponse, TenantResponse cũng cần `id: number` thay vì `string`.

### ISSUE-ARCH-06 (minor) — `BhytExportController` dùng `int id` thay UUID
- `BhytExportController.cs:52,62,76,...` — `{id:int}` cho bhyt exports, nhưng các bảng nghiệp vụ khác đều `Guid`. Không sai (bảng `diab_his_int_bhyt_exports` có thể dùng AUTO_INCREMENT INT theo schema legacy), nhưng KHÔNG nhất quán với pattern `CHAR(36) UUID` ở mục 3 CLAUDE.md. Đề xuất ghi ADR giải thích lý do dùng INT (period-locked, không cần phân tán).

## 3. Recommendation

**Deploy staging: GO** với các caveat:
1. ISSUE-ARCH-02 (cashier shift stub) — FE phải handle null trước khi deploy demo; backend implement query trong sprint kế tiếp.
2. ISSUE-ARCH-01 (total_pages) — non-blocking nếu FE đang dùng `meta?.total_pages ?? Math.ceil(total/page_size)`. Cần audit FE consumer; nếu không có fallback → fix BE trước deploy.
3. ISSUE-ARCH-04 (BHYT index) — chỉ cần khi data > 10k bill/tháng. Có thể chờ.
4. ISSUE-ARCH-05 (tenant_id type FE) — fix trong sprint cleanup typings (không block runtime vì TS bị as-cast).

**Production: HOLD** đến khi:
- ISSUE-ARCH-02 implement đầy đủ
- ISSUE-ARCH-01 fix toàn bộ controller (đảm bảo contract stability)
- Có ADR cho ISSUE-ARCH-06

## 4. Artifacts liên quan

- `backend/src/ProDiabHis.Api/Controllers/*.cs` (5 controllers reviewed)
- `frontend/lib/api/types.ts` (FE interface source-of-truth)
- `db/migrations/0041_billing_extensions.sql`, `0043_cashier_shifts.sql`, `0040_service_catalog.sql`, `9005_create_pharmacy.sql`, `9016_perf_indexes.sql`
- Đề xuất migration mới: `db/migrations/9021_perf_billing_service_supplier_indexes.sql` (chưa tạo trong review này)
