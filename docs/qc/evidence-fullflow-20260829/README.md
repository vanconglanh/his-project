# UTE — Test full flow nghiệp vụ phòng khám qua UI thật (Playwright)

| | |
|---|---|
| **Ngày test** | 2026-08-29 |
| **Môi trường** | Local Docker — FE http://localhost:3000, BE http://localhost:5000, MySQL 8.0.36 `prodiab_his` |
| **Branch** | `develop` |
| **Cách test** | Playwright Chromium 1440×950, thao tác y hệt người dùng thật (click / gõ phím / chọn dropdown). **KHÔNG gọi API tắt.** |
| **Xác minh** | 3 lớp: UI (ảnh) + API (status/body request thật do trình duyệt phát ra) + DB (`SELECT` trực tiếp MySQL) |
| **Bệnh nhân test** | `Trần Quốc Hưng 005752` — id `d88b8a19-e43f-4361-99fd-ae11189efb10`, mã BN `BNT01000023` |
| **Lượt khám test** | `1c49b2c1-3f08-40a8-abbe-f3034553b0bd` |

Ảnh evidence đều được **khoanh vùng trực tiếp trên ảnh**: 🟦 xanh dương = INPUT (ô nhập), 🟨 vàng/cam = ACTION (nút bấm), 🟩 xanh lá = RESULT (vùng kết quả).

Script test tái sử dụng được: `frontend/e2e/qc-lib.js`, `qc-step1.js`, `qc-flow.js`, `qc-step3*.js`, `qc-step4/5/6/7/8.js`, `qc-roles.js`.

---

## 1. Kết quả tổng hợp theo bước

| # | Bước nghiệp vụ | Vai trò được giao | Kết quả | Chặn ở đâu |
|---|---|---|---|---|
| 1 | Tiếp đón — tạo bệnh nhân mới | Lễ tân | ❌ **FAIL** | 403 khi POST `/patients` (BUG-01); admin cũng fail nếu bỏ trống ô ngày cấp CCCD (BUG-02) |
| 2a | Ghi sinh hiệu | Bác sĩ / Điều dưỡng | ❌ **FAIL** | Nút "Ghi sinh hiệu" mở drawer chỉ-đọc, không có form (BUG-03) |
| 2b | Chẩn đoán ICD-10 | Bác sĩ | ✅ **PASS** | — (chạy bằng admin) |
| 2c | Chỉ định CLS | Bác sĩ | ✅ **PASS** | — (chạy bằng admin) |
| 3 | Nhập kết quả XN | Kỹ thuật viên | ❌ **FAIL** | Form đã có bộ chọn chỉ định (agent song song đã fix) nhưng submit bị chặn `CLS_ORDER_UNPAID`, mà **không có đường nào thanh toán được** (BUG-05) |
| 4 | Kê đơn thuốc | Bác sĩ | ❌ **FAIL** | Dropdown gợi ý thuốc bị cắt + thiếu tên thuốc, không có nút lưu đơn (BUG-06) |
| 5 | Cấp phát thuốc | Dược sĩ | ❌ **FAIL** | Dropdown kho gọi sai URL → 404 → không chọn được kho (BUG-07) |
| 6 | Thu ngân / hoá đơn | Kế toán | ❌ **FAIL** | Trang chi tiết hoá đơn luôn báo "Không tìm thấy hoá đơn" (BUG-08); cột Bệnh nhân trống (BUG-09) |

**Tổng: 2 PASS / 6 FAIL.** Không bước nào bị bỏ qua — mọi bước fail đều có ảnh bằng chứng.

**Gate decision: FAIL.** Không thể chạy trọn một ca khám từ tiếp đón đến thu tiền bằng UI, kể cả với quyền admin.

---

## 2. Phát hiện nghiêm trọng nhất: mọi vai trò đều bị 403 trên chính màn hình của mình — nhưng UI im lặng

Đây là lý do QC vòng trước "PASS trên giấy": test bằng curl với token admin thì mọi thứ chạy; đăng nhập bằng vai trò thật thì hỏng, **mà màn hình không báo lỗi gì cả** — nó hiển thị empty state như thể chỉ là chưa có dữ liệu.

