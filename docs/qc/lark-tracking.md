# Lark board — rà soát bug Pro-Diab HIS

Board: DiaB His(Web) — https://sgkgryovqjc.sg.larksuite.com/base/FaAhbhLrJaCsPms7XnWlL9gngeh?table=tblaqjTDAwrsRcOB

---

## ĐỢT 6 — Rà soát toàn board (2026-10-22)

### Phương pháp
Quét toàn bộ grid "Task summary" (70 record active) + các cột Kanban. Lark API bị chặn (RolePermNotAllow / UAT hết hạn) nên rà bằng UI.

### Phân bố trạng thái (Progress)
- **In test**: ~65 ticket (BM-36 → BM-107, trừ 4 ticket To BA). Đây là các bug Đợt 1–5 đã fix + deploy staging, **đang chờ tester verify**.
- **To BA**: 4 ticket — BM-57, BM-69, BM-73, BM-97.
- **Not started / In dev / Return in dev**: **0 ticket**. Không có ticket nào tester trả về dev (không có cột Return in dev được populate), không có ticket dev đang dở.
- **Closed / Cancelled**: ngoài phạm vi grid active.

### Kết luận actionable
Hiện KHÔNG có ticket mới ở Not started/In dev/Return in dev. Không tìm thấy comment mới (ngày ~20–22/10) của tester "Dư Mỹ Vân" trên các ticket đã mở kiểm (comment gần nhất trên các ticket To BA là 10–11/10 từ Nguyễn Ngọc Minh Chi / ghi chú "Claude" test lại). Các ticket In test đang chờ tester.

### Chi tiết 4 ticket To BA
| Ticket | Issue | Bản chất | Hướng xử lý |
|---|---|---|---|
| **BM-57** | [HIS-12] Bác sĩ 2 sửa được bệnh án ca của bác sĩ khác (đang khám, chưa ký số), KHÔNG cảnh báo/chặn | **Bug thật** — tester re-test 11/10 "KHÔNG ĐẠT". Expectation: mở ca của bác sĩ khác → cảnh báo hoặc khoá editor (EmrTabPanel) | **DEV-ACTIONABLE** → fix (cảnh báo + read-only cho bác sĩ không phải chủ ca) |
| **BM-69** | [HIS-24] Khám bệnh — đường huyết dùng hai đơn vị, cùng chỉ số | Quyết định chuẩn đơn vị (mg/dL vs mmol/L) | Cần BA chốt chuẩn đơn vị |
| **BM-73** | [HIS-28] Danh mục — dữ liệu danh mục thuốc & dịch vụ | Nội dung dữ liệu danh mục | Cần BA/nghiệp vụ cung cấp data |
| **BM-97** | [HIS-51] Dược sĩ — chưa có chức năng bán thuốc/thiết bị y tế | **Feature mới** (bán lẻ thuốc/thiết bị) | Cần BA/product quyết định scope |

### Hành động Đợt 6 — KẾT QUẢ

| Ticket | Kết quả | Commit / Ghi chú |
|---|---|---|
| **BM-57** | ✅ Fix + deploy staging + test E2E (Bác sĩ 2 mở ca Bác sĩ 1 → banner cảnh báo + editor khoá) → **In test** | `7b2566f` (FE EncounterDetailClient canEdit theo chủ ca + banner; BE SaveEmrDraft chặn FORBIDDEN) |
| **BM-69** | ✅ Fix + deploy staging + test (5.6 mmol/L ↔ 100 mg/dL) → **In test** | `1e32bf2` (chuẩn hoá hiển thị mmol/L + toggle, hệ số 18.016; KHÔNG đổi schema/quy đổi dữ liệu cũ — glucose_mg_dl vẫn là nguồn sự thật) |
| **BM-73** | ✅ Nạp SUNS catalog vào staging + test UI (/drugs, /services có data + giá) → **In test** | Import `import_suns_catalog.py`; GOTCHA: clinic test là **tenant 2** (DIAB-TEST), lần đầu nạp nhầm tenant 1 → đã re-import tenant 2 (165 thuốc có giá, 49 dịch vụ, 159 XN, 13 CĐHA). Backup: `/home/stackops/backups/stg-his/prodiab_his_before_suns_20261022_190609.sql.gz` |
| **BM-97** | ⏸ Giữ **To BA** | Feature mới (bán thuốc/thiết bị y tế) — cần BA/product chốt scope |

