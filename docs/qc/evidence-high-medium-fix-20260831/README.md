# Evidence — Fix 6 lỗi High/Med còn tồn (2026-08-31)

Sau khi 4 Blocker (BUG-01→04) đã fix ở vòng 2, phiên này dọn nốt **3 High + 3 Med** còn tồn từ QC
`docs/qc/ute-full-flow-20260831.md` §7.5. Nhánh `develop`. Baseline test: 987 → sau fix **989** (thêm 2 UT khoá bug).

## Cổng chất lượng (đã chạy thật)

| Gate | Lệnh | Kết quả |
|---|---|---|
| Build backend | `dotnet build ProDiabHis.slnx` | ✅ 0 error |
| Unit + Integration + Architecture | `dotnet test ProDiabHis.slnx` | ✅ **989 PASS / 0 FAIL** (965 unit + 17 integration + 7 arch) |
| TypeScript FE | `npx tsc --noEmit` | ✅ 0 error |

## Từng bug — nội dung fix & cách verify

### UTC-RX-05 / BUG-06 (High) — `dtqg/status` trả 500
- **Nguyên nhân:** `DtqgHandlers.cs:141` gọi `ExecuteScalarAsync<int?>` trên cột `pha_prescriptions.ID`
  là **GUID CHAR(36)** → Dapper `Convert.ToInt32("149dc4a3-…")` → `FormatException` → 500.
- **Fix:** đổi sang `ExecuteScalarAsync<string?>` + kiểm null bằng `string.IsNullOrEmpty` (theo tiền lệ
  `GuidFormat=None` đã dùng ở dòng 385 cùng file, `DispensingHandlers.cs:277`).
- **Verify (API thật, backend đã rebuild):**
  - Đơn thuốc tồn tại → **HTTP 404** `DTQG_SUBMIT_FAILED` "Chua co thong tin gui DTQG cho don thuoc nay"
    (phản hồi nghiệp vụ đúng, **không còn 500**; chứng tỏ đã qua được truy vấn dòng 141).
  - Đơn không tồn tại → **HTTP 404** `PRESCRIPTION_NOT_FOUND`.
  - DB: `SELECT ID FROM pha_prescriptions` → GUID 36 ký tự (xác nhận premise fix).

### UTC-ENC-02 / BUG-05 (High) — lượt khám gán bác sĩ = lễ tân
- **Nguyên nhân:** `EncounterHandlers.cs` `AdmitTicketToEncounterCommandHandler`:
  `DoctorId = doctorId ?? _user.UserId` → khi vé chưa gán bác sĩ, người admit (lễ tân) bị gán làm bác sĩ.
- **Fix:** `DoctorId = doctorId` (để null); bác sĩ thật được gán khi "Bắt đầu khám"
  (`StartEncounterCommandHandler`, giữ nguyên — đúng nghiệp vụ).
- **Verify (API + DB thật):**
  - Lễ tân (`letan.test`, user_id `14ca565a…`) admit 2 vé không bác sĩ → 2 encounter mới có
    **`doctor_id = NULL`**, `created_by = 14ca565a…` (lễ tân chỉ ở created_by, KHÔNG ở doctor_id).
  - Bác sĩ (`bacsi.test`, user_id `e210a28b…`) "Bắt đầu khám" → `doctor_id = e210a28b…`, status `IN_PROGRESS`
    (regression: gán đúng bác sĩ vẫn hoạt động).
  - Data test đã dọn: 2 encounter soft-delete, 2 vé trả lại `WAITING`.

### UTC-CLS-15 (High) — OCR bỏ sót XN có hậu tố mã
- **Nguyên nhân:** mã `GLU_F` (glucose lúc đói) không có trong `CodeAliases` (chỉ có `GLU`/`GLUCOSE`) →
  phiếu ghi "Glucose (đường huyết) 7.2" không khớp được alias "glucose".
- **Fix:** `LabResultOcrParser.BuildLabelCandidates` tách tiền tố trước `_`/`-` (`GLU_F`→`GLU`) để lấy alias gốc.
- **Verify:** unit test `LabResultOcrParserTests.Parse_SuffixedCode_GLU_F_FallsBackToBaseAlias` →
  `Extracted=true`, `ValueNumeric=7.2`. PASS.

### UTC-DOC-04 / BUG-09 (Med) — phiếu KQ XN thật bị phân loại Unknown
- **Nguyên nhân:** điểm LabResult chỉ dựa số XN đang chờ khớp; phòng khám nhỏ chỉ 1–2 XN → 1 khớp = 0.55
  < ngưỡng 0.6 → `Unknown`. (Ngược lại, batch nhiều XN chờ từng đẩy điểm ảo lên → PASS giả.)
- **Fix:** `DocumentClassifierService` thêm **marker cấu trúc phiếu XN** (`xet nghiem`, `ket qua`, `don vi`,
  `khoang tham chieu`…). Có ≥2 marker → nâng điểm lên 0.8 (chỉ **nâng**, monotonic, không hạ điểm case đang
  đúng). Marker chỉ kích hoạt khi tài liệu thật là phiếu XN → không tăng false positive từ khớp ngẫu nhiên.
- **Verify:** unit test `DocumentClassifierServiceTests.ClassifyAsync_RealLabForm_SinglePendingMatch_ClassifiedAsLabResult`
  → `LabResult`, confidence ≥ 0.6. PASS. Các test cũ (InBody/Rad/Legacy/3-match) vẫn PASS.

### UTC-EMR-08 / BUG-08 (Med) — mẫu bệnh án hệ thống thiếu structured_json
- **Nguyên nhân:** 2 mẫu hệ thống seed ở `0026_create_emr_templates.sql` có `structured_json = NULL` →
  API `GET /emr-templates` trả `StructuredJson=null` → FE `DynamicFormRenderer` không có field → form rỗng.
- **Fix:** migration `db/migrations/9190_emr_system_templates_structured_json.sql` seed `structured_json`
  (mảng `EmrFormField` khớp type FE) cho 2 mẫu, section lấy theo heading trong `content_json`. Idempotent
  (chỉ UPDATE khi đang NULL/rỗng). KHÔNG đụng `content_json` (giữ chữ ký hash + diff).
- **Verify (MySQL thật):** sau khi apply, mẫu GENERAL có **6 field**, mẫu DIABETES có **9 field**,
  label tiếng Việt đúng dấu (`JSON_EXTRACT ... $[0].label = "Lý do khám"`).

### UTC-RX-07 / BUG-10 (Med) — total_amount đơn thuốc = 0
- **Nguyên nhân:** các query đọc đơn thuốc hardcode `0 as TotalAmount` (`PrescriptionHandlers.cs`).
- **Fix:** thay bằng subquery `SUM(line_total)` của `diab_his_pha_prescription_items` (item khi tạo đã lưu
  `line_total = unit_price * quantity`).
- **Verify (MySQL thật):** đơn 2 thuốc → `total_amount = 66.000` (trước là 0); đơn 1 thuốc → 5.000.

## Ghi chú vận hành (đã đưa vào docs, không phải bug)
- Vé treo `IN_PROGRESS` chiếm phòng vĩnh viễn theo logic sức chứa mới (BUG-02) → thêm mục **R-28**
  vào `docs/ops/release-checklist.md` (dọn vé treo cuối ngày).
