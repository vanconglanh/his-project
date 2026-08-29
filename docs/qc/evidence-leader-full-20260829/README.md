# Evidence — Phiên Leader FULL (2026-08-29)

Verify cho: H-2→H-15 (trừ H-14), E/Đợt 2-3 handler, C bước 2. Commits `90ca998`→`bdd19b4` trên `develop`.

## 1. Build backend
```
dotnet build src/ProDiabHis.Api/ProDiabHis.Api.csproj
=> Build succeeded. 0 Warning(s), 0 Error(s)
```

## 2. Unit test (toàn bộ suite)
```
dotnet test tests/ProDiabHis.UnitTests
=> Passed!  - Failed: 0, Passed: 747, Skipped: 0, Total: 747
```
Đã tự fix 3 lỗi tích hợp phát sinh khi merge công việc song song:
- Constructor `SearchPatientsQueryHandler` thêm `IBranchProvider/IPermissionChecker/IAuditService` → cập nhật `PatientHandlersTests`.
- NSubstitute nested-`Returns` gotcha trong `LoginCommandHandlerTests` → tách biến.
- 2 test 2FA mới thiếu `Status = UserStatus.Active` → user bị query filter loại.

## 3. Typecheck frontend
```
cd frontend && npx tsc --noEmit
=> exit 0 (0 error)
```

## 4. Migration trên DB THẬT (docker MySQL local `prodiab-mysql`, DB `prodiab_his`)
Apply pass 1 + pass 2 (idempotent) đều KHÔNG lỗi. Verify sau apply:
```
perm cross_branch (patient.cross_branch_search + cross_branch_view): 2   [migration 9161]
perm service.price_override: 1                                          [migration 9165]
billing_items price_source col: 1                                       [migration 9165]
diab_his_bil_service_branch_prices table: tồn tại                        [migration 9165]
diab_his_tel_allowed_icd10 rows: 6 (seed ICD-10 telehealth)             [migration 9170]
```
→ 3 migration hợp lệ SQL + idempotent + seed đúng trên MySQL 8 thật.

## 5. H-4 (rủi ro pháp lý P0) — verify bằng đọc code
`DeletePatientCommandHandler` (`PatientCommandHandler.cs:262`) là **soft-delete**: set `DeletedAt`/`DeletedBy` + audit log, KHÔNG `DELETE FROM`. Query filter loại `DeletedAt != null`. KHÔNG vi phạm yêu cầu lưu trữ pháp lý.

## 6. Browser E2E trên docker REBUILD (bổ sung 2026-08-30)
Đã rebuild + redeploy cả backend + frontend (`ops/docker-compose.yml` + `docker-compose.local-app.yml`, images `prodiab-dev-*`) với TOÀN BỘ code mới, apply đủ migration 9161/9165/9170/9171/9172. Verify qua browser thật (login panel dev) + API live:

- **H-10 (2FA bắt buộc theo role)** — CRITICAL "không khoá nhầm tài khoản":
  - Login `qc.admin` (role admin) → `accessToken` PRESENT + `mfaSetupRequired=true` + message tiếng Việt → vào được Dashboard, **KHÔNG bị khoá** (soft-gate đúng thiết kế).
  - Login `bacsi.test` (role bac_si, không bắt buộc) → `accessToken` PRESENT + `mfaSetupRequired=false`, không message.
- **H-14 (gia hạn gói)** — E2E browser đầy đủ:
  - Seed 1 subscription `SUB-TEST-0001` status=expired còn định mức 3/5 + bật setting tenant 1 = 30 ngày.
  - Màn chi tiết bệnh nhân hiện block amber "Gói ... đã hết hạn nhưng còn định mức" + nút **Gia hạn**.
  - Click Gia hạn → toast "Đã gia hạn gói SUB-TEST-0001" → block biến mất (status → active). API xác nhận: expired 2026-06-30 → active 2026-09-28.
  - Guard: gọi extend khi status=active → 400 "Chỉ gia hạn được gói đang ở trạng thái hết hạn (expired)".
- **H-9 (QR thanh toán động)**:
  - `POST /billings/{id}/qr-dynamic` chưa cấu hình → 400 "Chưa cấu hình tài khoản nhận thanh toán" (guard đúng).
  - Sau khi cấu hình `bil.qr_bank_bin/account_no/account_name` → trả `amount=53025.00` (ĐỘNG theo hoá đơn) + `qr_payload` VietQR/EMVCo hợp lệ (BIN 970436, nội dung "TT HOA DON HD-...") + `qr_payload_image_base64` (PNG QR).
- **Endpoint mới khác** (deploy OK, trả 401 auth thay vì 404): `/stock-transfers`, `/service-price-overrides`, `/package-subscriptions/{id}/extend`.

### Bug phát hiện & sửa trong lúc E2E
`PackageEntitlementService.GetPatientSummaryAsync` lọc `status IN (active/suspended/exhausted/pending_payment)` → gói **expired** không bao giờ vào summary → nút Gia hạn (H-14) + badge "Gói sắp hết hạn" (H-13) không thể hiện. Đã thêm `'expired'` vào IN list (commit `343c4cc`), rebuild backend (no-cache do NuGet restore layer cache lỗi NETSDK1064), verify lại button hiện đúng.