| Vai trò | Màn hình | Số API trả 403 | UI hiển thị |
|---|---|---|---|
| Lễ tân | `/reception` | 11 | "Không có phòng khám", hàng đợi trống |
| Bác sĩ | `/encounters/{id}` | 11 | Tab Cận lâm sàng trống |
| Kỹ thuật viên | `/labrad/results` | 5 | "Chưa có kết quả xét nghiệm" |
| Dược sĩ | `/pharmacy/dispense` | 8 | "Không có đơn thuốc chờ phát" |
| Kế toán | `/cashier` | 10 | Công nợ 0 ₫, danh sách rỗng |

Ảnh: `G_letan_reception.png`, `G_bacsi_encounter.png`, `G_ktv_labresults.png`, `G_duocsi_dispense.png`, `G_ketoan_cashier.png`.

Ví dụ rõ nhất — `G_ktv_labresults.png`: KTV thấy "Chưa có kết quả xét nghiệm / Nhập kết quả để bắt đầu", trong khi `GET /api/v1/lab-results` thực tế trả **403 PERMISSION_DENIED** 5 lần. Trong danh sách thật có 7+ kết quả.

**Nguyên nhân gốc (đã đối chiếu code ↔ DB):** seed phân quyền cấp cho role các mã quyền **khác** mã mà controller yêu cầu.

```
Controller PatientsController.cs:57  →  [RequirePermission("patient.write")]
DB: patient.write   → chỉ role admin
DB: patient.create  → admin, bac_si, le_tan   ← cấp nhầm mã này
```

Rà toàn bộ: **101 mã quyền** mà API đang enforce chỉ được cấp cho `admin` (hoặc không cấp cho ai), gồm gần như toàn bộ nghiệp vụ lâm sàng: `patient.write`, `vital_sign.write`, `lab_result.write/read`, `rad_result.write`, `dispense.perform/queue`, `payment.collect`, `billing.finalize`, `drug.read`, `stock.read`, `warehouse.read`, `service.read`, `room.read`, `reception.checkin`, `reception.rooms.read`, `dashboard.read`, `notification.read`…

Danh sách đầy đủ tái tạo được bằng lệnh trong `roles_matrix.json`.

---

## 3. Danh sách bug

### BUG-01 — Lễ tân không tạo được bệnh nhân (403), và toàn bộ ma trận phân quyền lệch mã
- **Severity**: 🔴 Blocker
- **Bước**: 1 — Tiếp đón
- **Vai trò**: Lễ tân (`letan.test@prodiab.test`)
- **Steps**: Đăng nhập nhanh → Lễ tân → `/patients/new` → điền Họ tên / Giới tính / Ngày sinh / SĐT / Địa chỉ → bấm **Tạo bệnh nhân**
- **Expected**: Tạo được bệnh nhân (role Lễ tân có `patient.create` trong DB)
- **Actual**: `POST /api/v1/patients` → **403 PERMISSION_DENIED**. Toast "Bạn không có quyền thực hiện thao tác này".
- **Evidence**: `s1_02_form_empty.png`, `s1_03_form_filled.png`, **`s1_04_after_submit.png`**, `s1_result.json`
- **Root cause**: `PatientsController.Create` yêu cầu `patient.write`; seed chỉ cấp `patient.write` cho `admin`, còn `le_tan`/`bac_si` được cấp `patient.create` (mã không ai enforce). Lệch mã này lặp lại trên 101 quyền.
- **Gợi ý vùng sửa**: seed `diab_his_sec_role_permissions` (thống nhất bộ mã với `[RequirePermission]` trong `backend/src/ProDiabHis.Api/Controllers/*`). Nên thêm test khởi động đối chiếu tập mã enforce ↔ tập mã được cấp.

### BUG-02 — Bỏ trống ô "Ngày cấp CMND/CCCD" (không bắt buộc) làm hỏng cả form tạo bệnh nhân
- **Severity**: 🔴 Blocker
- **Bước**: 1 — Tiếp đón
- **Steps**: `/patients/new` (bằng **admin**) → điền các ô bắt buộc, **để trống** "Ngày cấp CMND/CCCD" → Tạo bệnh nhân
- **Expected**: 201, bỏ trống ô không bắt buộc là hợp lệ
- **Actual**: **400 VALIDATION_ERROR** — `{"$.id_card_issued_date":["id_card_issued_date không đúng định dạng"],"request":["request là bắt buộc"]}`. FE gửi chuỗi rỗng thay vì `null`, lỗi ở tầng deserialize JSON nên **cả request bị vứt bỏ**.
- **Đã chứng minh**: điền ngày vào ô đó → **201 Created** ngay (`A1_03_after_submit.png`, toast "Tạo bệnh nhân thành công").
- **Evidence**: `A1_01_form_empty.png`, `A1_02_form_filled.png`, `A1_03_after_submit.png`, `flow_result_all.json`
- **Gợi ý vùng sửa**: FE form bệnh nhân — chuyển `""` → `null`/omit cho mọi field date optional trước khi submit.

