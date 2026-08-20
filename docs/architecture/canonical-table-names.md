# Tên bảng chuẩn (canonical table names) — Pro-Diab HIS

> Quyết định kiến trúc. Tác giả: Lành (architect). Ngày: 2026-08-20. Nhánh: `develop`.
> Phạm vi: chốt tên bảng chuẩn để DevOps dựng baseline schema và backend sửa code sau.
> **Tài liệu này KHÔNG sửa code.** Việc sửa code giao ở task tiếp theo.

---

## 0. Phương pháp & nguồn dữ liệu (đã kiểm chứng bằng công cụ)

| Nguồn | Nội dung |
|---|---|
| `db/diab_his_*.sql` (64 file) | Dump production **hệ thống tham chiếu cũ** (host `57.155.1.252`, DB `diab_his`). Tên bảng **KHÔNG có prefix**: `pat_patients`, `cli_lab_orders`, `cli_allergies`, `cdss_rules`… |
| `db/migrations/*.sql` (218 file) | Migration của Pro-Diab. Tạo bảng **có prefix** `diab_his_*`. |
| `db/migrations/9000_drop_legacy.sql` | **DROP toàn bộ bảng KHÔNG có prefix `diab_his_`** (trừ `hangfire_*`) — "Clean Slate". |
| `C:\tmp\ef_tables.txt` | 58 bảng EF Core biết. |
| `C:\tmp\dapper_tables.txt` | 89 tên bảng trích từ Dapper raw SQL. |

### Kết luận nền tảng #1 — hai không gian tên, chỉ một cái sống sót

Base dump `db/diab_his_*.sql` **không phải** schema runtime của Pro-Diab. Nó là schema hệ thống cũ,
được nạp trước rồi bị `9000_drop_legacy.sql` **xóa sạch**. Sau `9000`, chỉ còn:

- bảng `diab_his_*` (do migration `00xx` chạy trước `9000` và `9xxx` chạy sau `9000` tạo ra), và
- một số **VIEW tương thích tên cũ** tạo lại ở `9009`, `9022`, `9061` (`pat_patients`, `sec_users`,
  `pha_prescriptions`, `cli_visits`, `cli_lab_partners`… → SELECT từ bảng `diab_his_*`).

> **=> Tên bảng chuẩn của Pro-Diab LUÔN có prefix `diab_his_`.**
> Tên không prefix chỉ là VIEW tương thích, **cấm dùng trong code mới**.

### Kết luận nền tảng #2 — 2 thế hệ migration chồng nhau

- Dải `0001–0067`: thế hệ 1, viết khi còn giả định "ALTER lên bảng dump cũ".
- Dải `9001–9095`: thế hệ 2 ("Clean Slate"), `CREATE TABLE` lại từ đầu sau `9000_drop_legacy`.

Vì dải `0xxx` sắp xếp **trước** `9000` theo tên file, các bảng `diab_his_*` do `0xxx` tạo **không bị
drop** và tiếp tục tồn tại song song với bảng cùng nghiệp vụ do `9xxx` tạo. **Đây chính là nguồn gốc
của toàn bộ 5 cặp lệch tên.**

---

## 1. KẾT LUẬN 5 CẶP LỆCH — tất cả đều là loại (c): CẢ HAI BẢNG ĐỀU TỒN TẠI THẬT

Không cặp nào thuộc loại (a) hay (b). **Schema đang bị nhân đôi thật sự**, và với 2 cặp thì **dữ liệu
đang bị ghi vào 2 nơi khác nhau bởi 2 luồng nghiệp vụ khác nhau**.

### 1.1 `diab_his_lab_orders` vs `diab_his_cli_lab_orders` — **CANONICAL: `diab_his_cli_lab_orders`**

| | tạo bởi | ai dùng |
|---|---|---|
| `diab_his_cli_lab_orders` | `0031_create_lab_rad_orders.sql` | Dapper: `ClsHandlers` (**luồng kê chỉ định — nguồn ghi chính**, 6 chỗ), `ClsRoundHandlers`, `ClsPdfHandlers`, `BhytXmlSql`, `BillingCalculatorImpl`, `ProcessInboundJob`, `LabIntegrationHandlers`, Domain `ClsOrder.cs` |
| `diab_his_lab_orders` | `9004_create_labrad.sql` | EF `LabRadConfiguration.ToTable`, `_db.LabOrders` trong `LabResultHandlers`, `ReportRegistry` (7 chỗ), `DatasetRegistry`, `EncounterLockGuard`, `LabResultQuestPdfExporter` |