Evidence: `docs/qc/evidence-lark-20261020/BM-57-*.jpg`, `BM-69-*.jpg`, `BM-73-*.jpg`.

### Quan sát phụ (ngoài scope Đợt 6, cần theo dõi)
- Màn nhập sinh hiệu ở tiếp đón (vai Lễ tân) báo "Lịch sử đo: Không tải được lịch sử sinh hiệu" — endpoint history có thể lỗi/thiếu quyền cho Lễ tân. Chưa xử lý, nên kiểm tra riêng.
- Tenant 1 (DiaBetis HCM) đã bị nạp data SUNS thừa từ lần import đầu (devops để nguyên, không xoá vì khó tách). Nếu tenant 1 là môi trường có ý nghĩa, cần dọn.

---

## ĐỢT 7 — Ticket mới ≥ BM-109 (2026-10-22)

Nguồn: grid "Task summary" board DiaB His(Web). 4 ticket mới, đều module **Kho dược** (Pharmacy inventory), Role Quản trị viên, Priority Low, Progress Not started. Người file: Nguyen Duyen.

| Ticket | Màn | Mô tả | Expectation | Hướng |
|---|---|---|---|---|
| **BM-109** | Nhập kho > Tạo đơn đặt hàng (PO) | Tạo đơn thất bại nhưng KHÔNG hiện lý do | Hiện rõ lý do vì sao tạo đơn thất bại (vd: chưa điền đủ ô nào) | FE: hiển thị lỗi validation/BE rõ ràng |
| **BM-110** | Nhập kho (GRN) | Lỗi logic: bắt chọn đơn đặt hàng (PO) mới tạo được phiếu nhập kho | Cho phép tạo phiếu nhập kho (GRN) KHÔNG cần chọn PO — PO là tuỳ chọn | FE (+BE nếu bắt buộc PO ở API) |
| **BM-111** | Điều chỉnh kho | Thiếu trường Tên thuốc, tồn cuối, số lượng sau tăng/giảm | Dropdown thuốc→số lô→auto tồn cuối; 2 cách nhập: Số lượng (+/-) hoặc Số lượng sau — ô còn lại tự tính | FE form enhancement |
| **BM-112** | Tồn kho | Thiếu mã thuốc | Thêm cột Mã thuốc trước cột Tên thuốc | FE: thêm cột bảng |

### Hành động: xử lý từng ticket (ưu tiên bản mặc định, chỉ To BA khi thật cần BA).

### KẾT QUẢ ĐỢT 7

| Ticket | Kết quả | Ghi chú |
|---|---|---|
| **BM-112** | ✅ Fix + deploy (dùng bảng active diab_his_pha_stock/diab_his_pha_drugs) → In test | BE thêm drug_code (d.code); FE thêm cột. Lỗi header trùng "Mã thuốc" đã fix (commit a162d13). |
| **BM-109** | ✅ Fix (FE) → In test | Hook mutation dùng getErrorMessage (hết nuốt lỗi) + PurchaseOrderForm hiện message từng field + lỗi tổng. PO create ghi bảng active. |
| **BM-110** | ⏳ FE + endpoint xong nhưng CHẶN bởi bug data-model có sẵn → In dev | POST /pharmacy/grn trả 400: GrnItemRequest.DrugId=int ghi pha_stocks(legacy) trong khi picker/tồn kho dùng diab_his_pha_drugs/diab_his_pha_stock(GUID). Cần căn chỉnh write-path sang bảng active (task riêng). |
| **BM-111** | ⏳ Form UX xong (chọn thuốc/số lô/tồn cuối/2 cách nhập) nhưng SAVE chặn bởi cùng bug data-model → In dev | CreateAdjustmentHandler ghi pha_stocks(int) không khớp drug_id GUID + không hiện ở tồn kho. Cần căn chỉnh (task riêng). |

