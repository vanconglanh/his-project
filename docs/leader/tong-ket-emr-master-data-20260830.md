# Tổng kết — Master data thuốc + Tham chiếu diaB + EMR template hoá

- **Ngày**: 2026-08-30
- **Nguồn thiết kế (đã BO chốt)**: `docs/prd/kien-truc-master-data-package-emr-20260830.md` (v1.2)
- **Branch**: develop
- **Commit**: `a742a84` (Mục 3), `eb55093` (Mục 4), `04a78d9` (Mục 5), + commit docs

## ✅ Đã xong

### Mục 3 — Master data thuốc chuẩn BYT
- Migration `db/migrations/9180_drug_route_bhyt_code.sql` (đã bỏ `.draft`, đã áp DB thật): thêm `route`, `bhyt_code` + index; SYNC 1 chiều 9010→9005 (PRE-CHECK: 30 dòng, tất cả `*_need_sync=0` → no-op an toàn); backfill route từ lịch sử kê đơn.
- `ClosedXmlImporter.cs`: sửa lỗi ghi NHẦM bộ cột 9010 → nay ghi đúng bộ **9005** (`name/drug_form/sell_price/requires_rx/is_controlled`) + `route`; vẫn đồng bộ 9010 legacy để báo cáo COALESCE không mất dữ liệu; INSERT vào bảng canonical `diab_his_pha_drugs` (view `pha_drug_master`/9009 không expose cột mới).
- `BhytXmlGeneratorImpl.cs` + `BhytXmlSql.cs`: bỏ hardcode `"uong"`; route lấy theo `prescription_items.route → drugs.route`; cả 2 rỗng → KHÔNG phát hành XML, trả `DRUG_ROUTE_MISSING` kèm danh sách thuốc thiếu.
- `Drug.cs` + `PharmacyConfiguration`: map `Route`, `BhytCode`.
- **Xác nhận nghi vấn bảng trùng**: `pha_drug_master` là **VIEW** (`SELECT ...` từ `diab_his_pha_drugs`), KHÔNG phải bảng riêng → không có nợ kỹ thuật thứ hai.

### Mục 4 — Tham chiếu lộ trình diaB (chỉ phần HIS)
- `IExternalPathwayProvider` (Application) + `NullExternalPathwayProvider` (Infrastructure/Integrations/Diab) — luôn trả `NotConfigured`, không gọi mạng, không lỗi.
- `GET /api/v1/patients/{id}/external-pathway` — LUÔN trả HTTP 200 kèm `status`, không chặn luồng khám.
- Fix **T1** (walk-in không trừ định mức): hook `ConsumeAsync(source_type='ENCOUNTER')` vào lúc bác sĩ MỞ khám (không phải check-in); `idempotency_key = ENCOUNTER:{id}:VISIT`; gộp luồng Appointment về cùng key → 2 luồng hội tụ 1 lần trừ duy nhất; hết định mức KHÔNG chặn khám; ghi `covered_by_subscription_id`.

### Mục 5 — EMR template hoá
- Migration `9181` (gộp `diabetes_templates`→`EmrTemplate`, `structured_json`, bảng nối `emr_template_package_map`, `covered_by_subscription_id`) + `9182` (`template_id`/`structured_values_json`/`schema_snapshot_json`). Cả 2 đã áp DB thật.
- **QĐ4**: tách giá trị form (`structured_values_json` trên bệnh án) khỏi định nghĩa template (`structured_json`).
- **QĐ5**: SaveDraft chụp `structured_json` của template vào `schema_snapshot_json` ngay khi tạo version; render LUÔN theo snapshot, không đọc lại template hiện tại.
- **Hash chữ ký v2** (`EmrSignPayload.Build/BuildV1/BuildV2`): gộp `content_json + structured_values_json + schema_snapshot_json`; giữ đường verify v1 cho bệnh án cũ (2 cột NULL); không backfill, không ký lại hàng loạt.
- FE `DynamicFormRenderer` (8 loại field theo `type`, nhóm `group`, layout `colSpan`) gắn màn khám (chọn template → render form) + màn admin (nhập `structured_json` có validate + preview).