**Bằng chứng "cả hai đều thật" (không suy đoán)** — comment ngay trong code sản phẩm
`backend/src/ProDiabHis.Infrastructure/CLS/ClsPaymentGateImpl.cs:11-19`:

```
/// repo dang ton tai song song 2 cap bang chi dinh
/// (diab_his_cli_lab_orders/diab_his_cli_rad_orders tu 0031 va
///  diab_his_lab_orders/diab_his_rad_orders tu 9004). Gate tra cuu round_id o ca hai
private static readonly string[] LabTables = { "diab_his_cli_lab_orders", "diab_his_lab_orders" };
```

Gate thanh toán CLS đã phải **query cả 2 bảng** để chạy được — đây là workaround, không phải thiết kế.

**Chọn `cli_lab_orders`** vì nó là bảng của **luồng ghi chính** (kê chỉ định) và là bảng mà 3 tích hợp
downstream quan trọng nhất đang đọc: BHYT XML 4210, tính viện phí, PDF chỉ định.

**Hệ quả nghiệp vụ đang xảy ra (BUG THẬT, cần xác nhận trên DB production):** chỉ định tạo ở
`cli_lab_orders` nhưng `LabResultHandlers` (nhập kết quả) lại tra `_db.LabOrders` → `lab_orders`
⇒ nhập kết quả XN có thể **không tìm thấy chỉ định vừa tạo**. Báo cáo (`ReportRegistry`) đọc
`lab_orders` ⇒ **số liệu báo cáo XN nhiều khả năng thiếu/bằng 0**.

**Hành động:** sửa EF `LabRadConfiguration`, `LabResultHandlers`, `ReportRegistry`, `DatasetRegistry`,
`EncounterLockGuard`, `LabResultQuestPdfExporter` → trỏ `diab_his_cli_lab_orders`. Trước khi drop
`diab_his_lab_orders` **phải `SELECT COUNT(*)` trên production**; nếu > 0 thì viết migration copy dữ
liệu sang `cli_lab_orders` (map cột do backend chốt), rồi mới `RENAME TO _deprecated_lab_orders`
(giữ 1 sprint, không DROP ngay).

### 1.2 `diab_his_rad_orders` vs `diab_his_cli_rad_orders` — **CANONICAL: `diab_his_cli_rad_orders`**

Hoàn toàn đối xứng với 1.1. `cli_rad_orders` (`0031`) = luồng kê chỉ định CĐHA (`ClsHandlers`, 6 chỗ),
BHYT, billing, PDF. `rad_orders` (`9004`) = EF + `RadResultHandlers` (6 chỗ, nhập kết quả CĐHA) +
`ReportRegistry` + `PortalMeHandlers` (cổng bệnh nhân).

**Rủi ro cao hơn cặp XN:** `PortalMeHandlers` trả kết quả CĐHA cho **bệnh nhân** từ `rad_orders`
⇒ bệnh nhân có thể không thấy chỉ định đã tạo ở `cli_rad_orders`.

Hành động: như 1.1, sửa code trỏ về `cli_rad_orders`; kiểm đếm dữ liệu `rad_orders` trước khi loại bỏ.

### 1.3 `diab_his_pat_allergies` vs `diab_his_cli_allergies` — **CANONICAL: `diab_his_cli_allergies`**

| | tạo bởi | ai dùng |
|---|---|---|
| `diab_his_pat_allergies` | `9002_create_patient.sql` | EF `PatientConfiguration`, `_db.Allergies` — **UI hồ sơ bệnh nhân thêm/sửa/xem dị ứng** (`PatientCommandHandler:284,302`, `PatientQueryHandler:143`) |
| `diab_his_cli_allergies` | `9049_create_cli_allergies_v2.sql` | Dapper `CdssEngineImpl:92` — **CDSS cảnh báo thuốc–dị ứng** |

**ĐÂY LÀ CẶP NGUY HIỂM NHẤT VỀ AN TOÀN NGƯỜI BỆNH.** Dị ứng do lễ tân/bác sĩ nhập qua UI ghi vào
`pat_allergies`, nhưng CDSS kiểm tra chống chỉ định lại đọc `cli_allergies` ⇒ **CDSS gần như không bao
giờ thấy dị ứng đã khai báo ⇒ cảnh báo dị ứng thuốc âm tính giả**. Phải ưu tiên sửa.

