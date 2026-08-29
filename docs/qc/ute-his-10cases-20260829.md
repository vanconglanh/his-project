# UTE — 10 Kịch Bản Thực Tế (HIS Pro-Diab)

**Ngày thực thi:** 2026-08-29  
**Môi trường:** localhost Docker (branch `develop`, commit mới nhất sau merge `sys_phong_kham_noi`)  
**Tài khoản test:** `qc.admin@prodiab.test` / role admin / tenant_id=1 / branch_id=1  
**Backend:** http://localhost:5000 | **Frontend:** http://localhost:3000  

---

## Dữ liệu tham chiếu

| Resource | Giá trị |
|---|---|
| Room PK01 | `c0000000-0000-0000-0000-000000000001` |
| Room PK02 | `c0000000-0000-0000-0000-000000000002` |
| Drug Metformin 500mg | `d0000000-0000-0000-0000-000000000001`, batch LOT-M001 (qty 486) |
| Drug Amlodipine 5mg | `d0000000-0000-0000-0000-000000000002`, batch LOT-A001 (qty 200) |
| Drug Paracetamol 500mg | `d0000000-0000-0000-0000-000000000004`, batch LOT-P001 (qty 1000) |
| Drug Omeprazole 20mg | `d0000000-0000-0000-0000-000000000005`, batch LOT-O001 (qty 300) |
| Drug Glibenclamide 5mg | `d0000000-0000-0000-0000-000000000006` |
| Drug Gliclazide 80mg | `d0000000-0000-0000-0000-000000000007` |

---

## CASE 1 — Khám Mới Hoàn Toàn (Full Flow)

