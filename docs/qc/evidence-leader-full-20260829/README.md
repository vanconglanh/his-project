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

## Giới hạn — Browser E2E
Stack docker đang chạy là **production build của code TRƯỚC** (frontend `node server.js` standalone, không mount source; backend đã compile). Các feature MỚI chưa được deploy vào container đang chạy → chụp browser lúc này KHÔNG phản ánh code mới (sẽ gây hiểu lầm). Browser E2E cho từng feature mới cần **rebuild + redeploy 2 container** — để riêng bước deploy. Verification hiện tại dựa trên: build 0 error + 747 unit test pass + tsc 0 error + 3 migration apply/idempotent trên DB thật.