**Chọn `cli_allergies`** vì là superset: có `allergen_type`, `allergen_ingredient` (hoạt chất chuẩn hóa
để so khớp), `atc_code`, `is_active`, `updated_by`, severity có mức `LIFE_THREATENING`. Cả 2 bảng đều
`id CHAR(36)` + `tenant_id INT` + `patient_id CHAR(36)` nên **di trú dữ liệu được**:
`pat_allergies.allergen → cli_allergies.allergen_name`, giữ nguyên `id/tenant_id/patient_id/reaction/
severity/note/created_*`; `allergen_ingredient` để NULL và cần chuẩn hóa lại (nhân công hoặc job).

Lưu ý FK: `pat_allergies` có `FOREIGN KEY (patient_id) → diab_his_pat_patients(id) ON DELETE CASCADE`;
`cli_allergies` **không có FK** — khi chốt canonical nên bổ sung FK tương đương cho `cli_allergies`.

FHIR: bảng này map `AllergyIntolerance` (chưa được nêu ở đâu trong repo — cần bổ sung khi refactor).

### 1.4 `diab_his_cls_uploads` vs `diab_his_fil_cls_uploads` — **CANONICAL: `diab_his_fil_cls_uploads`**

Cặp này **đã được kết luận sẵn trong repo**, chỉ cần chính thức hóa. `db/migrations/9062_fix_cls_uploads_guid.sql:13-14`
ghi nguyên văn:

```
-- Luu y: bang diab_his_cls_uploads (EF DbSet cu, khong ai dung) GIU NGUYEN,
--        khong dung toi trong migration nay.
```

- `diab_his_fil_cls_uploads`: tạo ở `0006`, **rebuild đúng kiểu GUID ở `9062`**, là bảng mà
  `FileHandlers.cs` (7 chỗ, toàn bộ luồng upload/tải/xóa tài liệu CLS) và Domain `ClsUpload.cs` dùng.
- `diab_his_cls_uploads`: tạo ở `9004`, chỉ có EF `ClsUploadConfiguration:151` trỏ tới — **không luồng
  nghiệp vụ nào dùng**. Đây là bảng chết.

**Kết luận:** `ClsUploadConfiguration.ToTable("diab_his_cls_uploads")` là **EF config sai** → sửa
thành `diab_his_fil_cls_uploads`; `diab_his_cls_uploads` đưa vào danh sách loại bỏ.

**BUG MIGRATION kèm theo (ưu tiên cao):** `9011_create_missing_tables.sql:179` chạy
`CREATE OR REPLACE VIEW diab_his_fil_cls_uploads AS SELECT * FROM diab_his_cls_uploads;`
trong khi `diab_his_fil_cls_uploads` **đã là TABLE** (tạo ở `0006`) ⇒ lỗi MySQL **1347 "is not VIEW"**,
đúng lỗi mà `APPLY_ORDER.md` đã ghi nhận. **Phải xóa dòng 179 của `9011`** (và 4 dòng VIEW BHYT ở
`9011:173-176` cũng cùng loại lỗi — xem mục 3).

### 1.5 `diab_his_pha_dispenses` vs `diab_his_pha_dispense_records` — **CANONICAL: `diab_his_pha_dispense_records`**

| | tạo bởi | ai dùng |
|---|---|---|
| `diab_his_pha_dispense_records` | `0038_create_dispense_records.sql` + `9011` | `DispensingHandlers` (**11 chỗ — toàn bộ luồng cấp phát thuốc**), `BillingCalculatorImpl` (3 chỗ, tính tiền thuốc) |
| `diab_his_pha_dispenses` | `9005_create_pharmacy.sql` | EF `PharmacyConfiguration:152` + Domain `Dispense.cs` — **`_db.Dispenses` KHÔNG được tham chiếu ở bất kỳ handler nào** (grep `\.Dispenses` = 0 kết quả nghiệp vụ) |

**Kết luận:** `diab_his_pha_dispenses` là bảng chết do EF khai thừa. `PharmacyConfiguration` là **EF
config sai** → trỏ `diab_his_pha_dispense_records`, hoặc bỏ hẳn entity `Dispense` khỏi `AppDbContext`.
Rủi ro di trú: thấp (bảng nhiều khả năng 0 dòng — vẫn phải `COUNT(*)` xác nhận trước khi loại).