**Bug gốc (mới phát hiện):** Warehouse module tách 2 bộ bảng — WRITE (GRN/điều chỉnh/phát) → `pha_stocks`/`pha_drug_master` (drug_id INT), READ (tồn kho/movements) → `diab_his_pha_stock`/`diab_his_pha_drugs` (drug_id GUID). Gây 400 + dữ liệu nhập/điều chỉnh không hiện. Đã tạo task căn chỉnh data-model (chặn BM-110/111 hoạt động đầy đủ).

### KẾT QUẢ CUỐI ĐỢT 7 (sau refactor data-model + test E2E)
- Đã sửa gốc bug data-model kho dược: căn mọi write-path (GRN/điều chỉnh/chuyển kho) + NearExpiryJob + DdiChecker sang bảng active diab_his_pha_stock/diab_his_pha_drugs (drug_id GUID). Dispense vốn đã đúng. 4 integration test MySQL pass.
- **BM-110** ✅ E2E: tạo phiếu nhập KHÔNG PO (Glucophage XR 500mg, lô LOT-GRN-NOPO-1, SL 20) → Tồn kho hiện đúng lô (20, mã THUOC026). (Lỗi 400 trước do manufacture_date rỗng → fix FE "" → undefined.)
- **BM-111** ✅ E2E: điều chỉnh lô LOT-GRN-NOPO-1 (tồn cuối auto 20, nhập -5 → số lượng sau auto 15) → Tồn kho đổi 20→15. Chọn thuốc/số lô/2 chiều nhập OK.
- **BM-112** ✅ cột Mã thuốc đúng 1 cột, data căn đúng.
- **BM-109** ✅ hiện message lỗi tạo đơn/nhập (getErrorMessage) — verify qua lỗi "Cần chọn kho" hiện rõ.
- Regression sau deploy: PASS 12/0.
- → cả 4 chuyển In test.

---

## ĐỢT 8 (2026-10-25)

Ưu tiên user chốt: BM-115, BM-116.

| Ticket | Mô tả | Trạng thái xử lý | Kết quả |
|--------|-------|------------------|---------|
| BM-115 | Role Bác sĩ - Màn khám bệnh: card Sinh hiệu ở profile không hiển thị, phải bấm "Xem tất cả" mới hiện | FIXED backend | commit b96766e; card inline chỉ lấy vital theo encounter hiện tại -> thêm fallback lấy vital mới nhất của bệnh nhân (đồng phạm vi với "Xem tất cả") khi encounter chưa có. Deploy staging (health 200). Verify: API probe 3 case fallback OK + UI (enc e2000000...0002: 37.5C/88/115-90/98%/60kg hiện trực tiếp). Lark In test + comment + evidence. |
| BM-116 | "Role Bác sĩ \|" - ticket bỏ dở, KHÔNG có mô tả bug (Issue cụt, không expectation/ảnh/bước tái hiện; History chỉ có "Added record" + Issue) | BLOCKED - thiếu thông tin | Không thể fix khi không biết bug là gì. Đã comment yêu cầu reporter bổ sung mô tả; giữ Not started. Cần user/reporter điền chi tiết. |

Root-cause BM-115: `GetEncounterDetailQueryHandler` lấy `latestVital` lọc `v.EncounterId == encId` (chỉ lượt khám hiện tại); trong khi "Xem tất cả" (`GetVitalSignsHistoryQuery`) lọc `v.PatientId` (mọi lượt). Sinh hiệu đo ở tiếp đón thuộc encounter check-in khác -> card rỗng. Fix giữ nguyên ưu tiên vital của lượt hiện tại nếu có, fallback sang bản ghi mới nhất của bệnh nhân.

Rà board: 73->72 records (đã xóa 2 record rỗng tạo nhầm khi thao tác grid canvas: BM-114 đợt trước, BM-117 đợt này). Các ticket BM-50..112 đang In test (chờ QC); BM-97 To BA (chưa thấy BA trả lời khi rà). Không có ticket trạng thái In dev / Return in dev.

---

## OCR InBody (2026-10-26)

Bug (user báo + screenshot staging): upload file InBody thật (PDF) ở tiếp đón → classifier nhận diện đúng "Kết quả InBody" ~90% NHƯNG mọi ô chỉ số (Cân nặng, SMM, Khối lượng mỡ, PBF, BMI...) TRỐNG. File mẫu test (case1) thì trích được.