### BUG-03 — Nút "Ghi sinh hiệu" của bác sĩ mở drawer chỉ-đọc, không có form nhập
- **Severity**: 🔴 Blocker (đúng loại bug mà đề bài cảnh báo)
- **Bước**: 2 — Khám bệnh
- **Steps**: `/encounters/{id}` → panel Sinh hiệu → bấm **Ghi sinh hiệu**
- **Expected**: Mở form nhập sinh hiệu
- **Actual**: Mở drawer **"Nhật ký sinh hiệu"** hiển thị "Chưa có sinh hiệu", chỉ có nút Close. Không ô nhập, không nút lưu. Bác sĩ không có cách nào ghi sinh hiệu từ màn khám.
- **Evidence**: **`A4_01_vitals_dialog_empty.png`**
- **Root cause**: `components/domain/EncounterPatientSidebar.tsx:60` — `onAddNew={canEdit ? onOpenVitalDrawer : undefined}`: action "thêm mới" bị nối vào đúng handler mở drawer lịch sử chỉ-đọc. Component `components/domain/VitalSignsForm.tsx` **có tồn tại** nhưng chỉ được dùng ở `app/(dashboard)/nurse/`.
- **Gợi ý vùng sửa**: `EncounterPatientSidebar.tsx` + `EncounterDetailClient.tsx` — nối `onAddNew` vào `VitalSignsForm`.

### BUG-04 — Form sinh hiệu: để trống bất kỳ ô số nào là không lưu được, lỗi hiện bằng tiếng Anh thô
- **Severity**: 🟠 High
- **Bước**: 2 — Khám bệnh (màn Điều dưỡng)
- **Steps**: `/nurse` → **Nhập sinh hiệu** → điền một phần (vd chỉ nhiệt độ, mạch, HA tâm thu) → **Lưu sinh hiệu**
- **Expected**: Lưu được; các chỉ số không đo thì bỏ trống
- **Actual**: Không có toast, không có request nào gửi đi, form đứng im. Dưới ô trống hiện **"Invalid input: expected number, received NaN"** — chuỗi lỗi Zod nguyên bản tiếng Anh, giữa giao diện tiếng Việt cho điều dưỡng.
- **Đã chứng minh**: điền **đủ cả 10** ô số → `POST /encounters/{id}/vital-signs` **201**, toast "Đã lưu sinh hiệu" (`A4_30/31`).
- **Evidence**: `A4_20_vitals_form_empty.png`, `A4_21_vitals_form_filled.png`, **`A4_22_vitals_after_save.png`**, `A4_30_vitals_all_filled.png`, `A4_31_vitals_all_saved.png`
- **Gợi ý vùng sửa**: `components/domain/VitalSignsForm.tsx` — dùng `z.coerce.number().optional()` với preprocess `"" → undefined`; đồng thời i18n thông điệp lỗi Zod.

### BUG-05 — "Chốt đợt → chuyển thu ngân" không tạo hoá đơn; luồng CLS bị deadlock
- **Severity**: 🔴 Blocker
- **Bước**: 3 — Nhập kết quả XN
- **Steps**: `/encounters/{id}` → tab Cận lâm sàng → **Chốt đợt** → sang `/cashier` và `/billings` tìm hoá đơn
- **Expected**: Sinh hoá đơn cho đợt CLS 35.000 ₫ để thu ngân thu tiền, sau đó KTV nhập được kết quả
- **Actual**: Toast báo **"Đã chốt đợt chỉ định, chuyển thu ngân"** nhưng:
  - Thẻ đợt CLS vẫn "Chưa thanh toán", nút "Chốt đợt" biến mất, không còn thao tác nào
  - `/cashier` "Hoá đơn chờ thu" và `/billings` **không có hoá đơn 35.000 ₫ nào**
  - Khi nhập kết quả XN: `POST /api/v1/lab-results` → **400 `CLS_ORDER_UNPAID` "Đợt chỉ định chưa thanh toán"**
  - **Kiểm DB**: `diab_his_cls_order_rounds` id `fb23d2c4…` → `status=SUBMITTED`, `total_amount=35000.00`, **`billing_id = NULL`**