### Bảng tóm tắt 5 cặp

| # | Cặp | Loại | Tên chuẩn | Bên phải sửa | Rủi ro dữ liệu |
|---|---|---|---|---|---|
| 1 | lab_orders / cli_lab_orders | (c) | `diab_his_cli_lab_orders` | EF + 6 file code | **CAO** — 2 luồng ghi/đọc lệch nhau |
| 2 | rad_orders / cli_rad_orders | (c) | `diab_his_cli_rad_orders` | EF + 4 file code | **CAO** — ảnh hưởng cả portal bệnh nhân |
| 3 | pat_allergies / cli_allergies | (c) | `diab_his_cli_allergies` | EF Patient + 3 handler | **NGHIÊM TRỌNG** — CDSS dị ứng âm tính giả |
| 4 | cls_uploads / fil_cls_uploads | (c) | `diab_his_fil_cls_uploads` | EF `ClsUploadConfiguration` | Thấp (bảng EF chết) |
| 5 | pha_dispenses / pha_dispense_records | (c) | `diab_his_pha_dispense_records` | EF `PharmacyConfiguration` | Thấp (bảng EF chết) |

---

## 2. CẶP LỆCH THỨ 6 (chưa được nêu trong task, phát hiện thêm) — EMR content

Tồn tại **3** bảng cùng nghiệp vụ "nội dung bệnh án":

| Tên | Tạo bởi | Code dùng |
|---|---|---|
| `diab_his_enc_emr_contents` | `9003_create_encounter.sql` | EF `EncounterConfiguration:110` (`ToTable`, fluent → **thắng**) |
| `diab_his_cli_emr_contents` | `9006b_create_ext_tables.sql:369` | không file nào |
| `diab_his_cli_emr_content` (số ít) | `0027_create_emr_signatures.sql:11` | attribute `[Table]` trên `Domain/Entities/EmrContent.cs:5` — **bị fluent config override, thực tế chết** |

**Tên chuẩn: `diab_his_enc_emr_contents`.** Hai bảng còn lại đưa vào danh sách loại bỏ; attribute
`[Table("diab_his_cli_emr_content")]` trên `EmrContent.cs` phải xóa vì gây hiểu nhầm (đọc code tưởng
ghi vào bảng A nhưng EF ghi vào bảng B).

---

## 3. Bảng code query mà KHÔNG tồn tại trong DB

**KẾT QUẢ: KHÔNG CÓ.** Đã đối chiếu từng bảng trong số 89 bảng Dapper với tập `CREATE TABLE` của
`db/migrations/*.sql` — **tất cả 89 đều có migration tạo ra**. Không tái diễn bug kiểu
`BhytXmlGeneratorImpl` ở lần trước.

Riêng 2 tên nghi vấn được nêu trong task đã kiểm chứng và **là artifact của regex trích xuất, không
phải bảng thật, không phải bug**:

| Tên trong `dapper_tables.txt` | Sự thật |
|---|---|
| `diab_his_dict_icd` | Tên thật là `diab_his_dict_icd10` (tạo ở `0018`, `0028`). Grep `diab_his_dict_icd\b` = **0 kết quả** trong toàn repo. Regex đã cắt mất hậu tố `10`. |
| `diab_his_ref_icd` | Tên thật là `diab_his_ref_icd10` (tạo ở `9007`). Code dùng ở `ReportRegistry.cs:1403`, `ReportHandlers.cs:486,641,710`. Cùng lỗi cắt regex. |

> **Cảnh báo cho DevOps:** file `C:\tmp\dapper_tables.txt` có lỗi cắt hậu tố số. **Không dùng file đó
> làm đầu vào trực tiếp để sinh baseline** — dùng mục 5 dưới đây.

### Bug migration chặn dựng baseline (ƯU TIÊN CAO, không phải bảng thiếu mà là lỗi apply)

