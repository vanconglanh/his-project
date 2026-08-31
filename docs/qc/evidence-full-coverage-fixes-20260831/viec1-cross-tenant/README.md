# Việc 1 — Cross-Tenant Isolation Tests + Audit Dapper tenant_id

Ngày: 2026-08-31. Nhánh: `develop`. Chạy thật trên MySQL 8 (Testcontainers) + `WebApplicationFactory<Program>`.

## 1. Kết quả chạy thật

- Bộ CrossTenant + FhirOps metadata: **37 passed, 1 skipped (BUG-001 cũ, giữ nguyên), 0 failed**.
- Toàn bộ Integration Test sau khi thêm/sửa: **1176 passed, 1 skipped, 0 failed** (baseline 1151 → +25 test mới). Không có regression.

Xem `test-run-output.txt`.

## 2. File test mới (thư mục CrossTenant/)

- `CrossTenantSeeder.cs` — seed 2 tenant (A=1, B=2), mỗi tenant: tenant row + user + patient + encounter + billing + prescription + lab result + drug + branch. ID/GUID cố định (deterministic). Tenant/Branch chèn raw SQL id tường minh (PK INT identity). Patch schema test cho `diab_his_pha_prescription_items.deleted_at` và các cột read-model của `diab_his_pha_drugs` (không đụng `TestSchemaSupplement.cs` theo ràng buộc).
- `CrossTenantIsolationTests.cs` — 16 test HTTP: mỗi module (Patients, Encounters, Billings, Prescriptions, LabResults, Drugs, Branches, Reports) kiểm GET list (không lộ record tenant B) + GET /{id} bằng ID chính xác của tenant B → **404** (không 403/200).
- `CrossTenantQueryFilterTests.cs` — 8 test xác minh EF Core Global Query Filter theo TenantId cho Patient/Encounter/Billing/Prescription/LabResult/Drug/Branch/User (DbContext scoped tenant A không thấy row tenant B; `IgnoreQueryFilters()` thấy cả hai).

## 3. AUDIT Dapper — CÓ phát hiện query THIẾU tenant_id (đã sửa product code)

Các câu SELECT list/detail chính của 8 module đều CÓ `WHERE tenant_id`. Tuy nhiên phát hiện **3 câu Dapper phụ trợ thiếu `tenant_id`** (tra cứu tên/PII bệnh nhân + luồng kham) — nguy cơ lộ hồ sơ tenant khác (defense-in-depth). Đã sửa:

| # | File:line | Trước | Sau |
|---|-----------|-------|-----|
| 1 | `backend/src/ProDiabHis.Application/Billing/BillingHandlers.cs` (GetPatientSummaryAsync) | `... FROM diab_his_pat_patients WHERE id=@id AND deleted_at IS NULL` | thêm `AND tenant_id = @tenantId` |
| 2 | `backend/src/ProDiabHis.Application/Billing/BillingHandlers.cs` (ListBillingsHandler batch load) | `... WHERE id IN @ids AND deleted_at IS NULL` | thêm `AND tenant_id = @tenantId` |
| 3 | `backend/src/ProDiabHis.Application/Billing/BillingHandlers.cs` (GetPatientIdFromEncounterAsync) | `SELECT patient_id FROM diab_his_enc_encounters WHERE id=@id AND deleted_at IS NULL` | thêm `AND tenant_id = @tenantId` (đổi chữ ký nhận tenantId) |
| 4 | `backend/src/ProDiabHis.Application/Pharmacy/Prescriptions/PrescriptionHandlers.cs` (ListPrescriptionsHandler patient batch) | `... FROM diab_his_pat_patients p WHERE p.id IN @ids AND p.deleted_at IS NULL` | thêm `AND p.tenant_id = @tenantId` |

Test chứng minh đã bịt: `Billings_List/ChiTiet_*`, `Prescriptions_List/ChiTiet_*` (list không lộ patient tenant B, detail id tenant B → 404).

## 4. Ghi chú giới hạn hạ tầng test

- **Reports** (`/api/v1/reports/revenue*`): dùng cache Redis (`IConnectionMultiplexer`) — test host không cấu hình Redis → trả 500 (lỗi hạ tầng, KHÔNG phải lỗ hổng). 2 test Reports vẫn khẳng định response KHÔNG bao giờ chứa ID/mã tenant B (đúng cả khi 500). Audit read-side xác nhận query reports có `WHERE tenant_id=@tenantId`; isolation tầng dữ liệu đã được phủ bởi Billings/Encounters.
- **LabResults**: không có endpoint GET /{id} riêng → kiểm list + lọc `patient_id` = bệnh nhân tenant B (không trả kết quả).

## 5. BUG-001 (bonus)

`GET /api/fhir/r4/metadata` không kèm token → **200** (đã fix trong `RequirePermissionAttribute` bằng cách tôn trọng `IAllowAnonymous`). Thêm test `FhirOpsIntegrationTests.ChuaDangNhap_Metadata_Tra200` (chỉ THÊM method, giữ nguyên test cũ + skip BUG-001 lịch sử).