- **Kết luận**: đợt CLS vào ngõ cụt — không thanh toán được ⇒ không bao giờ nhập được kết quả XN.
- **Evidence**: `C1_01_cls_round.png`, `C1_03_after_chot.png`, `C2_01_cashier_list.png`, `C2_02_billings_list.png`, **`B2_admin_05_after_submit.png`**, `step5_admin.json`, `step6_result.json`
- **Root cause**: `backend/src/ProDiabHis.Application/CLS/ClsRoundHandlers.cs:269` — `SubmitClsRoundCommandHandler` chỉ `UPDATE … SET status='SUBMITTED'`, không tạo bản ghi billing, không gán `billing_id`. Có sẵn `PayClsRoundCommandHandler` và `payClsRound()` trong `frontend/lib/api/cls-rounds.ts:115`, nhưng **không hook/UI nào gọi tới** (`waiveClsRound` cũng vậy) → không có nút thanh toán/miễn phí trên giao diện.
- **Gợi ý vùng sửa**: `SubmitClsRoundCommandHandler` tạo billing + set `billing_id`; và/hoặc expose `payClsRound`/`waiveClsRound` trong `use-cls-rounds.ts` + `ClsOrderTabPanel.tsx`.

### BUG-05b — Bộ chọn chỉ định XN vẫn mời chọn đợt chưa thanh toán rồi mới báo lỗi
- **Severity**: 🟡 Medium (UX)
- **Bước**: 3
- **Actual**: Ở `/labrad/results` → "+ Nhập kết quả", bộ chọn liệt kê đợt CLS **chưa thanh toán** không kèm cảnh báo. Người dùng chọn bệnh nhân, gõ đủ giá trị / đơn vị / phương pháp / thời gian rồi bấm gửi mới nhận lỗi, dialog không đóng, toàn bộ công nhập giữ nguyên nhưng vô ích.
- **Evidence**: `B2_admin_02_form_empty.png`, `B2_admin_03_order_picked.png`, `B2_admin_04_form_filled.png`, `B2_admin_05_after_submit.png`
- **Ghi nhận tích cực**: phần fix của agent song song **đã vào và hoạt động** — form giờ có ô `#lr-order-search`, tìm được đúng "Trần Quốc Hưng 005752 · BNT01000023 — Đường huyết đói (GLU_F)". Bug "không có cách chọn chỉ định" **đã hết**.
- **Gợi ý**: lọc bỏ hoặc disable + gắn nhãn "Chưa thanh toán" ngay trong danh sách gợi ý.

### BUG-06 — Tab Đơn thuốc: gợi ý thuốc bị cắt cụt, thiếu tên thuốc, không có nút lưu đơn
- **Severity**: 🔴 Blocker
- **Bước**: 4 — Kê đơn
- **Steps**: `/encounters/{id}` → tab Đơn thuốc → gõ `metformin`
- **Expected**: Danh sách thuốc có tên, chọn được, nhập liều, có nút lưu đơn
- **Actual**:
  - Dropdown gợi ý bị container `overflow` cắt còn ~5px, thực tế không click được (`D1_02_rx_search.png`)
  - Nội dung gợi ý trích ra được là `"500mg / OTC / · 500đ/Viên / TH001"` — **không có tên thuốc**, chỉ có hàm lượng và mã
  - Trong cả tab không tồn tại nút **Lưu đơn / Tạo đơn / Kê đơn** nào
- **Evidence**: `D1_01_rx_tab_empty.png`, **`D1_02_rx_search.png`**, `D1_03_rx_drug_added.png`, `D1_04_rx_no_save_btn.png`, `step7_result.json`
- **Ghi chú thêm**: ở `/prescriptions`, cột **Bệnh nhân và Bác sĩ trống trơn** trên mọi dòng, "Số thuốc" = 0 và "Tổng tiền" = 0đ kể cả với đơn trạng thái "Đã phát" (`G_bacsi_rx.png`).