| File | Dòng | Lỗi | Xử lý đề xuất |
|---|---|---|---|
| `9011_create_missing_tables.sql` | 173–176 | `CREATE OR REPLACE VIEW diab_his_int_bhyt_exports/_export_items/_reconcile_items/_reconcile_uploads` nhưng các tên này **đã là TABLE** (tạo ở `0012`, `0046`) → lỗi 1347 | Xóa 4 dòng VIEW. Canonical là **TABLE `diab_his_int_bhyt_*`** (`0012`/`0046`); các bảng `diab_his_bhyt_*` (không có `int_`) ở `9006b` là bản trùng → loại bỏ |
| `9011_create_missing_tables.sql` | 179 | như mục 1.4 → lỗi 1347 | Xóa dòng 179 |
| dải `0018`–`0066` seed permission | — | `INSERT/ALTER` vào `diab_his_sec_*` **trước khi** `9001` tạo bảng | Đổi tên file seed sang dải `9xxx` (sau `9001`) hoặc gộp vào `9001+`. **Đây là quyết định thứ tự, không phải quyết định tên bảng — vẫn thuộc thẩm quyền architect, sẽ ra ADR riêng.** |

---

## 4. Bảng chỉ dùng Dapper (EF không định nghĩa) — CHẤP NHẬN ĐƯỢC

Trong 44 bảng "EF không biết", **100% đều tồn tại trong `db/migrations/`**. Đây là các bảng phục vụ
đọc/ghi bằng Dapper thuần (read model, danh mục, log, tích hợp) — đúng với kiến trúc đã chốt trong
`CLAUDE.md` (Dapper cho read, EF cho write/migration). **Không cần thêm entity EF**, nhưng
**bắt buộc phải có mặt trong baseline schema**:

`diab_his_api_request_logs`, `diab_his_bil_cash_out`, `diab_his_bil_counters`,
`diab_his_cdss_alert_events`, `diab_his_cdss_alert_override_log`, `diab_his_cdss_ddi_pairs`,
`diab_his_cdss_rules`, `diab_his_cli_ai_suggestion_log`, `diab_his_cli_care_pathway_target`,
`diab_his_cli_followup_recall`, `diab_his_cli_patient_risk_flag`, `diab_his_cls_order_rounds`,
`diab_his_dict_icd10`, `diab_his_dict_lab_tests`, `diab_his_dict_rad_procedures`,
`diab_his_int_dtqg_credentials`, `diab_his_int_dtqg_submissions`, `diab_his_pha_ddi_rules`,
`diab_his_pha_drug_categories`, `diab_his_pha_prescription_print_history`,
`diab_his_pha_purchase_order_items`, `diab_his_pha_stock_movements`, `diab_his_pha_stocktakes`,
`diab_his_pha_stocktake_items`, `diab_his_ptl_med_reminders`, `diab_his_rad_results`,
`diab_his_rcp_queue_tickets`, `diab_his_rcp_ticket_reassignments`, `diab_his_ref_icd10`,
`diab_his_rep_dashboards`, `diab_his_rep_definitions`, `diab_his_rep_schedules`,
`diab_his_sch_appointments`, `diab_his_sch_doctor_schedules`, `diab_his_sch_schedule_blocks`,
`diab_his_sec_encryption_keys`, `diab_his_sys_code_master`, `diab_his_sys_code_detail`,
`diab_his_sys_feature_flags`, `diab_his_sys_rooms`
(+ `diab_his_cli_allergies`, `diab_his_cli_lab_orders`, `diab_his_cli_rad_orders`,
`diab_his_pha_dispense_records` — 4 bảng thuộc 5 cặp ở mục 1, sau khi sửa EF sẽ có entity).

**Yêu cầu bắt buộc:** mọi query Dapper trên các bảng trên phải có `WHERE tenant_id = @tenantId`
(CLAUDE.md mục 3). Cần một task QC riêng rà lại 120 file Dapper — **chưa nằm trong phạm vi tài liệu này**.

---

## 5. DANH SÁCH TÊN BẢNG CHUẨN cho baseline schema (DevOps dùng)

Quy tắc: **mọi bảng đều có prefix `diab_his_`**. Tên không prefix chỉ được phép tồn tại dưới dạng
VIEW tương thích (`9009`, `9022`, `9061`) và **không được dùng trong code mới**.

### 5.1 Bảng chuẩn — GIỮ