Điều tra (lấy file thật từ MinIO staging bucket inbody-reports, trích text qua đúng PdfPig của app):
- 3 file InBody thật đều là **PDF ảnh scan KHÔNG có text layer** (PdfPig trả text rỗng) → parser nhận chuỗi rỗng → 0 ô. File mẫu case1 có text layer nên chạy được.
- Classifier vẫn detect đúng vì nó dùng OCR (Tesseract); còn `InBodyPdfTextProvider` CHỈ đọc text layer, KHÔNG OCR → lệch.

Root cause: `InBodyPdfTextProvider` không OCR phiếu ảnh scan.

Fix (commit 39168ef + f891a02):
1. `InBodyPdfTextProvider` tái dùng `IPdfTextExtractor` (PdfPig text layer + FALLBACK render từng trang → OCR Tesseract vie+eng) — đã có sẵn, Lab/Rad dùng chung. Phiếu scan giờ được OCR.
2. `InBodyReportParser` bền hơn với layout InBody270 OCR thật:
   - chuẩn hoá "76. 6"/"28, 4" → 76.6/28.4; sửa O/()→0 trong ngữ cảnh số;
   - lấy value NGAY SAU nhãn, TRƯỚC cột "(range)", cùng dòng;
   - bỏ mục "Body Composition History" (tránh lấy số lần đo cũ);
   - anchor: Visceral Fat Level "Level N", InBody Score "N/100";
   - **ƯU TIÊN PRECISION**: số dính sau "(" hoặc kết thúc bẩn ("3|.|") bị loại; giá trị ngoài khoảng vật lý khả dĩ bị loại → ô để TRỐNG + cảnh báo thay vì điền sai (yêu cầu an toàn y tế).
3. Unit test fixture = text OCR thật của phiếu scan (ảnh 7.webp user gửi), đã ẩn danh (bỏ tên/SĐT).

Kết quả extract trên phiếu thật (ground truth Weight 76.6/TBW 33.3/BodyFat 31.1/SMM 25.0/BMI 28.4/PBF 40.6/Visceral 15/BMR 1353/Score 63):
- Điền ĐÚNG & chắc: Weight 76.6, TBW 33.3, Visceral 15, InBody Score 63, BMR 1353 (khi OCR rõ), Body Fat Mass 31 (khi OCR rõ).
- Để TRỐNG + cảnh báo (OCR vùng bar/callout hỏng nặng, không điền sai): SMM, PBF, BMI (và các ô khác tuỳ chất lượng OCR từng lần).
- Trước fix: TẤT CẢ ô trống. Sau fix: 5-6/9 ô tự điền đúng, phần còn lại điều dưỡng nhập tay — không có giá trị sai.

Giới hạn đã biết: OCR ảnh scan không ổn định 100% giữa các lần render; chủ trương điền ô chắc chắn, bỏ trống ô không chắc (người dùng review + xác nhận trước khi lưu). Có thể cân nhắc tăng DPI render / preprocessing ảnh để tăng recall sau này.

### OCR InBody — kết quả E2E thực tế trên staging (2026-10-26, backend f54f188)

Upload ảnh InBody270 scan thật (PDF ảnh-only, không text layer) ở màn Tiếp đón → "Tài liệu lên (tự nhận diện)" → nhận diện "Kết quả InBody" 90% → bảng chỉ số:

| Chỉ số | Ground truth | Giá trị điền (E2E) | Kết quả |
|--------|-------------|--------------------|---------|
| Cân nặng (Weight) | 76.6 kg | 76.6 | ✅ đúng |
| SMM (Khối cơ xương) | 25.0 kg | (trống) | ⚪ để trống (OCR callout/history hỏng — không điền sai) |
| Khối lượng mỡ (Body Fat Mass) | 31.1 kg | (trống) | ⚪ để trống (OCR lần render này hỏng chữ số) |
| PBF (Tỷ lệ mỡ) | 40.6 % | (trống) | ⚪ để trống (OCR callout hỏng) |
| BMI | 28.4 | 28.4 | ✅ đúng (tính W/H² + cross-check, chọn ứng viên khớp) |
| Mỡ nội tạng (Visceral) | 15 | 15 | ✅ đúng |
| TBW (Tổng nước) | 33.3 L | 33.3 | ✅ đúng |
| BMR (Chuyển hoá cơ bản) | 1353 kcal | 1353 | ✅ đúng |
| Điểm InBody | 63 | 63 | ✅ đúng (gộp chữ số OCR tách "6 3/100"→63) |

