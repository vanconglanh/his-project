# PO Review — Tích hợp diaB vào Pro-Diab HIS (2026-08-30)

- **Người review**: PO (vai trò)
- **Ngày**: 2026-08-30
- **Nhánh**: develop
- **Phiên bản tài liệu thiết kế tham chiếu**: `docs/prd/kien-truc-master-data-package-emr-20260830.md` v1.2
- **Phương pháp**: đọc code thật, không dựa vào tài liệu mô tả (tài liệu ghi lại kỳ vọng; code thật là bằng chứng)

---

## 1. Bảng đối chiếu chi tiết

| Yêu cầu BO | Đã làm gì (bằng chứng code thật) | Đủ chưa | Gap còn lại (nếu có) | Cần làm gì tiếp |
|---|---|---|---|---|
| **YC1a** — Hiển thị bệnh nhân đang dùng dịch vụ nào, còn bao nhiêu định mức | `GetPatientSummaryAsync` → `GET /api/v1/patients/{id}/package-summary`; `diab_his_pkg_usage_logs` ghi `source_type/source_id/subscription_id/balance_id`; `bil_billing_items.covered_by_subscription_id` | ✅ Đủ | — | — |
| **YC1b** — Đánh dấu lượt khám thuộc gói nào | `PackageUsageLog` + `bil_billing_items.covered_by_usage_log_id` (migration 9093). Truy ngược 2 chiều. `diab_his_enc_encounters.covered_by_subscription_id` được thêm trong **9181 (DRAFT, chưa chạy)** | ⚠️ Gần đủ | **Migration 9181 CHƯA CHẠY** → cột `covered_by_subscription_id` trên bảng encounter chưa tồn tại → badge "Thuộc gói X" trên UI danh sách lượt khám phải join chậm, không thể lọc nhanh | Chạy migration 9181 (ưu tiên cao) |
| **YC1c** — Quyết định có tính phí không | `ConsumeAsync` trả `covered_quantity`/`excess_quantity`/`covered_amount`; `BillingCalculatorImpl` áp discount 100% phần covered, tính phí phần excess. Chống double-consume bằng `UNIQUE uq_usage_idem` | ✅ Đủ | — | — |
| **YC1d** — Gap T1: walk-in không qua Appointment thất thoát định mức | `StartEncounterCommandHandler.TryConsumeVisitAsync` (`EncounterHandlers.cs:280-284,328`) gọi `ConsumeAsync(source_type='ENCOUNTER', source_id=encounterId)` tại điểm `WAITING→IN_PROGRESS`, dùng `idempotency_key` hội tụ cả 2 luồng. Comment code: *"Diem nay chung cho CA walk-in LAN co hen => 2 luong hoi tu, khong con that thoat walk-in"* | ✅ Đã fix (confirm qua code) | — | QC phải test: BN có gói walk-in → phải trừ 1 lượt; BN có gói có hẹn → trừ đúng 1 lượt (không 2) |
| **YC2** — Gọi API diaB real-time khi bác sĩ mở màn khám | `IExternalPathwayProvider` + `ExternalPathwayResult/Query` (file `IExternalPathwayProvider.cs`); `NullExternalPathwayProvider` luôn trả `NotConfigured`; endpoint `GET /api/v1/patients/{id}/external-pathway` luôn HTTP 200 + `data.status`; handler có try/catch bao bọc provider (`ExternalPathwayHandlers.cs:53-65`) | ✅ Interface sẵn sàng | **diaB CHƯA có endpoint** (đã biết, blocked bên ngoài). `FromCache: false` hardcode — Redis cache chưa implement. Circuit breaker Polly chưa thấy trong `NullExternalPathwayProvider` (chưa cần vì chưa gọi thật). `DiabPathwayProvider` chưa có | Khi diaB có endpoint: thêm `DiabPathwayProvider` (HttpClient + Polly), thêm Redis cache TTL 5 phút, đăng ký DI thay `NullExternalPathwayProvider`. **Không cần sửa UI/Application** |
| **YC3a** — EMR đa khoa, bác sĩ chọn template từ danh sách | `EmrTemplateSelector` trên `EmrTabPanel.tsx`; FE dùng `useEmrTemplate` hook; bác sĩ chọn tay hoặc hệ thống gợi ý. `SPECIALITY_LABELS` có 7 chuyên khoa gồm DIABETES | ✅ Cơ chế chọn có. `DynamicFormRenderer` đã gắn và verify PASS | `EmrTemplateResolver` (auto-resolve từ gói → template) và API `/emr-templates/resolve` (§5.3.4) **chưa thấy trong code** — bác sĩ phải chọn thủ công, không có gợi ý tự động từ gói | Implement `EmrTemplateResolver` service + endpoint (medium priority, UX enhancement) |
| **YC3b** — Template mẫu "Tư vấn gói theo dõi ĐTĐ" cụ thể | Migration 9181 (DRAFT) convert `diab_his_cli_diabetes_templates` → `diab_his_cli_emr_templates` với `speciality='DIABETES'`. Bảng nối `diab_his_cli_emr_template_package_map` được tạo trong 9181 | ⚠️ Cơ chế có, nhưng **9181 CHƯA CHẠY** và **KHÔNG có seed data mẫu template "Tư vấn gói" nào trong `db/seeds/`** | Chưa có 1 template mẫu cụ thể nào cho use-case "tư vấn gói kiểu diaB" được seed sẵn → bác sĩ không có gì để chọn cho usecase này ngay khi go-live | Tạo seed data: 1 `EmrTemplate` với `speciality='DIABETES'`, `name='Tư vấn gói theo dõi ĐTĐ'`, `is_default=1`, và gắn vào `emr_template_package_map` với gói mẫu |
| **YC3c** — Snapshot schema tại thời điểm ký | Migration 9182 (DRAFT) thêm `schema_snapshot_json` vào `diab_his_cli_emr_versions`, `structured_values_json` vào cả `emr_versions` và `emr_contents`; thiết kế đúng pattern snapshot của module gói | ⚠️ Schema đúng, nhưng **9182 CHƯA CHẠY** | Bệnh án ký sau khi có 9182 sẽ snapshot đúng. Bệnh án cũ `schema_snapshot_json=NULL` — đã ghi rõ không backfill (đúng) | Chạy migration 9182 (sau 9181) |