| Nhóm | Bảng |
|---|---|
| Hệ thống / tenant | `diab_his_sys_tenants`, `diab_his_sys_clinics`, `diab_his_sys_branches`, `diab_his_sys_rooms`, `diab_his_sys_code_master`, `diab_his_sys_code_detail`, `diab_his_sys_feature_flags` |
| Bảo mật | `diab_his_sec_users`, `diab_his_sec_roles`, `diab_his_sec_permissions`, `diab_his_sec_user_roles`, `diab_his_sec_role_permissions`, `diab_his_sec_sessions`, `diab_his_sec_audit_logs`, `diab_his_sec_encryption_keys` |
| Bệnh nhân | `diab_his_pat_patients`, `diab_his_pat_insurances`, `diab_his_pat_emergency_contacts`, `diab_his_pat_consents`, `diab_his_pat_portal_accounts`, `diab_his_pat_portal_otp_log`, `diab_his_pat_portal_sessions` |
| Tiếp đón | `diab_his_rcp_queue_tickets`, `diab_his_rcp_ticket_reassignments` |
| Lịch hẹn | `diab_his_sch_appointments`, `diab_his_sch_doctor_schedules`, `diab_his_sch_schedule_blocks` |
| Khám bệnh | `diab_his_enc_encounters`, `diab_his_enc_diagnoses`, `diab_his_enc_vital_signs`, `diab_his_enc_emr_contents` |
| Lâm sàng | `diab_his_cli_allergies`, `diab_his_cli_emr_templates`, `diab_his_cli_emr_versions`, `diab_his_cli_emr_signatures`, `diab_his_cli_encounter_addenda`, `diab_his_cli_diabetes_assessments`, `diab_his_cli_diabetes_templates`, `diab_his_cli_care_pathway_target`, `diab_his_cli_followup_recall`, `diab_his_cli_patient_risk_flag`, `diab_his_cli_ai_suggestion_log` |
| CLS | `diab_his_cli_lab_orders`, `diab_his_lab_results`, `diab_his_cli_rad_orders`, `diab_his_rad_results`, `diab_his_cls_order_rounds`, `diab_his_fil_cls_uploads` |
| CDSS | `diab_his_cdss_rules`, `diab_his_cdss_ddi_pairs`, `diab_his_cdss_alert_events`, `diab_his_cdss_alert_override_log` |
| Dược | `diab_his_pha_drugs`, `diab_his_pha_drug_categories`, `diab_his_pha_stock`, `diab_his_pha_stock_movements`, `diab_his_pha_stocktakes`, `diab_his_pha_stocktake_items`, `diab_his_pha_prescriptions`, `diab_his_pha_prescription_items`, `diab_his_pha_prescription_print_history`, `diab_his_pha_dispense_records`, `diab_his_pha_dispense_items`, `diab_his_pha_ddi_rules`, `diab_his_pha_suppliers`, `diab_his_pha_purchase_orders`, `diab_his_pha_purchase_order_items`, `diab_his_pha_grn` |
| Thu ngân | `diab_his_bil_billing`, `diab_his_bil_billing_items`, `diab_his_bil_payments`, `diab_his_bil_qr_codes`, `diab_his_bil_einvoices`, `diab_his_bil_cashier_shifts`, `diab_his_bil_cash_out`, `diab_his_bil_counters`, `diab_his_bil_services`, `diab_his_bil_service_packages`, `diab_his_bil_service_package_items` |
| Tích hợp | `diab_his_int_bhyt_exports`, `diab_his_int_bhyt_export_items`, `diab_his_int_bhyt_reconcile_uploads`, `diab_his_int_bhyt_reconcile_items`, `diab_his_int_dtqg_credentials`, `diab_his_int_dtqg_submissions`, `diab_his_int_lab_partners` |
| API partner | `diab_his_api_partners`, `diab_his_api_request_logs` |
| Thông báo | `diab_his_nti_notifications`, `diab_his_nti_preferences`, `diab_his_nti_web_push_subs`, `diab_his_nti_vapid_keys` |
| Portal BN | `diab_his_ptl_med_reminders` |
| Danh mục | `diab_his_dict_icd10`, `diab_his_dict_lab_tests`, `diab_his_dict_rad_procedures`, `diab_his_ref_icd10` |
| Báo cáo | `diab_his_rep_definitions`, `diab_his_rep_dashboards`, `diab_his_rep_schedules` |
| File | `fil_files` ⚠️ **vi phạm quy ước prefix** (tạo ở `9062`, code `FileHandlers.cs` đang dùng). Đề xuất đổi thành `diab_his_fil_files` ở migration sau — **nhưng KHÔNG làm chung với đợt sửa 5 cặp** để tách rủi ro. Baseline hiện tại giữ nguyên tên `fil_files`. |

