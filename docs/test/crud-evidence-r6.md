# CRUD Evidence R6 - Pro-Diab HIS

**Ngày chạy:** 2026-06-01
**Tester:** Phượng (QA)
**Môi trường:** Docker MySQL 8.0 (prodiab-mysql healthy) + Redis 7 (prodiab-redis healthy) + BE .NET 8 (localhost:5000, ASPNETCORE_ENVIRONMENT=Development) + FE Next.js 15 (localhost:3000)
**Account:** admin@prodiab.local / Admin@123
**Spec chạy:** `e2e/crud-actions.spec.ts` + `e2e/reports-cohort-fix.spec.ts`

---

## 1. Tóm tắt điều hành (Executive Summary)

| Vòng  | PASS | SKIP | FAIL | Tổng | Tỉ lệ PASS | Delta vs vòng trước |
|-------|------|------|------|------|-----------|---------------------|
| R4    | 32   | 16   | 0    | 48   | 66.7%     | baseline            |
| R5    | 37   | 19   | 0    | 56   | 66.1%     | +5 PASS, +8 action  |
| **R6**| **39** | **17** | **0** | **56** | **69.6%** | **+2 PASS, -2 SKIP** |

**Mục tiêu R6:** ≥ 45/56 (≥ 80%).
**Thực tế R6:** 39/56 (69.6%).
**Verdict:** **FAIL** mục tiêu 80%, nhưng **đạt tiến bộ +2 action** sau commit FE `c8f99f5` (10 P1 fix).

**Reports cohort spec:** 1 PASS / 1 FAIL / 2 did not run = **1/4** (yêu cầu 4/4 - **FAIL**).

---

## 2. Module Overview (15 module)

| # | Module          | PASS | SKIP | Tổng | Ghi chú R6                                  |
|---|-----------------|------|------|------|---------------------------------------------|
| 1 | Patient         | 5    | 0    | 5    | Full CRUD OK                                |
| 2 | Encounter       | 3    | 2    | 5    | ViewDetail + CloseEncounter vẫn SKIP        |
| 3 | Reception       | 2    | 1    | 3    | PrintTicket SKIP                            |
| 4 | Prescription    | 2    | 2    | 4    | AddDrugItem + Submit SKIP                   |
| 5 | Pharmacy Stock  | 3    | 1    | 4    | **TAB:Adjustment PASS (mới fix R6)**, CreateAdjustment vẫn SKIP |
| 6 | Pharmacy Dispense | 4 | 0    | 4    | Full PASS                                   |
| 7 | Drug            | 2    | 1    | 3    | Search SKIP                                 |
| 8 | Cashier         | 1    | 3    | 4    | Toàn bộ row action SKIP (thiếu data billing)|
| 9 | Billing         | 1    | 3    | 4    | Toàn bộ row action SKIP (thiếu data)        |
| 10| Service Catalog | 2    | 1    | 3    | UpdatePrice SKIP                            |
| 11| BHYT            | 2    | 1    | 3    | ViewDetail vẫn SKIP (chưa nhận button text "Chi tiết") |
| 12| Admin Users     | 4    | 0    | 4    | Full PASS                                   |
| 13| Admin Roles     | 2    | 1    | 3    | **EditPermissions PASS (mới fix R6)**       |
| 14| Admin Tenants   | 3    | 0    | 3    | **SuspendActivate PASS (mới fix R6)**, full module green |
| 15| Supplier        | 2    | 1    | 3    | Update SKIP                                 |
|   | **TỔNG**        | **39** | **17** | **56** | |

---

## 3. Trạng thái 10 P1 (commit FE c8f99f5)

