# Smoke Test Matrix — Pro-Diab HIS

> Phiên test: **2026-05-23**
> Base URL: `http://localhost:5000` (API) / `http://localhost:3000` (UI)
> Auth: `admin@prodiab.local` / `Admin@123` → JWT lưu biến `$TOKEN`
> Người chạy: Phượng (tester) — Test lead: Lành
> Mục tiêu: smoke CRUD ~36 module, đánh dấu PASS/FAIL/EXPECTED_FAIL

---

## 0. Quy ước

- **VIEW** = GET list + GET detail (by id seed đầu tiên)
- **CREATE** = POST với payload tối thiểu hợp lệ
- **UPDATE** = PUT/PATCH 1 trường (vd `note`, `description`)
- **DELETE** = DELETE soft (kiểm tra `deleted_at` set)
- HTTP 200/201/204 = PASS, 4xx/5xx schema mismatch = EXPECTED_FAIL (xem mục 3), còn lại = FAIL
- Mọi request kèm header `Authorization: Bearer $TOKEN` + `X-Tenant-Id: $TENANT`

---

## 1. Test Matrix (Module × CRUD)

### 1.1 Identity & Tenant (Sprint 1-2)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **Auth** | `GET /api/v1/auth/me` · UI `/login` · AC: trả về user info | `POST /api/v1/auth/login` · AC: token + refresh | `POST /api/v1/auth/refresh` · AC: token mới | `POST /api/v1/auth/logout` · AC: 204 |
| **Tenant** | `GET /api/v1/tenants` · UI `/admin/tenants` · AC: list ≥1 tenant | `POST /api/v1/tenants` · AC: tạo tenant mới | `PUT /api/v1/tenants/{id}` · AC: đổi name | `DELETE /api/v1/tenants/{id}` · AC: soft delete |
| **User** | `GET /api/v1/users` · UI `/admin/users` · AC: list user của tenant | `POST /api/v1/users` · AC: tạo user mới + hash password | `PUT /api/v1/users/{id}` · AC: đổi email/role | `DELETE /api/v1/users/{id}` · AC: soft delete |
| **Role** | `GET /api/v1/roles` · UI `/admin/roles` · AC: list 6 role mặc định | `POST /api/v1/roles` · AC: tạo custom role | `PUT /api/v1/roles/{id}` · AC: đổi permission set | `DELETE /api/v1/roles/{id}` · AC: chặn role hệ thống |
| **Permission** | `GET /api/v1/permissions` · UI `/admin/permissions` · AC: list permission key | `POST /api/v1/permissions` · AC: tạo permission tùy biến | `PUT /api/v1/permissions/{id}` · AC: đổi description | `DELETE /api/v1/permissions/{id}` · AC: 204 |
| **AuditLog** | `GET /api/v1/audit-logs` · UI `/admin/audit` · AC: list event paging | N/A (auto) | N/A | N/A |

### 1.2 Patient Domain (Sprint 3-4)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **Patient** | `GET /api/v1/patients` · UI `/patients` · AC: list 30 BN seed | `POST /api/v1/patients` · AC: tạo BN mới + sinh mã | `PUT /api/v1/patients/{id}` · AC: đổi phone | `DELETE /api/v1/patients/{id}` · AC: soft delete |
| **Allergy** | `GET /api/v1/patients/{id}/allergies` · UI `/patients/{id}/allergies` · AC: list dị ứng | `POST /api/v1/patients/{id}/allergies` · AC: thêm dị ứng | `PUT /api/v1/allergies/{id}` · AC: đổi severity | `DELETE /api/v1/allergies/{id}` · AC: 204 |
| **Insurance** | `GET /api/v1/patients/{id}/insurances` · UI `/patients/{id}/bhyt` · AC: list thẻ BHYT | `POST /api/v1/patients/{id}/insurances` · AC: thêm thẻ | `PUT /api/v1/insurances/{id}` · AC: đổi hạn | `DELETE /api/v1/insurances/{id}` · AC: 204 |
| **EmergencyContact** | `GET /api/v1/patients/{id}/emergency-contacts` · UI `/patients/{id}` · AC: list liên hệ | `POST /api/v1/patients/{id}/emergency-contacts` · AC: thêm liên hệ | `PUT /api/v1/emergency-contacts/{id}` · AC: đổi phone | `DELETE /api/v1/emergency-contacts/{id}` · AC: 204 |
| **Consent** | `GET /api/v1/patients/{id}/consents` · UI `/patients/{id}/consent` · AC: list consent | `POST /api/v1/patients/{id}/consents` · AC: thêm consent ký | `PUT /api/v1/consents/{id}` · AC: revoke | `DELETE /api/v1/consents/{id}` · AC: 204 |