### 5.2 Bảng LOẠI BỎ (deprecated — không đưa vào baseline mới)

| Bảng | Lý do |
|---|---|
| `diab_his_lab_orders` | trùng `diab_his_cli_lab_orders` (mục 1.1) |
| `diab_his_rad_orders` | trùng `diab_his_cli_rad_orders` (mục 1.2) |
| `diab_his_pat_allergies` | trùng `diab_his_cli_allergies` (mục 1.3) |
| `diab_his_cls_uploads` | trùng `diab_his_fil_cls_uploads` (mục 1.4) |
| `diab_his_pha_dispenses` | trùng `diab_his_pha_dispense_records` (mục 1.5) |
| `diab_his_cli_emr_contents`, `diab_his_cli_emr_content` | trùng `diab_his_enc_emr_contents` (mục 2) |
| `diab_his_bhyt_exports`, `diab_his_bhyt_export_items`, `diab_his_bhyt_reconcile_uploads`, `diab_his_bhyt_reconcile_items` | bản trùng của `diab_his_int_bhyt_*` (mục 3) |
| `diab_his_dict_icd10` **hoặc** `diab_his_ref_icd10` | **CHƯA QUYẾT ĐỊNH** — xem mục 6 |

**Quy tắc loại bỏ:** không `DROP` ngay. Trình tự bắt buộc:
`COUNT(*)` trên production → nếu > 0 thì migration copy dữ liệu → sửa code → chạy 1 sprint →
`RENAME TO zz_deprecated_<ten>` → sprint sau mới `DROP`.

---

## 6. Việc CHƯA quyết định được (ghi rõ, không đoán)

1. **`diab_his_dict_icd10` vs `diab_his_ref_icd10`** — cặp lệch thứ 7, cùng là danh mục ICD-10, cả hai
   đều tồn tại và **cả hai đều đang được code dùng**: `dict_icd10` (seed `0018`/`0028`, `Icd10Handlers`
   đọc `is_billable`) và `ref_icd10` (seed `9007`, dùng trong `ReportRegistry`, `ReportHandlers`).
   Chưa chốt vì chưa biết bảng nào có bộ mã đầy đủ hơn trên production — **cần DevOps chạy
   `SELECT COUNT(*)` trên cả hai rồi báo lại**, sau đó tôi ra ADR.
2. **Số dòng thực tế của các bảng deprecated trên production** — không truy cập được DB từ máy này.
   Toàn bộ kết luận ở mục 1 dựa trên migration + code, **chưa xác minh bằng dữ liệu runtime**.
   Trước khi backend sửa code, DevOps phải cung cấp `COUNT(*)` của 5 cặp (10 bảng).
3. **Thứ tự apply migration `0xxx` vs `9xxx`** — đã xác định là nguyên nhân gốc nhưng phương án sửa
   (đổi tên file vs viết lại chain vs sinh baseline một file duy nhất) sẽ ra **ADR riêng**, chưa chốt ở đây.
4. **Mapping FHIR R4** cho các bảng chuẩn mới — mới xác định `diab_his_cli_allergies` →
   `AllergyIntolerance`. Các bảng còn lại chưa rà soát trong lượt này.

---

## 7. Thứ tự ưu tiên xử lý đề xuất

| Ưu tiên | Việc | Lý do |
|---|---|---|
| **P0** | Hợp nhất dị ứng về `diab_his_cli_allergies` | An toàn người bệnh — CDSS đang bỏ sót dị ứng |
| **P0** | Xóa 5 dòng `CREATE OR REPLACE VIEW` lỗi ở `9011` (dòng 173–176, 179) | Chặn dựng baseline (lỗi 1347) |
| **P1** | Hợp nhất `cli_lab_orders` / `cli_rad_orders` | Sai lệch dữ liệu chỉ định ↔ kết quả ↔ báo cáo ↔ portal |
| **P1** | DevOps cung cấp `COUNT(*)` 10 bảng + 2 bảng ICD | Đầu vào bắt buộc cho P0/P1 và mục 6.1 |
| **P2** | Sửa EF `ClsUploadConfiguration`, `PharmacyConfiguration`, xóa attribute `[Table]` ở `EmrContent.cs` | Bảng chết, rủi ro thấp nhưng gây hiểu nhầm khi đọc code |
| **P3** | Đổi `fil_files` → `diab_his_fil_files` | Vi phạm quy ước, không gây lỗi chức năng |