| # | Action                                | Trước R6 | Sau R6 | Kết quả |
|---|---------------------------------------|----------|--------|---------|
| 1 | PharmacyStock :: TAB:Adjustment       | SKIP     | PASS   | ✓ FIX OK (label tab "Điều chỉnh") |
| 2 | PharmacyStock :: CreateAdjustment     | SKIP     | SKIP   | ✗ button "Tạo điều chỉnh" chưa được spec phát hiện |
| 3 | AdminRoles :: EditPermissions         | SKIP     | PASS   | ✓ FIX OK (button "Sửa quyền" inline) |
| 4 | AdminTenants :: SuspendActivate       | SKIP     | PASS   | ✓ FIX OK (button "Tạm ngưng"/"Kích hoạt" inline) |
| 5 | BHYT :: ViewDetail                    | SKIP     | SKIP   | ✗ button "Chi tiết" chưa khớp selector spec |
| 6 | Cashier :: OpenBill                   | SKIP     | SKIP   | ✗ thiếu seed billing |
| 7 | Cashier :: ReceivePayment             | SKIP     | SKIP   | ✗ thiếu seed billing |
| 8 | Cashier :: PrintReceipt               | SKIP     | SKIP   | ✗ thiếu seed billing |
| 9 | Billing :: ReceivePayment             | SKIP     | SKIP   | ✗ thiếu seed billing |
| 10| Billing :: PrintInvoice               | SKIP     | SKIP   | ✗ thiếu seed billing |

**Kết quả:** 3/10 P1 chuyển từ SKIP -> PASS. 7/10 còn SKIP.
- 5/7 là vấn đề **seed data** (Cashier/Billing chưa có row FINALIZED|PARTIAL_PAID trong DB sạch sau khi rebuild MySQL volume).
- 2/7 (CreateAdjustment, BHYT ViewDetail) là vấn đề **selector mismatch** giữa text mới và regex trong spec - cần cập nhật spec hoặc đồng bộ text button.

---

## 4. Reports Cohort Fix (4 TC)

| TC   | Tên                                      | Kết quả |
|------|------------------------------------------|---------|
| TC01 | (BeforeAll login + setup)                | PASS    |
| TC02 | Clinical tab cohort card render          | **FAIL** - không thấy giá trị mock 348/87 |
| TC03 | Dashboard cohort widget khong crash      | did not run (sau fail) |
| TC04 | Network request cohort?dm_type=ALL       | did not run |

**Tỉ lệ:** 1/4 = 25%. Yêu cầu 4/4 -> **FAIL**.
Screenshot lỗi: `frontend/test-results/reports-cohort-fix-reports-f1fe1-ical-tab-cohort-card-render-chromium/test-failed-1.png`.

---

## 5. Còn lại (P2 backlog)

| Module       | Action chưa PASS                  | Khuyến nghị owner |
|--------------|-----------------------------------|-------------------|
| Encounter    | ViewDetail, CloseEncounter        | frontend          |
| Reception    | PrintTicket                       | frontend          |
| Prescription | AddDrugItem, Submit               | frontend          |
| Pharmacy     | CreateAdjustment                  | frontend (selector) |
| Drug         | Search                            | frontend          |
| Cashier      | OpenBill, ReceivePayment, PrintReceipt | po-analyst + backend (seed/data flow) |
| Billing      | ViewBill, ReceivePayment, PrintInvoice | po-analyst + backend (seed/data flow) |
| Service      | UpdatePrice                       | frontend          |
| BHYT         | ViewDetail                        | frontend (selector text) |
| AdminRoles   | CREATE                            | frontend          |
| Supplier     | Update                            | frontend          |
| Reports      | Cohort TC02-04                    | frontend (mock data binding) |

---

## 6. Blocker

- **Cashier/Billing 6 SKIP** không thể giải quyết bằng FE — yêu cầu seed dữ liệu `billing` với status `FINALIZED|PARTIAL_PAID` trong DB test.
- **Reports cohort TC02 FAIL** - mock data 348/87 không render -> cần dev FE kiểm tra binding Clinical tab cohort card.

---

**File này lưu UTF-8 BOM, tiếng Việt theo CLAUDE.md §6.**