---

## 2. Danh sách gap thật — theo mức độ ưu tiên

### Mức đỏ (chặn go-live nếu không làm)

**GAP-R1: Migration 9181 + 9182 là DRAFT, chưa chạy**

Cả hai migration đều mang comment `DRAFT - CHUA CHAY`. Điều này có nghĩa:
- Bảng `diab_his_cli_emr_template_package_map` **chưa tồn tại** trong DB
- Cột `diab_his_enc_encounters.covered_by_subscription_id` **chưa tồn tại**
- Cột `structured_json`, `is_default`, `legacy_source` trên `diab_his_cli_emr_templates` **chưa tồn tại**
- Cột `structured_values_json`, `schema_snapshot_json` trên `emr_versions/emr_contents` **chưa tồn tại**

Đây là migration cơ sở cho toàn bộ tính năng EMR template hoá. Không chạy = mọi tính năng mới phụ thuộc vào các bảng/cột này đều lỗi runtime.

**Bằng chứng**: đuôi file `.sql` (không phải `.sql.draft`) nhưng header có `-- Migration (DRAFT - CHUA CHAY)`. Cần dev xác nhận migration runner đã bỏ qua hay chưa apply.

**Hành động**: dev chạy 9181 → 9182 (theo thứ tự dependency).

**GAP-R2: Không có seed template mẫu "Tư vấn gói ĐTĐ"**

Migration 9181 convert dữ liệu từ `diab_his_cli_diabetes_templates` sang `diab_his_cli_emr_templates`. Nhưng bảng nguồn `diab_his_cli_diabetes_templates` **có thể rỗng trên môi trường fresh** (không có seed mẫu tường minh nào trong `db/seeds/` cho loại template này).

Hệ quả thực tế: sau khi 9181 chạy, bác sĩ mở màn khám bệnh nhân thuộc gói ĐTĐ → dropdown template chỉ có template GENERAL mặc định, **không có template "Tư vấn gói"** nào để chọn → use-case diaB không có gì để demo/vận hành.

**Hành động**: tạo `db/seeds/seed_emr_template_diabetes_package.sql` với ít nhất 1 template mẫu (is_system=1, speciality='DIABETES', structured_json có các trường tư vấn gói cơ bản).

### Mức cam (cần làm sớm, không chặn hoàn toàn)

**GAP-O1: `EmrTemplateResolver` chưa được implement**

Tài liệu §5.3.3 mô tả service tự động resolve template theo `(package_id, service_id, visit_sequence)`. Nhưng tìm kiếm trong toàn bộ thư mục `Application/` không thấy class này, và API `/api/v1/emr-templates/resolve` (§5.3.4) cũng chưa có endpoint tương ứng. Hiện tại bác sĩ phải chọn thủ công từ dropdown.

Đây là UX gap, không phải bug nghiệp vụ (bác sĩ vẫn làm việc được), nhưng làm mất giá trị của bảng nối `emr_template_package_map` vừa tạo.

**GAP-O2: Redis cache cho `external-pathway` chưa implement**

`ExternalPathwayHandlers.cs:76` set `FromCache: false` hardcode. Thiết kế §4.7.3 yêu cầu cache Redis TTL 5 phút để tránh bắn request diaB mỗi lần mở/đóng tab. Khi diaB có endpoint thật, nếu không có cache → mỗi thao tác mở tab khám gọi HTTP sang diaB → latency tăng, rate-limit bị vượt.

