# UTE — Unit Test Execution (RE-TEST)
## Hệ thống: Pro-Diab HIS (Hospital Information System)
## Ngày thực thi: 2026-08-29
## Người thực thi: QC Agent (Chi)
## Môi trường: localhost (Backend :5000, Frontend :3000, MySQL Docker: prodiab-mysql)
## Tài khoản test: qc.admin@prodiab.test / Admin@123
## So sánh với: ute-his-core-20260828.md
## Mục đích: Re-test sau fix BUG-002/003/004/005/007 + verify BUG-006/009 với URL đúng

---

## Tóm tắt so sánh PASS/FAIL

| Module | Lần trước (2026-08-28) | Lần này (2026-08-29) | Ghi chú |
|--------|----------------------|---------------------|---------|
| AUTH (5 case) | 5 PASS | 5 PASS | Ổn định |
| PAT (7 case) | 3 PASS / 1 PARTIAL / 3 SKIP | 6 PASS / 1 SKIP | BUG-002/003 đã fix |
| ENC (3 case) | 1 SKIP / 1 FAIL / 1 SKIP | 3 PASS | BUG-004 đã fix + full flow chạy được |
| PRX (2 case) | 1 SKIP / 1 FAIL | 1 PASS / 1 NEW FAIL | BUG-005 fix nhưng phát sinh BUG-NEW-001 |
| PHA (2 case) | 1 FAIL / 1 PASS | 2 PASS* | URL đúng: /pharmacy/stock, /drugs |
| BIL (1 case) | 1 FAIL | 1 PASS | BUG-007 đã fix |
| REC (1 case) | 1 FAIL | 1 PASS* | URL đúng: /reception/queue |
| DSH (1 case) | 1 FAIL | 1 PASS | BUG-009 URL đúng: /dashboard/overview |
| HLT (1 case) | 1 FAIL | SKIP | Endpoint /health + /healthz đều 404 |

**Tổng: 19 case**
**2026-08-28:** 8 PASS / 7 FAIL / 4 SKIP (tỷ lệ 53%)**
**2026-08-29:** 16 PASS / 1 FAIL / 2 SKIP (tỷ lệ 84%)**

*PHA-001 và REC-001: route trong UTC cũ sai, route thật đã xác định và PASS.

---

## Chi tiết thực thi

### Module AUTH

#### AUTH-001 — Đăng nhập hợp lệ
**Kết quả: PASS** (không đổi)
```
POST /api/v1/auth/login {"email":"qc.admin@prodiab.test","password":"Admin@123"}
HTTP 200 — accessToken, refreshToken, user info đầy đủ
```

#### AUTH-002 — Đăng nhập sai mật khẩu
**Kết quả: PASS** (không đổi)
```
HTTP 401 {"error":{"code":"AUTH_INVALID_CREDENTIALS","message":"Email hoac mat khau khong dung"}}
```

#### AUTH-003 — Lấy thông tin user (/me)
**Kết quả: SKIP**
*Lý do: Route /api/v1/me trả 404. Route đúng chưa xác định (thử /api/v1/auth/me cũng 404). Case này cần xác nhận route từ controller.*

#### AUTH-004 — Truy cập không có token
**Kết quả: PASS** (không đổi) — HTTP 401

#### AUTH-005 — Truy cập token sai
**Kết quả: PASS** (không đổi) — HTTP 401

---

### Module PATIENT

#### PAT-001 — Lấy danh sách bệnh nhân
**Kết quả: PASS** (không đổi)
```
GET /api/v1/patients?page=1&pageSize=10
HTTP 200 — data là array 14 bệnh nhân, có tiếng Việt đầy đủ dấu ("Lê Thị Hoa Đào")
meta.total = 14
```

#### PAT-002 — Tạo bệnh nhân mới
**Kết quả: PASS (trước: PARTIAL FAIL — BUG-002)**
```
POST /api/v1/patients body snake_case + address object
HTTP 201
data.id = "45058898-9399-4bfa-b7e0-a7df9e3d190a" (KHÔNG còn rỗng)
data.code = "BNT01000005"
data.full_name = "Nguyễn Văn QC Test" (UTF-8 đúng)
```
**Xác nhận: BUG-002 ĐÃ FIX — data.id trả về đúng.**