### 1.3 Clinical Workflow (Sprint 5-7)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **Reception** | `GET /api/v1/receptions?date=today` · UI `/reception` · AC: list ticket hôm nay | `POST /api/v1/receptions` · AC: lấy số + phân phòng | `PUT /api/v1/receptions/{id}` · AC: đổi phòng | `DELETE /api/v1/receptions/{id}` · AC: hủy ticket |
| **Encounter** | `GET /api/v1/encounters` · UI `/encounters` · AC: list 55 encounter seed | `POST /api/v1/encounters` · AC: tạo lượt khám mới | `PUT /api/v1/encounters/{id}` · AC: close encounter | `DELETE /api/v1/encounters/{id}` · AC: soft delete |
| **VitalSign** | `GET /api/v1/encounters/{id}/vitals` · UI `/encounters/{id}` · AC: list 50 vital seed | `POST /api/v1/encounters/{id}/vitals` · AC: nhập M/HA/T | `PUT /api/v1/vitals/{id}` · AC: sửa số liệu | `DELETE /api/v1/vitals/{id}` · AC: 204 |
| **EMR** | `GET /api/v1/encounters/{id}/emr` · UI `/encounters/{id}/emr` · AC: bệnh án | `POST /api/v1/encounters/{id}/emr` · AC: tạo bệnh án | `PUT /api/v1/emr/{id}` · AC: cập nhật SOAP | `DELETE /api/v1/emr/{id}` · AC: 204 |
| **Diagnosis** | `GET /api/v1/encounters/{id}/diagnoses` · UI `/encounters/{id}` · AC: list ICD-10 | `POST /api/v1/encounters/{id}/diagnoses` · AC: thêm ICD-10 | `PUT /api/v1/diagnoses/{id}` · AC: đổi type (primary/sec) | `DELETE /api/v1/diagnoses/{id}` · AC: 204 |
| **DiabetesAssessment** | `GET /api/v1/diabetes-assessments` · UI `/diabetes` · AC: list 30 seed | `POST /api/v1/diabetes-assessments` · AC: tạo đánh giá ĐTĐ | `PUT /api/v1/diabetes-assessments/{id}` · AC: đổi HbA1c | `DELETE /api/v1/diabetes-assessments/{id}` · AC: 204 |

### 1.4 Lab & Radiology (Sprint 8)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **LabOrder** | `GET /api/v1/lab-orders` · UI `/cls/lab` · AC: list chỉ định | `POST /api/v1/lab-orders` · AC: chỉ định XN | `PUT /api/v1/lab-orders/{id}` · AC: đổi status | `DELETE /api/v1/lab-orders/{id}` · AC: hủy |
| **RadOrder** | `GET /api/v1/rad-orders` · UI `/cls/rad` · AC: list CĐHA | `POST /api/v1/rad-orders` · AC: chỉ định X-quang | `PUT /api/v1/rad-orders/{id}` · AC: đổi status | `DELETE /api/v1/rad-orders/{id}` · AC: hủy |
| **LabResult** | `GET /api/v1/lab-orders/{id}/results` · UI `/cls/lab/{id}` · AC: kết quả | `POST /api/v1/lab-orders/{id}/results` · AC: nhập KQ | `PUT /api/v1/lab-results/{id}` · AC: sửa giá trị | `DELETE /api/v1/lab-results/{id}` · AC: 204 |
| **LabPartner** | `GET /api/v1/lab-partners` · UI `/admin/lab-partners` · AC: list NCC ngoài | `POST /api/v1/lab-partners` · AC: tạo đối tác | `PUT /api/v1/lab-partners/{id}` · AC: đổi API key | `DELETE /api/v1/lab-partners/{id}` · AC: 204 |