### BUG-07 — Cấp phát thuốc: dropdown kho gọi sai URL → 404 → không chọn được kho
- **Severity**: 🔴 Blocker
- **Bước**: 5 — Cấp phát thuốc
- **Steps**: `/pharmacy/dispense` → **Phát thuốc** trên một đơn trong hàng chờ
- **Expected**: "Kho phát thuốc" load danh sách kho, chọn kho rồi xác nhận phát
- **Actual**: Dropdown đứng ở "-- Chọn kho --" và rỗng. `GET /api/v1/warehouses` → **404** (gọi 2 lần). Không chọn được kho ⇒ không xác nhận phát thuốc được.
- **Evidence**: `E1_01_dispense_queue.png`, **`E1_02_dispense_detail.png`**, `step7_result.json`
- **Root cause**: sai đường dẫn API. FE `frontend/lib/api/pharmacy-warehouse.ts:130` gọi `/warehouses`; BE khai báo `[HttpGet("api/v1/pharmacy/warehouses")]` tại `backend/src/ProDiabHis.Api/Controllers/PharmacyWarehouseController.cs:17`. Cả 4 hàm trong file FE (list/create/update/delete) đều thiếu tiền tố `pharmacy/`.

### BUG-08 — Trang chi tiết hoá đơn luôn báo "Không tìm thấy hoá đơn"
- **Severity**: 🔴 Blocker
- **Bước**: 6 — Thu ngân
- **Steps**: `/billings` → click dòng bất kỳ → vào `/billings/{id}`
- **Expected**: Hiện chi tiết hoá đơn, nút xác nhận / thu tiền
- **Actual**: Chỉ hiện chữ **"Không tìm thấy hoá đơn"**. Không có nút nào. **Trình duyệt không phát ra một request API nào** cho trang này.
- **Kiểm DB**: hoá đơn `2ffe93bd-ae07-42c2-ae29-21e1adea03c9` **có thật** — `bill_no=HD-202608-F7919`, `patient_payable=53025.00`, `status=DRAFT`, `deleted_at=NULL`, `tenant_id=1`.
- **Evidence**: `F1_01_invoice_detail.png`, **`F1_03_invoice_detail_404.png`**, `step8_billing_detail.json`
- **Root cause**: `app/(dashboard)/billings/[id]/page.tsx:6` nhận `params` kiểu đồng bộ `{ params }: { params: { id: string } }` rồi dùng `params.id`. Ở Next.js phiên bản dự án đang dùng, `params` là Promise ⇒ `params.id === undefined` ⇒ `useBilling(undefined)` có `enabled: Boolean(id)` = false ⇒ **không fetch** ⇒ rơi vào nhánh `if (!billing)`.
- **Phạm vi**: đã rà toàn bộ 16 route động — **chỉ mình `billings/[id]/page.tsx` còn dùng params đồng bộ**, 15 trang còn lại đã `await params` đúng.

### BUG-09 — Cột "Bệnh nhân" trống trên mọi danh sách hoá đơn / thu ngân / đơn thuốc
- **Severity**: 🟠 High
- **Bước**: 6 — Thu ngân
- **Actual**: `/billings` (6 dòng), `/cashier` tab "Hoá đơn chờ thu", `/prescriptions` — cột Bệnh nhân (và Bác sĩ ở màn đơn thuốc) **trống hoàn toàn** trên tất cả các dòng. Thu ngân chỉ nhìn thấy số hoá đơn và số tiền, không biết thu của ai.
- **Evidence**: **`C2_02_billings_list.png`**, `C2_01_cashier_list.png`, `G_bacsi_rx.png`, `G_ketoan_cashier.png`

### BUG-10 — Lỗi API hiện bằng tiếng Anh thô thay vì thông điệp tiếng Việt mà backend đã trả
- **Severity**: 🟡 Medium
- **Actual**: Ở form tạo bệnh nhân, banner đỏ hiển thị **"Request failed with status code 403"** (chuỗi mặc định của axios) trong khi API đã trả sẵn `{"error":{"message":"Bạn không có quyền thực hiện thao tác này"}}`.
- **Evidence**: `s1_04_after_submit.png`
- **Gợi ý**: lớp hiển thị lỗi form nên đọc `error.response.data.error.message` trước khi fallback về `error.message`.