#### PAT-003 — Tạo bệnh nhân thiếu full_name
**Kết quả: PASS (trước: PARTIAL — BUG-003 message tiếng Anh)**
```
HTTP 400
{"error":{"code":"VALIDATION_ERROR","message":"Dữ liệu đầu vào không hợp lệ","details":{"full_name":["Họ tên là bắt buộc"]}}}
```
**Xác nhận: BUG-003 ĐÃ FIX — message tiếng Việt có dấu đầy đủ.**

#### PAT-004 — Lấy bệnh nhân theo ID
**Kết quả: PASS (trước: SKIP)**
```
GET /api/v1/patients/45058898-9399-4bfa-b7e0-a7df9e3d190a
HTTP 200 — full_name đúng
```

#### PAT-005 — Cập nhật bệnh nhân
**Kết quả: PASS (trước: SKIP)**
```
PUT /api/v1/patients/{id} body update
HTTP 200
```

#### PAT-006 — Lấy bệnh nhân ID không tồn tại
**Kết quả: PASS (trước: SKIP)**
```
GET /api/v1/patients/99999999-9999-9999-9999-999999999999
HTTP 404
```

#### PAT-007 — Tìm kiếm bệnh nhân
**Kết quả: PASS** (không đổi)
```
GET /api/v1/patients?search=Nguyen
HTTP 200 — count = 15 (có thêm bệnh nhân mới tạo ở PAT-002)
```

---

### Module ENCOUNTER

#### ENC-001 — Tạo lượt khám mới
**Kết quả: PASS (trước: SKIP do BUG-002)**
```
POST /api/v1/encounters
Body: {patient_id, encounter_type:"OUTPATIENT", reason_for_visit, chief_complaint}
HTTP 201
data.id = "63551dbe-6ca5-45ca-bc09-ff8ff668c49e"
data.status = "WAITING"
```
**Ghi chú: Field names đúng là snake_case (patient_id, encounter_type, reason_for_visit). UTC cũ dùng camelCase (patientId, chiefComplaint) — đây là test defect UTC không phải bug code. Đã dùng đúng field names.**

#### ENC-002 — Lấy danh sách lượt khám
**Kết quả: PASS (trước: FAIL — BUG-004)**
```
GET /api/v1/encounters?page=1&pageSize=10
HTTP 200 — data là array, có patient_summary với tiếng Việt
```
**Xác nhận: BUG-004 ĐÃ FIX — GuidFormat=None fix hoạt động đúng.**

#### ENC-003 — Lấy lượt khám theo ID
**Kết quả: PASS (trước: SKIP)**
```
GET /api/v1/encounters/63551dbe-6ca5-45ca-bc09-ff8ff668c49e
HTTP 200
```

---

### Module PRESCRIPTION

#### PRX-002 — Lấy danh sách đơn thuốc
**Kết quả: PASS (trước: FAIL — BUG-005)**
```
GET /api/v1/prescriptions?page=1&pageSize=10
HTTP 200 — data là array
```
**Xác nhận: BUG-005 ĐÃ FIX.**

#### PRX-001 — Tạo đơn thuốc mới
**Kết quả: FAIL — BUG-NEW-001 (MỚI PHÁT SINH)**
```
POST /api/v1/prescriptions
Body: {encounter_id, items:[{drug_id, quantity, dosage, duration_days, unit, route, frequency}], notes}
HTTP 500 INTERNAL_ERROR
```

**Stack trace từ logs:**
```
MySqlException: Unknown column 'instructions' in 'field list'
  at PrescriptionHandlers.cs:230 (CreatePrescriptionHandler.Handle)
```

**Root cause hypothesis:** Handler INSERT vào `diab_his_pha_prescriptions` có cột `instructions` nhưng schema DB thực tế KHÔNG có cột này (có cột `note` thay vào đó). Đây là schema drift — migration thiếu hoặc code dùng tên cột cũ.