**Tổng: 6/9 chỉ số tự điền ĐÚNG, 3 để trống (nhập tay), 0 giá trị sai.** Trước fix: TẤT CẢ 9 ô trống (phiếu scan không có text layer → parser nhận chuỗi rỗng).

Evidence (không chứa PII — dialog chỉ hiện các ô chỉ số): `docs/qc/evidence-inbody-ocr-20261026/`.

Giới hạn còn lại: SMM/PBF/Khối mỡ phụ thuộc chất lượng OCR callout/history từng lần render (không ổn định) → chủ trương để trống + cảnh báo khi không chắc, điều dưỡng review/nhập tay trước khi lưu. Hướng nâng recall tương lai (ngoài phạm vi đợt này): tăng DPI render + tiền xử lý ảnh (binarize/denoise), OCR riêng vùng bảng Muscle-Fat/Obesity, hoặc tích hợp thẳng API máy InBody (chính xác tuyệt đối).

Commit: 39168ef (OCR fallback + parser), f891a02 (siết chống giá trị sai), 5ef9864 (loại vạch thang đo), 5ae49ec (BMI cross-check + history fallback), f54f188 (InBody Score gộp chữ số). Deploy staging image `prodiab-his-backend:stg-f54f188` (ID 6725c5b002b4). Rollback: retag prev-20261026-1605 (=5ae49ec) → latest. Regression 12/0.

---

## ĐỢT 9 (2026-10-27) — Ticket mới sau BM-116 + 3 bug phát hiện khi chụp tài liệu

Rà board DiaB His(Web): xoá 1 record rỗng tạo nhầm khi thao tác grid (về lại 76 records). Ticket mới Not started sau BM-116: BM-116, BM-120, BM-122, BM-124, BM-125, BM-126. Không thấy comment feedback mới của tester Dư Mỹ Vân trên ticket In test (các ticket In test vẫn chờ QC verify).

### Kết quả xử lý