### BUG-11 — Bệnh nhân đang khám biến mất khỏi hàng chờ điều dưỡng
- **Severity**: 🟠 High
- **Actual**: Hàng chờ `/nurse` chỉ liệt kê bệnh nhân trạng thái "Chờ khám". Sau khi bác sĩ bấm "Bắt đầu khám", bệnh nhân chuyển "Đang khám" và **rời khỏi hàng chờ điều dưỡng**. Kết hợp với BUG-03 (màn bác sĩ không có form) ⇒ **không có màn hình nào ghi được sinh hiệu cho ca đang khám**.
- **Evidence**: `A4_10_nurse_queue.png`, `step3d_result.json` (`containsOurPatient: false`)

---

## 4. Nhận xét UX / a11y

| # | Vấn đề | Ảnh hưởng | Đề xuất |
|---|---|---|---|
| 1 | 403 bị nuốt, hiển thị thành empty state | Người dùng và cả QC tưởng hệ thống chạy đúng nhưng "chưa có dữ liệu". Nguy hiểm nhất trong toàn bộ báo cáo. | Phân biệt rõ *rỗng* và *không có quyền*: 403 phải render "Bạn không có quyền xem mục này", kèm log |
| 2 | Toast "Đã chốt đợt chỉ định, chuyển thu ngân" trong khi không có gì được chuyển | Bác sĩ tin đã xong, thu ngân không thấy gì, bệnh nhân chờ vô ích | Chỉ báo thành công khi hoá đơn thực sự được tạo |
| 3 | Thông điệp lỗi tiếng Anh thô ("Invalid input: expected number, received NaN", "Request failed with status code 403") | Điều dưỡng / lễ tân không hiểu, không biết sửa ô nào | i18n toàn bộ message Zod + axios |
| 4 | Gợi ý bệnh nhân (`/encounters/new`) và gợi ý thuốc không dùng `role="option"`/`listbox` | Trình đọc màn hình và điều hướng bàn phím không dùng được | Dùng combobox có ARIA đầy đủ |
| 5 | Các ô số trong form sinh hiệu không có `label for` | Screen reader đọc ô trống không rõ nghĩa | Gắn `htmlFor`/`aria-label` |
| 6 | Dropdown gợi ý thuốc bị container cắt | Không kê được đơn dù backend có dữ liệu | Bỏ `overflow-hidden` ở card cha hoặc render dropdown qua portal |
| 7 | Đăng nhập mất ~6,9 giây (log backend) | Mỗi ca trực đăng nhập lại đều khựng | Xem lại work factor bcrypt cho môi trường dev/prod |

---

## 5. Ưu tiên sửa

1. **BUG-01** — đồng bộ mã quyền seed ↔ `[RequirePermission]` (mở khoá 5/6 vai trò, ảnh hưởng 101 quyền)
2. **BUG-08** — `await params` ở `billings/[id]/page.tsx` (sửa 1 dòng, mở khoá toàn bộ bước thu ngân)
3. **BUG-07** — thêm tiền tố `pharmacy/` cho 4 lời gọi warehouse (mở khoá bước cấp phát)
4. **BUG-05** — chốt đợt phải tạo hoá đơn, hoặc expose nút thanh toán/miễn phí (mở khoá bước kết quả XN)
5. **BUG-06** — sửa dropdown thuốc + bổ sung nút lưu đơn (mở khoá bước kê đơn)
6. **BUG-03 / BUG-11** — nối form sinh hiệu vào màn khám của bác sĩ
7. **BUG-02, BUG-04** — chuẩn hoá field optional rỗng (`""` → `null`/`undefined`) ở cả 2 form

---

## 6. Ghi chú về độ tin cậy của kết quả

- **Rebuild của agent song song**: có gặp 2 lần đăng nhập không chuyển trang (`AX_error.png`) — đã thử lại và qua ngay, xác định là click trước khi React hydrate xong, đã thêm chờ hydrate + retry 3 lần vào `qc-lib.js`. **Không có bug nào trong báo cáo này bắt nguồn từ hiện tượng đó** — mọi bug đều tái hiện được nhiều lần và đều có xác nhận ở tầng code hoặc DB.
- Fix "+ Nhập kết quả" của agent song song đã có mặt và hoạt động; bug còn lại ở bước 3 là chuyện khác (chưa thanh toán được).
- Các bước 2b/2c chạy bằng quyền admin để tách bạch lỗi UI khỏi lỗi phân quyền; với vai trò Bác sĩ thật thì vẫn bị BUG-01 chặn.