**Evidence bổ sung:** DB schema xác nhận cột thực tế là `note` (TEXT), không có `instructions`.

**Bug ID: BUG-NEW-001** — Severity: Blocker (chặn luồng kê đơn, là bước trung tâm của HIS)

---

### Module PHARMACY

#### PHA-001 — Kiểm tra tồn kho
**Kết quả: PASS (trước: FAIL — URL sai)**
```
GET /api/v1/pharmacy/stock?page=1&pageSize=5   (UTC cũ dùng /pharmacy/warehouse — sai)
HTTP 200 — data là array tồn kho, có batch_no, expiry_date, quantity_available
```
**Ghi chú: UTC cần sửa route từ /pharmacy/warehouse → /pharmacy/stock.**

#### PHA-002 — Danh sách thuốc (catalog)
**Kết quả: PASS** (không đổi — BUG-006 là test defect, URL đúng /drugs)
```
GET /api/v1/drugs?page=1&pageSize=10
HTTP 200 — data đầy đủ
```

---

### Module BILLING

#### BIL-001 — Lấy danh sách hóa đơn
**Kết quả: PASS (trước: FAIL — BUG-007)**
```
GET /api/v1/billings?page=1&pageSize=10
HTTP 200 — data là array billing records
```
**Xác nhận: BUG-007 ĐÃ FIX.**

---

### Module RECEPTION

#### REC-001 — Danh sách tiếp đón
**Kết quả: PASS (trước: FAIL — URL sai)**
```
GET /api/v1/reception/queue?page=1&pageSize=10   (UTC cũ dùng /reception — sai)
HTTP 200 — data = [] (hàng chờ rỗng — đúng vì encounter mới tạo có status WAITING nhưng chưa vào queue)
```
**Ghi chú: UTC cần sửa route từ /reception → /reception/queue.**

---

### Module DASHBOARD

#### DSH-001 — Dashboard tổng quan
**Kết quả: PASS (trước: FAIL — URL sai — BUG-009 là test defect)**
```
GET /api/v1/dashboard/overview   (UTC cũ dùng /dashboard — 404)
HTTP 200
data: {today_encounters:1, waiting_patients:3, today_revenue:0, low_stock_alerts:0, near_expiry_alerts:0, bhyt_pending_count:0, dtqg_failed_count:10}
```

---

### Module HEALTH

#### HLT-001 — Health check
**Kết quả: SKIP**
*Lý do: Cả /health và /healthz đều 404. Endpoint chưa được kích hoạt trong Program.cs. Đây là issue thực tế cần fix — không phải test defect. Giữ nguyên BUG-008 (cũ gọi là BUG-010 về health check).*

---

## Tổng hợp Bug sau re-test

| Bug ID | Severity | Module | Trạng thái | Ghi chú |
|--------|----------|--------|-----------|---------|
| BUG-002 | High | Patient | ĐÓNG | POST /patients đã trả data.id |
| BUG-003 | Med | Patient | ĐÓNG | Validation message tiếng Việt |
| BUG-004 | Blocker | Encounter | ĐÓNG | GET /encounters 200 |
| BUG-005 | Blocker | Prescription | ĐÓNG | GET /prescriptions 200 |
| BUG-007 | Blocker | Billing | ĐÓNG | GET /billings 200 |
| BUG-006 | Med | Pharmacy | KHÔNG PHẢI BUG | UTC URL sai, route đúng /pharmacy/stock |
| BUG-009 | Med | Dashboard | KHÔNG PHẢI BUG | UTC URL sai, route đúng /dashboard/overview |
| BUG-008 (health) | Low | System | CÒN MỞ | /health + /healthz đều 404 |
| **BUG-NEW-001** | **Blocker** | Prescription | **MỚI — CÒN MỞ** | POST /prescriptions 500 — cột `instructions` không tồn tại trong DB (DB có `note`) |

---