### 1.5 Pharmacy (Sprint 9-10)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **Prescription** | `GET /api/v1/prescriptions` · UI `/prescriptions` · AC: list đơn | `POST /api/v1/prescriptions` · AC: kê đơn + đẩy ĐTQG | `PUT /api/v1/prescriptions/{id}` · AC: sửa trước khi gửi | `DELETE /api/v1/prescriptions/{id}` · AC: hủy đơn |
| **Drug** | `GET /api/v1/drugs` · UI `/pharmacy/drugs` · AC: list drug master | `POST /api/v1/drugs` · AC: thêm thuốc | `PUT /api/v1/drugs/{id}` · AC: đổi giá | `DELETE /api/v1/drugs/{id}` · AC: 204 |
| **Pharmacy/Stock** | `GET /api/v1/stocks` · UI `/pharmacy/stock` · AC: tồn kho theo lô | `POST /api/v1/stocks/imports` · AC: nhập kho | `PUT /api/v1/stocks/{id}` · AC: điều chỉnh | `DELETE /api/v1/stocks/{id}` · AC: 204 |
| **Dispense** | `GET /api/v1/dispenses` · UI `/pharmacy/dispense` · AC: list cấp phát | `POST /api/v1/dispenses` · AC: cấp phát theo đơn | `PUT /api/v1/dispenses/{id}` · AC: confirm | `DELETE /api/v1/dispenses/{id}` · AC: hoàn lại tồn |

### 1.6 Billing & BHYT (Sprint 11)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **ServiceCatalog** | `GET /api/v1/services` · UI `/admin/services` · AC: bảng giá DV | `POST /api/v1/services` · AC: thêm dịch vụ | `PUT /api/v1/services/{id}` · AC: đổi giá | `DELETE /api/v1/services/{id}` · AC: 204 |
| **Billing** | `GET /api/v1/billings` · UI `/cashier/billing` · AC: list hóa đơn | `POST /api/v1/billings` · AC: tạo hóa đơn từ encounter | `PUT /api/v1/billings/{id}` · AC: thêm item | `DELETE /api/v1/billings/{id}` · AC: hủy |
| **Payment** | `GET /api/v1/payments` · UI `/cashier/payments` · AC: list giao dịch | `POST /api/v1/payments` · AC: thu tiền | `PUT /api/v1/payments/{id}` · AC: đổi method | `DELETE /api/v1/payments/{id}` · AC: refund |
| **EInvoice** | `GET /api/v1/einvoices` · UI `/cashier/einvoice` · AC: list HĐĐT | `POST /api/v1/einvoices` · AC: phát hành | `PUT /api/v1/einvoices/{id}` · AC: thay thế | `DELETE /api/v1/einvoices/{id}` · AC: hủy |
| **Cashier** | `GET /api/v1/cashier/shifts` · UI `/cashier` · AC: ca làm | `POST /api/v1/cashier/shifts` · AC: mở ca | `PUT /api/v1/cashier/shifts/{id}` · AC: đóng ca | N/A |
| **BHYT Export** | `GET /api/v1/bhyt/exports` · UI `/bhyt` · AC: list file XML | `POST /api/v1/bhyt/exports` · AC: gen XML 4750 | `PUT /api/v1/bhyt/exports/{id}` · AC: re-gen | `DELETE /api/v1/bhyt/exports/{id}` · AC: 204 |

### 1.7 Platform (Sprint 12-13)