| Ticket/Bug | Nội dung | Nguyên nhân | Commit | Trạng thái |
|---|---|---|---|---|
| **BM-116** | Tiếp đón không nhận diện kết quả file InBody | Reporter đã bổ sung mô tả (trước đây cụt) → chính là bug OCR InBody đã fix+deploy 2026-10-26 (OCR fallback cho PDF scan). Code đã có mặt. | (đã deploy f54f188, 2026-10-26) | **In test** |
| **BM-120** | Upload InBody chỉ nhận PDF text-layer; cần jpeg/png + PDF scan | BE `UploadInBodyReportCommandHandler.AllowedMimes` chỉ `application/pdf`; `InBodyPdfTextProvider` chỉ đọc PDF (không OCR ảnh trực tiếp). FE đã accept `image/*`. | `281636a` | **In test** |
| **BM-125** | Sinh hiệu đo ở Tiếp đón không hiện ở màn Điều dưỡng (hiện ở Bác sĩ) | `ListVitalSignsByEncounterQueryHandler` lọc chỉ theo `EncounterId`; vital đo ở Tiếp đón thuộc encounter check-in khác. Thêm fallback theo `PatientId` (đồng pattern BM-115). | `281636a` | **In test** |
| **BM-122** | "Không khám" cần hiện chỉ định dịch vụ phân 7 nhóm | (1) Bug chặn luồng: `SERVICE_ONLY_EMPTY` — đã fix (xem bug-a). (2) Hiện 7 nhóm chi tiết (Gói DiaB/đối tác, CĐHA Siêu âm/XQ/MRI, Thăm dò CN, XN, Thủ thuật, InBody) cần `category` mịn + `PackageType` DiaB/đối tác ở schema — **cần BA chốt taxonomy danh mục**. | (bug chặn: 281636a) | **To BA** (phần phân nhóm) |
| **BM-124** | Gắn event tracking toàn site HIS | Feature xuyên suốt; cần quyết định nền tảng analytics (GA4/PostHog/self-host) + xử lý PII app y tế → **cần product/BA quyết định**. | — | **To BA** |
| **BM-126** | Thiếu phiếu thu tạm + in theo Figma | Feature mới (khái niệm "thu tạm" + template in theo Figma). Cần design/BA cung cấp layout + quy tắc nghiệp vụ. | — | **To BA** |
| **Bug (a)** [không có ticket riêng] | Tiếp đón "Không khám"→"Tạo chỉ định" luôn báo SERVICE_ONLY_EMPTY | FE `ServiceOnlyRequest` gửi camelCase (`serviceIds`) trong khi BE JSON policy snake_case (`service_ids`) → BE đọc null → EMPTY. Đổi FE sang snake_case. | `281636a` | Fixed + deploy; verify: camelCase→EMPTY (tái hiện), snake_case→201 tạo hoá đơn |
| **Bug (b)** [không có ticket riêng] | /cashier thẻ "Tổng thu hôm nay" = 0 | `GetTodayReportHandler` đọc cột `total_*` trên row ca (chỉ ghi lúc đóng ca); ca đang OPEN → 0. Thêm tính LIVE từ bảng payments khi ca OPEN (nhân bản logic CalculateShiftSummaryAsync, read-only). | `281636a` | Fixed + deploy (code-verified: unit test + build; staging chưa có ca OPEN có giao dịch để assert live) |
| **Bug (c)** [không có ticket riêng] | Ô "Lượt khám liên quan" (dialog tải tài liệu) & ô "Thuốc" (GRN) hiện UUID thay vì tên | Base UI `Select.Value` render value thô khi thiếu prop `items` (SmartUploadDialog 2 chỗ). GrnForm lưu drug label ở state theo `field.id` (bay khi thêm/xoá lô) → fallback về GUID. | `281636a` | Fixed + deploy: thêm `items` cho Select encounter; lưu `drug_label` vào form data |

### Deploy & verify
- Build local từ `git archive HEAD` (281636a) → `docker save | ssh docker load` → `compose up -d --no-deps --force-recreate backend frontend` → `docker restart stg-his-nginx`. Chỉ staging (his.diab.vn, stack /srv/apps/stg/his, tenant 2).
- Image staging: `prodiab-his-backend:latest` + `prodiab-his-frontend:latest` (từ 281636a). Rollback: `prev-20261027-1713` (backend+frontend, = image trước đợt này, 6725c5b002b4).
- **Không có migration** (toàn bộ fix là logic/query, không đổi schema) → không backup DB.
- Build backend 0 lỗi; FE typecheck pass; unit test InBody/Vital/Cashier/ServiceOnly 76/76 pass.
- **Regression**: `BASE_URL=https://his.diab.vn node docs/qc/regression/regression.js` → **PASS 12 / FAIL 0**. (Lần chạy đầu BM-47/65 FAIL do test chọn mù encounter đầu tiên — nay là ca DONE đã ký số (signed 2026-10-27 08:13) → 400 EMR_ALREADY_SIGNED đúng domain, KHÔNG phải regression từ code đợt này. Đã sửa test chọn encounter IN_PROGRESS của chính bác sĩ → xanh.)
- Evidence (API probe, không PII): `docs/qc/evidence-lark-20261027/EVIDENCE.txt` (BM-125 fallback trả 1 sinh hiệu; BM-120 upload ảnh PNG không còn INBODY_INVALID_FORMAT; bug-a snake_case tạo hoá đơn HD-... còn camelCase tái hiện EMPTY). Đã dọn hoá đơn draft REGR vừa tạo.

### Còn tồn / chờ BA
- BM-122 (phân 7 nhóm chỉ định), BM-124 (event tracking toàn site), BM-126 (phiếu thu tạm + in Figma): chờ BA/product.
- Bug (b) cashier: cần 1 ca thu ngân OPEN có giao dịch trên staging để assert "Tổng thu hôm nay" > 0 (hiện code-verified).