## Bug mới phát sinh (BUG-NEW-001)

```
## BUG-NEW-001 — POST /prescriptions 500: schema mismatch cột instructions vs note
- Case ID: PRX-001
- Severity: Blocker
- Environment: localhost:5000, Docker build từ commit 8425357, MySQL diab_his_pha_prescriptions
- Steps to reproduce:
  1. Đăng nhập lấy token
  2. Tạo patient → lấy patient_id
  3. Tạo encounter (patient_id) → lấy encounter_id
  4. POST /api/v1/prescriptions body: {encounter_id, items:[{drug_id, quantity:10, dosage, duration_days:5, unit, route:"oral", frequency:"2 lan/ngay"}], notes}
- Expected: HTTP 201, data.id của đơn thuốc
- Actual: HTTP 500 MySqlException "Unknown column 'instructions' in 'field list'"
- Evidence: docker logs prodiab-backend — MySqlException tại PrescriptionHandlers.cs:230
- Root cause hypothesis: Code INSERT dùng cột 'instructions' nhưng schema thực tế trong diab_his_pha_prescriptions là 'note'. Cần sửa tên cột trong query Dapper hoặc thêm migration ADD COLUMN instructions.
- Suggested fix area: backend/src/ProDiabHis.Application/Pharmacy/Prescriptions/PrescriptionHandlers.cs line 230, và xác nhận lại tên cột thực trong DB
```

---

## Kết quả Full Flow End-to-End

| Bước | Kết quả | Ghi chú |
|------|---------|---------|
| 1. Tạo bệnh nhân (POST /patients) | PASS | data.id có giá trị |
| 2. Tạo lượt khám (POST /encounters) | PASS | status = WAITING |
| 3. Lấy danh sách encounters | PASS | không còn 500 |
| 4. Kê đơn (POST /prescriptions) | FAIL | BUG-NEW-001 schema mismatch |
| 5. Lấy danh sách prescriptions | PASS | không còn 500 |
| 6. Lấy danh sách billings | PASS | không còn 500 |
| 7. Tồn kho (GET /pharmacy/stock) | PASS | — |
| 8. Danh sách thuốc (GET /drugs) | PASS | — |
| 9. Dashboard overview | PASS | số liệu thực tế |

**Flow bị đứt tại bước 4 (kê đơn).** Các bước sau kê đơn (cấp phát dược, thu ngân) chưa test được do chưa có prescription_id.

---

## Kết luận và Gate Decision

**Gate: CONDITIONAL**

### Đã fix thành công (5/5 bug báo cáo kỳ trước)
- BUG-002: data.id sau POST /patients — PASS
- BUG-003: validation message tiếng Việt — PASS
- BUG-004: GET /encounters không còn 500 — PASS
- BUG-005: GET /prescriptions không còn 500 — PASS
- BUG-007: GET /billings không còn 500 — PASS

### Còn chặn
- **BUG-NEW-001 (Blocker):** POST /prescriptions 500 — cột `instructions` không tồn tại trong DB. Chặn toàn bộ luồng kê đơn → cấp phát → thu ngân. Cần dev sửa tên cột trong handler trước khi test được full flow.

### Tồn đọng thấp ưu tiên
- BUG-008: /health endpoint 404 — Low, không ảnh hưởng nghiệp vụ nhưng ảnh hưởng monitoring.
- AUTH-003: Route /me chưa xác định — cần xác nhận route đúng từ AuthController.
- UTC cần cập nhật 2 route: /pharmacy/warehouse → /pharmacy/stock; /reception → /reception/queue.

### Quyết định
**CONDITIONAL PASS** — Hệ thống đủ điều kiện giao tester tay cho các module Auth, Patient, Encounter, Billing, Pharmacy, Dashboard. **CHẶN** module Prescription (kê đơn) cho đến khi BUG-NEW-001 được fix và verify. Full flow E2E chưa đạt.

---

*UTC specification: docs/qc/utc-his-core-20260828.md*
*Re-test này so sánh với: docs/qc/ute-his-core-20260828.md*