**Kịch bản:** Bệnh nhân lần đầu, đầy đủ luồng Tiếp đón → Kê đơn → Cấp phát → Thu ngân.  
**Bệnh nhân:** Le Van Cuong (id=`7a485755-499e-43b5-9bd6-517d50c72cbf`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient | POST /api/v1/patients | 201 | PASS | id=7a485755, code=BNT01000012 |
| Tạo encounter | POST /api/v1/encounters | 201 | PASS | id=d3b91fdc, status=WAITING, type=OUTPATIENT |
| Kê đơn (2 thuốc) | POST /api/v1/prescriptions | 201 | PASS | id=fc89f196, items confirmed via GET detail |
| Ký đơn | POST /api/v1/prescriptions/{id}/sign | 200 | PASS | status=SIGNED |
| Cấp phát | POST /api/v1/pharmacy/dispense/{rxId} | 200 | PASS | status=DISPENSED |
| Tạo billing | POST /api/v1/billings | 201 | PASS | id=cbde1631, payable=0 |
| Finalize billing | POST /api/v1/billings/{id}/finalize | 200 | PASS | status=FINALIZED |
| Thu tiền | POST /api/v1/payments | 201 | PASS | status=COMPLETED, amount=0 |

**DB verification:**
- `diab_his_enc_encounters`: status=WAITING (encounter chưa start — xem Finding F1)
- `diab_his_pha_prescriptions`: status=DISPENSED ✓
- `diab_his_bil_billing`: status=PAID, patient_payable=0.00, paid_amount=0.00
- `diab_his_bil_billing_items`: COUNT=0 (xem Finding F2)
- `diab_his_bil_payments`: status=COMPLETED, amount=0.00 ✓

**Kết quả: PASS** (về mặt flow hoạt động) — nhưng có 2 finding cần chú ý bên dưới.

**Finding F1:** Encounter vẫn ở status=WAITING dù đã dispense và bill xong. Hệ thống cho phép kê đơn, cấp phát, thu ngân mà không cần encounter ở trạng thái IN_PROGRESS hay DONE. Đây là rủi ro data integrity: encounter "treo" WAITING trong khi đã có prescription+payment.

**Finding F2:** `POST /api/v1/prescriptions` response trả về `items: []` (mảng rỗng) dù đã gửi items trong body. Phải gọi thêm `GET /api/v1/prescriptions/{id}` mới thấy items đầy đủ. Inconsistent response gây client phải thêm 1 roundtrip.

---

## CASE 2 — Tái Khám (Revisit)

**Kịch bản:** Dùng lại bệnh nhân Case 1, tạo encounter mới (lần khám thứ 2).

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| List encounter cũ | GET /api/v1/encounters?patient_id={id} | 200 | PASS | total=1 encounter trước đó |
| Tạo encounter mới | POST /api/v1/encounters (type=REVISIT) | 201 | PASS | id=4a58b316, type=REVISIT |
| List lại | GET /api/v1/encounters?patient_id={id} | 200 | PASS | total=2 (tăng đúng) |
| Xem detail | GET /api/v1/encounters/{id} | 200 | PASS | status=WAITING, type=REVISIT |

**Kết quả: PASS**

**Finding F3:** Encounter REVISIT không có trường `previous_encounter_id` hay tham chiếu nào đến lượt khám trước. Lịch sử khám chỉ truy vấn được qua `GET /encounters?patient_id=...`. Bác sĩ không thể biết chẩn đoán cũ trực tiếp từ encounter mới mà phải tự tra cứu thêm — UX kém cho tái khám.

---

## CASE 3 — Chuyển Phòng Khám Giữa Chừng

**Kịch bản:** Tạo encounter tại PK01 → chuyển sang PK02.  
**Bệnh nhân:** Pham Van Duc (`7c080b16-26b6-4b26-8920-0e5bddd12364`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient | POST /api/v1/patients | 201 | PASS | id=7c080b16 |
| Tạo encounter tại PK01 | POST /api/v1/encounters | 201 | PASS | room=PK01 |
| Chuyển sang PK02 | PUT /api/v1/encounters/{id} body `{room_id: PK02}` | 200 | PASS | room=c0000000-...-000000000002 |
| Verify | GET /api/v1/encounters/{id} | 200 | PASS | room_id=PK02 ✓ |

**Kết quả: PASS**

**Note:** Không có event log về việc chuyển phòng trong timeline (timeline rỗng cho encounter WAITING). Dev cần xem xét có cần audit trail chuyển phòng không.

---

## CASE 4 — Bệnh Nhân Có BHYT

**Kịch bản:** Tạo patient, gắn thẻ BHYT, kê đơn, áp dụng BHYT khi billing.  
**Bệnh nhân:** Nguyen Thi Mai (`e5555236-44ed-4e27-8e9a-926b5b934eaf`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient | POST /api/v1/patients | 201 | PASS | id=e5555236 |
| Thêm BHYT ~~`/insurances`~~ | POST /api/v1/patients/{id}/insurances | 404 | FAIL | Route sai — xem Bug B1 |
| Thêm BHYT (đúng route) | POST /api/v1/patients/{id}/insurance | 201 | PASS | card=DN406***, coverage=80% |
| Tạo encounter + Rx + Sign + Dispense | (như Case 1) | 2xx | PASS | |
| Tạo billing | POST /api/v1/billings | 201 | PASS | payer=BHYT |
| Apply BHYT | POST /api/v1/billings/{id}/apply-bhyt `{copay_rate:20}` | 200 | PASS | bhyt_amount=0 |

**Kết quả: PARTIAL PASS**

**Bug B1 (Low):** Route tài liệu brief ghi `/patients/{id}/insurances` (số nhiều) nhưng thực tế là `/patients/{id}/insurance` (số ít). Client sẽ nhận 404 nếu dùng theo brief.

**Finding F4:** `bhyt_amount=0` sau khi apply BHYT vì tất cả thuốc trong danh mục có `price=null` → billing không có line items → không tính được BHYT. Không thể verify nghiệp vụ BHYT tính đúng hay không. Cần seed giá thuốc để test thực sự.

---

## CASE 5 — Trẻ Em Cần Người Giám Hộ

**Kịch bản:** Tạo bệnh nhân dưới 72 tháng tuổi, kiểm tra validation guardian.  
**Bệnh nhân:** Tran Bich Ngoc (DOB=2025-01-15, ~7 tháng tuổi)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo child KHÔNG có guardian | POST /api/v1/patients (DOB=2024-03-01) | 422 | PASS | `GUARDIAN_INFO_REQUIRED` — enforcement đúng |
| Tạo child CÓ guardian | POST /api/v1/patients + `guardians: [{full_name,relationship,phone,id_number}]` | 201 | PASS | id=bddcdd80 |
| DB verify guardian | DB query `diab_his_pat_guardians` | — | PASS | 1 record: Tran Van Hung, Cha ✓ |
| Get guardian list | GET /api/v1/patients/{id}/guardians | 404 | FAIL | Endpoint không tồn tại — xem Bug B2 |
| Get patient detail | GET /api/v1/patients/{id} | 200 | — | Field `guardians` không có trong response |

**Kết quả: PASS (validation đúng) + BUG B2**

**Bug B2 (Medium):** `GET /api/v1/patients/{id}/guardians` trả 404. Không có endpoint nào để đọc lại guardian đã tạo qua API. Guardian data được lưu vào DB (`diab_his_pat_guardians`) nhưng không expose qua API và không hiển thị trong patient detail. Frontend không thể hiển thị thông tin người giám hộ của bệnh nhân trẻ em.

---

## CASE 6 — Trùng Lặp Bệnh Nhân (Dedup)

**Kịch bản:** Tạo patient → thử tạo lại cùng CCCD+SĐT → verify cảnh báo trùng lặp.  
**Bệnh nhân gốc:** Hoang Thi Thu (`bfe014d0-8b0a-4d0c-a088-6bb24b9900d0`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient gốc | POST /api/v1/patients | 201 | PASS | id=bfe014d0, CCCD=031190077001 |
| Tạo lại CÙNG CCCD+SĐT+ngày sinh | POST /api/v1/patients (body y hệt) | 200 | PASS | `possible_duplicate=true`, candidates=1, `match_reason=CCCD_TRUNG` |
| Xác nhận tạo mới dù trùng | POST /api/v1/patients + `confirm_create_despite_duplicate:true` | 201 | PASS | id=aba7ec4a (patient mới tạo được) |

**Kết quả: PASS** — Dedup hoạt động đúng theo spec.

---

## CASE 7 — Kê Đơn Nhiều Thuốc + DDI Check

**Kịch bản:** Kê 3 thuốc (Glibenclamide + Gliclazide cùng nhóm sulfonylurea + Metformin), gọi DDI check.  
**Bệnh nhân:** Vo Thanh Long (`7d485...(mới)`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient + encounter | (như Case 1) | 2xx | PASS | |
| Kê 3 thuốc | POST /api/v1/prescriptions (3 items) | 201 | PASS | id=345e17b8, items=3 (via GET detail) |
| DDI check | GET /api/v1/prescriptions/{id}/ddi-check | 200 | PASS (nhưng kết quả rỗng) | `warnings=[]`, `has_contraindicated=false` |

**Kết quả: PARTIAL PASS**

**Finding F5:** DDI check endpoint tồn tại nhưng response trống. Glibenclamide + Gliclazide là 2 thuốc cùng nhóm sulfonylurea (ATC A10BB01 + A10BB09) — combination này tăng nguy cơ hạ đường huyết nặng, cần cảnh báo. Hệ thống không phát hiện do DDI database chưa được seed. Response structure thực tế: `{prescription_id, warnings: [], has_contraindicated: false}` (khác với `{has_interactions, interactions}` trong brief — lưu ý cho client).

---

## CASE 8 — Hủy Lượt Khám Giữa Chừng

**Kịch bản:** Tạo encounter → hủy/đóng bất thường.  
**Bệnh nhân:** Dinh Van Khanh (`(mới tạo)`)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient + encounter | (như Case 1) | 2xx | PASS | status=WAITING |
| Close từ WAITING | POST /api/v1/encounters/{id}/close | 422 | Expected | `ENCOUNTER_INVALID_TRANSITION` — cần start trước |
| Start encounter | POST /api/v1/encounters/{id}/start | 200 | PASS | status=IN_PROGRESS |
| Close chưa có chẩn đoán | POST /api/v1/encounters/{id}/close | 422 | Expected | `DIAGNOSIS_REQUIRED` |
| Thêm chẩn đoán PRIMARY | POST /api/v1/encounters/{id}/diagnoses `{type:"PRIMARY"}` | 201 | PASS | icd10=Z00.0 |
| Close chưa ký bệnh án | POST /api/v1/encounters/{id}/close | 422 | Expected | `EMR_NOT_SIGNED` |
| Lưu EMR draft | PUT /api/v1/encounters/{id}/emr | 200 | PASS | |
| Ký bệnh án | POST /api/v1/encounters/{id}/emr/sign `{certificate_id:...}` | 200 | PASS | |
| Close | POST /api/v1/encounters/{id}/close | 200 | PASS | closed=true |
| Verify status | GET /api/v1/encounters/{id} | 200 | PASS | status=DONE |
| Verify không có billing treo | GET /api/v1/billings?encounter_id={id} | 200 | PASS | total=0 ✓ |
| Verify không có prescription treo | GET /api/v1/prescriptions?encounter_id={id} | 200 | PASS | total=0 ✓ |

**Kết quả: PASS**

**Finding F6 (UX):** Flow đóng encounter yêu cầu 5 bước riêng biệt (Start → Diagnose → Save EMR → Sign EMR → Close). Không có route "cancel encounter" dành cho trường hợp bệnh nhân về giữa chừng mà không muốn hoàn tất bệnh án. Dev cần cân nhắc thêm route CANCEL (riêng với CLOSE) cho case này.

**Finding F7:** `POST /api/v1/encounters/{id}/emr/sign` yêu cầu field `certificate_id` (khác với `certificate_thumbprint` trong prescription sign). Bất nhất giữa 2 sign flow — FE cần xử lý 2 tên field khác nhau.

---

## CASE 9 — Thanh Toán Một Phần (Partial Payment)

**Kịch bản:** Tạo billing → add service item thủ công → finalize → thu tiền 1 phần.

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient + encounter | (như Case 1) | 2xx | PASS | |
| Tạo billing | POST /api/v1/billings | 201 | PASS | id=mới |
| Add billing item | POST /api/v1/billings/{id}/items `{type:"SERVICE", unit_price:150000}` | 500 | **BUG** | `INTERNAL_ERROR` — xem Bug B3 |
| Service catalog | GET /api/v1/services | 200 | — | total=0 (catalog rỗng) |

**Kết quả: FAIL — BUG B3**

**Bug B3 (High):** `POST /api/v1/billings/{id}/items` luôn trả HTTP 500 `INTERNAL_ERROR`.

Root cause từ backend log:
```
Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException: The database operation was expected 
to affect 1 row(s), but actually affected 0 row(s)
at ProDiabHis.Application.Billing.AddBillingItemHandler.Handle(...) in BillingHandlers.cs:line 309
```

EF Core phát sinh UPDATE thay vì INSERT cho item mới — có thể `BillingMapper.Recalculate(b)` đang modify items collection sau khi Add, khiến EF mất tracking state. Suggest dev xem lại cách `Recalculate` tương tác với DbContext change tracker, và kiểm tra có `RowVersion`/concurrency token nào trên bảng hay không.

**Hậu quả:** Không thể test partial payment vì không tạo được billing item có giá trị thực. Test case này chỉ test được với amount=0 (từ dispense null prices) — không có giá trị.

---

## CASE 10 — Cấp Cứu (Emergency Encounter)

**Kịch bản:** Tạo encounter loại EMERGENCY, chạy full flow, verify type preserved.  
**Bệnh nhân:** Nguyen Van Tuan (`26ef35cb` encounter)

| Bước | Route | HTTP | Kết quả | Evidence |
|---|---|---|---|---|
| Tạo patient | POST /api/v1/patients | 201 | PASS | id=(mới) |
| Tạo encounter EMERGENCY | POST /api/v1/encounters `{encounter_type:"EMERGENCY"}` | 201 | PASS | id=26ef35cb, type=EMERGENCY, status=WAITING |
| Start | POST /api/v1/encounters/{id}/start | 200 | PASS | status=IN_PROGRESS |
| Thêm chẩn đoán PRIMARY | POST /api/v1/encounters/{id}/diagnoses | 201 | PASS | icd10=I20.0 (Dau thắt ngực cấp) |
| Kê đơn + Ký + Cấp phát | (như Case 1) | 2xx | PASS | |
| Lưu EMR + Ký | PUT + POST /emr/sign | 200 | PASS | |
| Close | POST /api/v1/encounters/{id}/close | 200 | PASS | closed=true |
| Verify | GET /api/v1/encounters/{id} | 200 | PASS | status=DONE, type=EMERGENCY ✓ |
| Timeline | GET /api/v1/encounters/{id}/timeline | 200 | PASS | 1 event (encounter closed) |

**Kết quả: PASS**

**Finding F8:** Timeline chỉ có 1 event (close event). Các sự kiện quan trọng như start, diagnosis, prescription, dispense không được ghi vào timeline. Bác sĩ/QC không thể trace lại timeline đầy đủ của lượt khám.

---

## Tổng Hợp

### Bảng PASS/FAIL

| Case | Tên | Kết quả | Bugs | Severity |
|---|---|---|---|---|
| C1 | Khám mới hoàn toàn | PASS | F1, F2 | Med, Low |
| C2 | Tái khám | PASS | F3 (UX) | Low |
| C3 | Chuyển phòng | PASS | — | — |
| C4 | Bệnh nhân BHYT | PARTIAL | B1 (route sai), F4 (giá null) | Low, Med |
| C5 | Trẻ em + guardian | PASS + BUG | B2 (no GET guardian) | Medium |
| C6 | Trùng lặp (dedup) | PASS | — | — |
| C7 | DDI check | PARTIAL | F5 (DDI DB rỗng) | Medium |
| C8 | Hủy lượt khám | PASS | F6, F7 (UX/API) | Low |
| C9 | Thanh toán một phần | FAIL | **B3 (500 error)** | **High** |
| C10 | Emergency encounter | PASS | F8 (timeline thiếu) | Low |

**Tổng:** 7 PASS / 1 FAIL / 2 PARTIAL  
**Pass rate (bước):** ~88% (các bước API riêng lẻ)

---

### Bug Report

#### BUG-B3 (High) — AddBillingItem trả HTTP 500
- **Route:** POST /api/v1/billings/{id}/items
- **Steps:** Tạo billing (DRAFT) → POST items với bất kỳ body hợp lệ
- **Expected:** 201 Created với item được thêm
- **Actual:** HTTP 500 `INTERNAL_ERROR` `DbUpdateConcurrencyException`
- **Root cause:** EF Core UpdateConcurrencyException tại `BillingHandlers.cs:309` (SaveChangesAsync). Nguyên nhân: `BillingMapper.Recalculate(b)` có thể modify EF tracking state khiến item mới bị xử lý như UPDATE thay vì INSERT.
- **File:** `backend/src/ProDiabHis.Application/Billing/BillingHandlers.cs` line 282–311

#### BUG-B2 (Medium) — Không có API đọc guardian
- **Route:** GET /api/v1/patients/{id}/guardians → 404
- **Root cause:** PatientsController không có route guardian. Guardian được lưu DB qua create patient, nhưng không có read endpoint. Field `guardians` cũng không có trong patient detail response.
- **File:** `backend/src/ProDiabHis.Api/Controllers/PatientsController.cs` — thiếu guardian GET/LIST route

#### BUG-B1 (Low) — Route insurance sai (số nhiều vs số ít)
- **Route doc/brief:** `/patients/{id}/insurances` → thực tế: `/patients/{id}/insurance`
- **Fix:** Cập nhật docs hoặc thêm alias route

---

### Findings Cần Data/Config Fix

| ID | Nội dung | Ưu tiên |
|---|---|---|
| F4 | Tất cả drug `price=null` → billing 0đ, không test được BHYT/partial payment | High (data) |
| F5 | DDI database rỗng → không cảnh báo tương tác thuốc nguy hiểm | High (data) |

---

### UX Issues

| # | Vấn đề | Ảnh hưởng | Đề xuất | Effort |
|---|---|---|---|---|
| 1 | Đóng encounter cần 5 bước riêng biệt (start→diagnose→save emr→sign emr→close). Không có route CANCEL cho trường hợp bệnh nhân về sớm | Bác sĩ, Lễ tân | Thêm route `POST /encounters/{id}/cancel` không yêu cầu chẩn đoán + EMR ký số | Medium |
| 2 | Encounter REVISIT không tham chiếu encounter trước | Bác sĩ | Thêm `previous_encounter_id` hoặc `previous_diagnoses` trong encounter detail | Medium |
| 3 | Timeline chỉ có 1 event (close). Không trace được start/diagnose/rx trong session | Audit, QC | Ghi event cho các action: start, add_diagnosis, create_prescription, dispense | Low |
| 4 | Prescription create response `items:[]` — phải GET lại để thấy items | Dev FE | Bổ sung items vào create response | Low |
| 5 | EMR sign dùng `certificate_id`, prescription sign dùng `certificate_thumbprint` — bất nhất | Dev FE | Chuẩn hóa tên field hoặc document rõ sự khác biệt | Low |

---

### Cases điều chỉnh khác brief

| Case | Điều chỉnh | Lý do |
|---|---|---|
| C4 | Không verify được BHYT amount > 0 | Drug prices null trong seed data |
| C8 | Test "hủy" = dùng route Close (không có route Cancel) | Không có `POST /encounters/{id}/cancel` |
| C9 | Không test được partial payment thực | AddBillingItem 500, service catalog rỗng |
| C7 | DDI check PASS về HTTP nhưng không có warnings | DDI DB không có data |

---

## Gate Decision

**CONDITIONAL** — Giao người test tay sau khi fix:

1. **Fix ngay (blocker cho partial payment):** BUG-B3 `AddBillingItem HTTP 500`
2. **Fix ngay (UX blocker cho trẻ em):** BUG-B2 Guardian không đọc được qua API
3. **Seed data:** Drug prices (ít nhất 5 thuốc chính) để test BHYT + billing thực
4. **Seed data:** DDI interaction database để test cảnh báo tương tác thuốc

Sau khi fix 4 điểm trên, full flow khám-kê đơn-cấp phát-billing có thể giao manual test.