**GAP-O3: Polly circuit breaker chưa có trong DiabPathwayProvider**

`NullExternalPathwayProvider` không cần circuit breaker (không gọi mạng). Nhưng khi code `DiabPathwayProvider` thật, cần nhớ bổ sung Polly với config §4.7.3: timeout 3s, mở circuit sau 5 lỗi/30s. Không có sẵn hạ tầng này trong Infrastructure layer hiện tại (cần kiểm tra `ServiceCollectionExtensions` xem đã register Polly chưa).

### Mức vàng (ghi nhận, không ưu tiên MVP)

**GAP-Y1: `external_pathway_stage` 6 cột của Phương án A (migration 9183) không được tạo**

BO đã bác Phương án A, chọn Phương án C. Đúng. Nhưng **migration 9183 vẫn còn dạng draft trong tài liệu** — không phải trong `db/migrations/` thật (đã kiểm tra Glob: không có file `9183_*.sql`). Không có vấn đề gì, chỉ ghi nhận để tránh nhầm.

**GAP-Y2: `CitizenId` chưa được truyền vào `ExternalPathwayQuery`**

`ExternalPathwayHandlers.cs:51`: `CitizenId: null` hardcode. Tài liệu §4.4.2 nêu rủi ro Q4.8: bệnh nhân đăng ký diaB bằng SĐT khác → không tra được. Khi có `DiabPathwayProvider` thật, nên bổ sung logic fallback tra theo CCCD nếu SĐT không khớp.

---

## 3. Kết luận tổng quan

### Đánh giá theo từng yêu cầu BO

**YC1 (hiển thị + đánh dấu + tính phí):** Phần lớn đã đủ và hoạt động tốt. Fix T1 walk-in đã có trong code thật tại `StartEncounterCommandHandler.TryConsumeVisitAsync`, idempotent đúng thiết kế. Điểm duy nhất chưa đủ là **migration 9181 chưa chạy** (cột denormalize cho badge "Thuộc gói X").

**YC2 (gọi API real-time):** Interface đã sẵn sàng và thiết kế đúng. Endpoint trả HTTP 200 mọi tình huống, graceful degradation có. Phụ thuộc bên ngoài (diaB chưa có endpoint) là rủi ro đã biết và đã có kế hoạch. Không phải gap của HIS.

**YC3 (EMR đa khoa, template):** Cơ chế có (`DynamicFormRenderer`, `EmrTemplateSelector`, bảng nối), nhưng **2 migration nền tảng chưa chạy** và **không có seed data template mẫu**.

### Verdict: Chưa đủ để go-live vận hành thực tế

Không phải do kiến trúc sai, mà do:
1. Migration 9181 + 9182 chưa apply → DB chưa có schema mới
2. Không có data mẫu → demo/UAT không có gì để show

### Ưu tiên để "đủ dùng được" (không cần hoàn hảo, chỉ cần vận hành được khi diaB có endpoint)

| Thứ tự | Việc | Loại | Thời gian ước tính |
|---|---|---|---|
| 1 | **Chạy migration 9181** (sau khi dev xác nhận idempotent + tên bảng encounter đúng) | DB ops | 30 phút |
| 2 | **Chạy migration 9182** | DB ops | 30 phút |
| 3 | **Tạo seed 1 EmrTemplate mẫu "Tư vấn gói ĐTĐ"** với `speciality='DIABETES'`, `is_system=1`, `is_default=1`, và 1 bản ghi trong `emr_template_package_map` gắn với gói mẫu | Seed data (không code) | 2-3 giờ (nội dung template) |
| 4 | **QC test fix T1 walk-in** theo kịch bản ở §4.7.5 | Test | nửa ngày |
| 5 | Khi diaB có endpoint: **code `DiabPathwayProvider`** + Redis cache + Polly, đổi DI | Backend 1 sprint | 3-5 ngày |

Bước 5 chờ team diaB — không thuộc kiểm soát của HIS. Bước 1-4 có thể làm ngay.

---

## 4. Điểm cần escalate với BO / team diaB

| ID | Nội dung | Người phải quyết |
|---|---|---|
| Q4.6 | Team diaB có đồng ý bổ sung endpoint `GET /api/integration/his/patient-pathway` không, và bao giờ? | BO + PM diaB |
| Q4.7 | Cơ chế auth HIS→diaB: API key theo tenant hay OAuth client credentials? | Team diaB + Security |
| Q4.8 | Khi SĐT bệnh nhân trong HIS khác SĐT đăng ký diaB → ai chịu trách nhiệm khớp danh tính? | BO |
| Pháp lý | Bệnh nhân có đồng ý chia sẻ dữ liệu lộ trình từ diaB sang HIS chưa? diaB có `Share-Profile` — nên tận dụng | BO + Legal |

---

*Báo cáo này dựa trên đọc code thật ngày 2026-08-30. Không có giả định. Mọi kết luận "đã có" đều có file/dòng code kèm theo.*