| Module | VIEW | CREATE | UPDATE | DELETE |
|---|---|---|---|---|
| **Notification** | `GET /api/v1/notifications` · UI `/notifications` · AC: list thông báo | `POST /api/v1/notifications` · AC: gửi noti | `PUT /api/v1/notifications/{id}/read` · AC: mark read | `DELETE /api/v1/notifications/{id}` · AC: 204 |
| **ApiPartner** | `GET /api/v1/api-partners` · UI `/admin/partners` · AC: list integration | `POST /api/v1/api-partners` · AC: tạo client | `PUT /api/v1/api-partners/{id}` · AC: rotate secret | `DELETE /api/v1/api-partners/{id}` · AC: 204 |
| **PortalAuth** | `GET /api/v1/portal/me` · UI `/portal/login` · AC: BN tự đăng nhập | `POST /api/v1/portal/auth/login` · AC: OTP login | `POST /api/v1/portal/auth/refresh` · AC: token mới | `POST /api/v1/portal/auth/logout` · AC: 204 |
| **Dashboard** | `GET /api/v1/dashboard/summary` · UI `/dashboard` · AC: KPI cards | N/A | N/A | N/A |
| **Reports** | `GET /api/v1/reports/revenue?from=&to=` · UI `/reports` · AC: doanh thu | `POST /api/v1/reports/exports` · AC: export Excel | N/A | `DELETE /api/v1/reports/exports/{id}` · AC: 204 |

---

## 2. Test script

Xem `smoke-test.sh` (bash + curl + jq).
Output: TSV `module \t action \t http_code \t error_code \t pass/fail \t note`
Redirect: `./smoke-test.sh | tee smoke-2026-05-23.tsv`

---

## 3. Expected known issues (EXPECTED_FAIL)

Các bảng schema dump cũ chưa drop/recreate, đánh dấu **EXPECTED_FAIL** thay vì raise bug mới:

| Bảng / Module | Vấn đề | Ảnh hưởng endpoint |
|---|---|---|
| `drug_master` | Thiếu cột `atc_code`, `route`, `tenant_id` chưa NOT NULL | `/drugs` CREATE/UPDATE |
| `pha_prescriptions` | Cột `donthuocqg_code` rename, FK encounter chưa cập nhật | `/prescriptions` toàn bộ |
| `pha_prescription_items` | Mismatch `dosage_text` vs `instruction` | `/prescriptions` CREATE |
| `bil_billing` | Thiếu `discount_amount`, `vat_rate` | `/billings` CREATE/UPDATE |
| `bil_billing_items` | Schema dump cũ, chưa có `service_id` FK | `/billings` detail |
| `cli_lab_results` | Cột `reference_range` thiếu, value type mismatch | `/lab-results` CREATE |
| `sec_audit_logs` | Schema cũ vẫn còn, chưa drop+recreate theo entity mới | `/audit-logs` VIEW có thể 500 |
| `pha_stock_lots` | Chưa migrate `expiry_date` từ varchar → date | `/stocks` VIEW |
| `bhyt_export_files` | XSD 4750 chưa đầy đủ, file gen có thể invalid | `/bhyt/exports` CREATE |
| `notif_*` | Bảng chưa tạo (Sprint 12 mới spec) | `/notifications` 404 toàn bộ |

> Nếu lỗi nằm ngoài danh sách trên → tạo bug ticket bình thường.

---

## 4. Priority bucket

### P0 — BLOCKER (golden path, fail = stop test)
- Auth login + refresh
- Dashboard summary
- Patient list + create + detail
- Encounter list + detail
- VitalSign create
- DiabetesAssessment list

### P1 — CRITICAL (core nghiệp vụ)
- Prescription create + view (kể cả khi EXPECTED_FAIL vẫn ghi log)
- Drug list
- Dispense create
- Billing create + Payment
- BHYT Export gen XML
- LabOrder + LabResult
- Reception flow

### P2 — NICE (phụ trợ, có thể skip nếu hết giờ)
- Notification
- ApiPartner
- PortalAuth
- Reports export
- Consent / EmergencyContact / Allergy
- LabPartner / RadOrder
- EInvoice / Cashier shift

---

**Hết matrix.** Phượng chạy theo thứ tự P0 → P1 → P2, ghi kết quả vào file TSV và đính kèm khi báo cáo.