## 🔍 Đã verify thế nào
- `cd backend && dotnet build` → **0 error, 0 warning**.
- `cd backend && dotnet test` → **895 pass, 0 fail** (884 unit + 6 arch + 5 integration). `EmrSignFlowTests` **9/9** — gồm case `V2_TamperStructuredValues_AfterSign_VerifyFails`, `V2_TamperSchemaSnapshot_AfterSign_VerifyFails` (sửa dữ liệu sau ký → verify FAIL đúng thiết kế), `V1_Record_NullColumns_UsesV1Payload_VerifyOk`.
- `cd frontend && npx tsc --noEmit` → sạch (exit 0).
- Migration áp trên DB thật `prodiab_his`, verify đủ cột/bảng mới bằng `information_schema`.
- **Kịch bản SNAPSHOT (trọng tâm)** — chạy qua API thật trên stack local đã rebuild code mới (evidence JSON `docs/qc/evidence-emr-template-master-data-20260830/01..07`): tạo template S1 → bác sĩ dùng + nhập giá trị → ký (`signed_at` set) → mở lại `schema_snapshot == S1`; **sửa template gốc sang S2** → mở lại bệnh án cũ **`schema_snapshot` VẪN == S1** (không rò S2), `structured_values` không đổi; template hiện tại đã đổi thật sang S2. **KẾT LUẬN: PASS**.
- **Mục 4** endpoint verify thật: `08-external-pathway.json` → HTTP 200, `status=NOT_CONFIGURED`, không lỗi.
- Browser screenshot bổ sung: `docs/qc/evidence-emr-template-master-data-20260830/` (do qc-agent chụp).

## ⚠️ Giả định đã dùng
- Encounter mở khám = điểm chung của cả walk-in lẫn có hẹn (đúng theo §4.7.5); nếu quy trình thực tế còn luồng tạo Encounter khác không đi qua điểm này thì cần rà thêm — hiện đã hook tại `EncounterHandlers` (mở khám).
- Importer ghi cả 2 bộ cột (9005 nguồn sự thật + 9010 legacy) để báo cáo cũ đọc COALESCE không gãy; khi dọn `ReportRegistry`/`ReportingServiceImpl` xong mới DROP bộ 9010 (đợt sau, đã ghi backlog trong tài liệu §6 mục 10).
- View `pha_drug_master` (9009) chưa expose `route`/`bhyt_code` — báo cáo hiện chưa cần; nếu cần đọc route qua view thì recreate view (follow-up nhỏ).

## ❌ Chưa làm / còn tồn (đúng phạm vi thiết kế)
- **Gọi API diaB thật (Mục 4)**: BỊ CHẶN bởi phụ thuộc bên ngoài — diaB CHƯA có endpoint `GET /api/integration/his/patient-pathway` (§4.7.1). Đã code sẵn `IExternalPathwayProvider` để cắm `DiabPathwayProvider` khi diaB bổ sung endpoint + cơ chế auth (Q4.6, Q4.7).
- DROP bộ cột 9010 + dọn `ReportRegistry.cs`/`ReportingServiceImpl.cs` — để đợt sau (tránh gãy báo cáo), theo backlog §6.
- Deprecate COMMENT cho 6 cột 9010 (phần (D) trong 9180) đang comment-out (cần lấy đúng kiểu từ `SHOW CREATE TABLE`) — không bắt buộc, làm cùng đợt DROP.

## 👉 Cần user / BO quyết (không chặn bàn giao)
- **Q4.6 / Q4.7**: chốt với team diaB việc bổ sung endpoint lộ trình + cơ chế auth (API key theo tenant) — điều kiện để go-live phần gọi thật.
- Các câu hỏi mở còn lại của thiết kế (Q3.1 file danh mục chuẩn, Q3.6 có hợp đồng BHYT không, Q5.2 phụ lục TT 32/2023) — ảnh hưởng các đợt master data tiếp theo, không chặn phần đã giao.

## Ghi chú kỹ thuật
- Phát hiện phụ (KHÔNG phải regression của đợt này): Swagger generation lỗi 500 do `DrugsController.Import` dùng `[FromForm] IFormFile` không có annotation Swashbuckle — đây là nợ có sẵn từ trước (file không nằm trong diff đợt này). App vẫn chạy bình thường; chỉ trang swagger UI bị ảnh hưởng. Đề xuất fix riêng (thêm `[Consumes("multipart/form-data")]` / operation filter).
