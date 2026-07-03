# PRD — Component Drug Picker (chọn thuốc bằng tìm kiếm)

> Tác giả: Đăng (PO/BA) · Ngày: 2026-07-03 · Version: 1.0
> Liên quan: CLAUDE.md §4 Pharmacy, `docs/design/design-system-standards.md` §7 (giảm nhập tay — autocomplete là pattern P0), `docs/review/architect-decision-f01-f03.md` §3.1/§3.6 (rủi ro "3 form còn dùng UUID thô")
> Cross-link: `docs/prd/prescription-prd.md` (US-RX-01 AC-3), FHIR R4 `MedicationRequest.medication`
> Yêu cầu PO nhấn mạnh: **flow phải dễ dùng — tối ưu cho nhập NHIỀU dòng liên tục bằng bàn phím**

---

## 1. Bối cảnh & Mục tiêu

### 1.1. Vấn đề
Audit layout (F-03) + architect review phát hiện 3 form kho dược bắt người dùng **gõ tay `drug_id` dạng UUID thô** (`<Input placeholder="drug_id..." />`):
- `PurchaseOrderForm.tsx` — dòng `items[]`: `drug_id`, số lượng, đơn giá.
- `GrnForm.tsx` — dòng `items[]`: `drug_id`, số lô, NSX, HSD, số lượng nhận, đơn giá nhập.
- `AdjustmentForm.tsx` — dòng `items[]`: `drug_id`, số lô, số lượng chênh lệch (+/-).

Không dược sĩ/nhân viên mua hàng nào thuộc UUID → 3 form này **không dùng được trong thực tế**. Đây là nợ chức năng, không phải nợ layout (architect đã tách ra PRD riêng — mục §3.6 của architect-decision).

### 1.2. Mục tiêu
- Thay ô nhập UUID thô bằng **1 component Drug Picker dùng chung**: gõ tên/mã/hoạt chất → gợi ý → chọn → form lưu `drug_id` ngầm, người dùng chỉ thấy tên thuốc.
- **Tối ưu nhập batch bằng bàn phím**: nhập liên tục nhiều dòng (đặt hàng 20-30 mặt hàng, kiểm kê cả kho) mà tay không rời bàn phím.
- Chuẩn hóa 1 component duy nhất cho toàn hệ thống (kho dược + kê đơn), tránh mỗi màn tự viết một kiểu.

### 1.3. Không phải mục tiêu
Không thiết kế lại nghiệp vụ PO/GRN/Adjustment; không đổi API tạo đơn/nhập kho/điều chỉnh; không làm phần layout Fullpage (đã thuộc F-03, do Nam làm riêng).

---

## 2. Hiện trạng khảo sát (findings — quyết định tái dùng)

> Mục này ghi lại kết quả đọc code để chốt: **xây mới hay tái dùng**.

### 2.1. ĐÃ CÓ sẵn component picker tốt — `DrugAutocomplete.tsx`
`frontend/components/domain/DrugAutocomplete.tsx` đã tồn tại và đang được dùng ở màn **chi tiết đơn thuốc** (`prescriptions/[id]/_components/PrescriptionDetailClient.tsx` → thêm thuốc vào đơn). Nó đã làm tốt:
- Gọi `useDrugSearch(query)` → `GET /api/v1/drugs/search?q=&limit=20` (debounce qua React Query `staleTime`).
- Dropdown hiển thị: `name_vi`, `strength`, badge (Hướng thần / Gây nghiện / OTC), `form`, `manufacturer`, `price/unit`, `code` (mono).
- Có `role="listbox"`/`role="option"`, click-outside để đóng, skeleton loading, empty state "Không tìm thấy thuốc".
- Callback `onSelect(drug: DrugMasterResponse)` — trả cả object, không chỉ id.

→ **QUYẾT ĐỊNH: TÁI DÙNG + NÂNG CẤP `DrugAutocomplete`, KHÔNG viết lại từ đầu.** PRD này đặc tả phần còn thiếu để nó (a) dùng được trong form batch kho dược và (b) chuẩn hóa cho cả kê đơn.

### 2.2. Màn kê đơn có tái dùng được không?
- Màn **chi tiết đơn** (`PrescriptionDetailClient`) đã dùng `DrugAutocomplete` → picker của kê đơn **chính là** `DrugAutocomplete`. Vậy không có "picker riêng của kê đơn" tốt hơn để bê sang — cùng một component.
- Màn **tạo đơn** (`prescriptions/new/page.tsx`) hiện KHÔNG chọn thuốc (submit `items: []`, thuốc thêm sau ở màn chi tiết). Ô tìm bệnh nhân / tìm ICD-10 ở đây là autocomplete **viết tay inline trong page** (dropdown thủ công), không phải component tái dùng.
- `Icd10Picker.tsx` là mẫu tham chiếu tốt cho pattern "gợi ý + gần đây + yêu thích" (localStorage) — có thể học pattern "gần đây/yêu thích" cho Drug Picker (P2, không bắt buộc đợt 1).

→ Kết luận: **chuẩn hóa `DrugAutocomplete` thành component dùng chung `DrugPicker`** cho cả 3 form kho dược + kê đơn, thay vì đặc tả từ đầu.

### 2.3. Khoảng trống của `DrugAutocomplete` hiện tại (phải bổ sung)
| # | Thiếu | Vì sao cần cho kho dược |
|---|---|---|
| G1 | **Không có điều hướng bàn phím** (chỉ click chuột chọn; không ArrowUp/Down, không Enter chọn) | Đây là **điểm chặn chính** với yêu cầu PO "nhập nhiều dòng bằng bàn phím". |
| G2 | **Không hiển thị tồn kho / đơn vị tồn** | Người đặt hàng/kiểm kê cần thấy tồn hiện tại để quyết định số lượng. |
| G3 | **Không lọc/không đánh dấu thuốc ngừng kinh doanh** (`status=INACTIVE`) | `/drugs/search` hiện trả cả INACTIVE → dễ chọn nhầm thuốc đã ngừng. |
| G4 | **Không có trạng thái "đã chọn"** để hiển thị lại tên trên dòng form | Form kho lưu `drug_id` nhưng phải cho người dùng thấy tên đã chọn + đổi lại được. |
| G5 | **Ngưỡng kích hoạt = 1 ký tự** | PO yêu cầu **≥2 ký tự** (giảm tải query, đồng bộ với tìm bệnh nhân đã dùng `>=2`). |
| G6 | **Không có biến thể chọn theo LÔ tồn** | `AdjustmentForm` cần chọn `drug_id` **+ `batch_no`** của lô đang tồn — picker thuốc thuần không đủ. |

### 2.4. API sẵn có
- `GET /api/v1/drugs/search?q=&limit=` — **có sẵn**, dùng cho picker thuốc. Trả `DrugMasterResponse` (gồm `status` ACTIVE/INACTIVE, nhưng **không** kèm tồn kho).
- `GET /api/v1/pharmacy/stocks?warehouse_id=&drug_id=&batch_no=` — **có sẵn** (`listStocks`), trả từng lô: `batch_no`, `expiry_date`, `quantity_available`, `days_to_expiry`, `is_near_expiry`. Dùng cho **biến thể chọn lô** (Adjustment) và để hiển thị tồn.
- `GET /api/v1/drugs?status=&atc_code=…` — list có filter `status` (dùng nếu cần lọc INACTIVE ở search).

---

## 3. Personas & RBAC

| Persona | Quyền (permission) | Dùng Drug Picker ở đâu |
|---|---|---|
| DuocSi (Dược sĩ) | `pharmacy.grn.create`, `pharmacy.adjustment.create` | Nhập kho (GRN), kiểm kê/điều chỉnh (Adjustment) |
| DuocSi / KeToan (người đặt hàng) | `pharmacy.po.create` | Tạo đơn đặt hàng (PO) |
| BacSi | `rx.create/edit` | Kê đơn (màn chi tiết đơn — đã dùng, sẽ hưởng lợi nâng cấp) |
| Admin | `pharmacy.*`, `drug.read` | Toàn bộ |

Ràng buộc: Drug Picker chỉ hoạt động khi user có `drug.read` (đã là điều kiện của `/drugs/search`). Không nới quyền mới.

---

## 4. Phạm vi & biến thể component

Drug Picker có **2 biến thể** (cùng lõi tìm kiếm, khác dữ liệu chọn):

| Biến thể | Chọn ra | Dùng ở | Nguồn dữ liệu |
|---|---|---|---|
| **A — Drug Picker** (thuốc từ danh mục) | `drug_id` (+ hiển thị tên, đơn vị, giá) | PO, GRN, Kê đơn | `GET /drugs/search` |
| **B — Stock-Lot Picker** (thuốc + lô đang tồn) | `drug_id` + `batch_no` (+ HSD, tồn khả dụng) | Adjustment (điều chỉnh 1 lô cụ thể) | Biến thể A để chọn thuốc, rồi `GET /pharmacy/stocks?warehouse_id=&drug_id=` để chọn lô |

> GRN nhập **lô mới** → `batch_no` là ô nhập tay tự do (thuốc mới về), chỉ cần biến thể A cho `drug_id`. Adjustment điều chỉnh **lô đã tồn** → cần biến thể B.

---

## 5. Use cases

- **UC-01**: Chọn 1 thuốc bằng tìm kiếm tên/mã/hoạt chất (biến thể A).
- **UC-02**: Nhập liên tục nhiều dòng thuốc bằng bàn phím trong 1 form batch (PO/GRN/Adjustment).
- **UC-03**: Chọn thuốc + lô tồn kho khi điều chỉnh (biến thể B).
- **UC-04**: Đổi lại thuốc đã chọn trên một dòng.
- **UC-05**: Xử lý khi không tìm thấy / thuốc ngừng kinh doanh / lô hết hạn.

---

## 6. User stories & Acceptance Criteria

> AC dạng Given/When/Then, đo lường được (không dùng "nhanh/tốt").

### US-DP-01 — Tìm & chọn thuốc bằng gõ phím (biến thể A)
- **AC-1**: Given ô Drug Picker rỗng, When người dùng gõ **≥ 2 ký tự**, Then hệ thống gọi `GET /drugs/search?q={query}&limit=20` sau **debounce 250ms** kể từ lần gõ cuối, và hiện dropdown gợi ý. Gõ < 2 ký tự → **không** gọi API, không mở dropdown.
- **AC-2**: Given dropdown đang mở có ≥1 kết quả, When người dùng bấm **↓/↑**, Then di chuyển highlight qua từng dòng (vòng lại đầu/cuối khi chạm biên), dòng đang highlight có nền `bg-accent` + `aria-selected="true"`.
- **AC-3**: Given có 1 dòng đang highlight, When bấm **Enter**, Then chọn dòng đó: form ghi `items[idx].drug_id = drug.id`, ô hiển thị tên thuốc đã chọn (không hiển thị UUID), dropdown đóng. Khi mở dropdown lần đầu (chưa di chuyển phím), dòng **đầu tiên** được highlight sẵn để Enter chọn ngay.
- **AC-4**: Given dropdown đang mở, When bấm **Esc**, Then đóng dropdown, giữ nguyên giá trị đang gõ, không chọn gì; focus vẫn ở ô input.
- **AC-5**: Mỗi dòng gợi ý hiển thị tối thiểu: `name_vi`, `strength`, `unit`, và `code` (mono). Có badge khi: Hướng thần, Gây nghiện, OTC (không kê đơn), **Ngừng KD** (status=INACTIVE). Tìm khớp được theo `name_vi`, `generic_name` (hoạt chất), `code` (BE đảm nhiệm — xem §9 phụ thuộc).
- **AC-6**: Chọn bằng **chuột click** vẫn hoạt động song song với bàn phím (không loại bỏ hành vi cũ).

### US-DP-02 — Nhập batch nhiều dòng không rời bàn phím (yêu cầu trọng tâm)
- **AC-1**: Given con trỏ ở ô Drug Picker của dòng `idx` và đã chọn được 1 thuốc, When bấm **Tab** (hoặc Enter nếu là ô cuối luồng theo thiết kế), Then focus chuyển sang ô kế tiếp **trong cùng dòng** theo đúng thứ tự đọc (VD PO: Thuốc → Số lượng → Đơn giá).
- **AC-2**: Given con trỏ ở **ô cuối cùng của dòng cuối cùng** và dòng đó đã hợp lệ (đã chọn thuốc), When bấm **Enter**, Then hệ thống **tự thêm 1 dòng mới** (append) và **tự focus vào ô Drug Picker của dòng mới** — không cần với chuột bấm "Thêm dòng".
- **AC-3**: Given đang ở ô Drug Picker của dòng cuối rỗng, When bấm phím tắt **thêm dòng** (nút "Thêm dòng"/"Thêm lô" vẫn giữ, có `type="button"`), Then thêm dòng và focus vào ô thuốc của dòng mới; thao tác chuột và bàn phím cho kết quả giống nhau.
- **AC-4**: Given một dòng chưa chọn thuốc (drug_id rỗng) mà bấm Enter ở ô cuối, Then **không** append dòng mới; hiện lỗi inline "Chọn thuốc" dưới ô Drug Picker của dòng đó (tiếng Việt có dấu), focus quay về ô thuốc.
- **AC-5**: Sau khi hoàn tất chuỗi nhập, người dùng có thể bấm **Ctrl/Cmd+S** để submit form mà không cần chuột (nhất quán với `prescriptions/new`). *(Phím submit do trang Fullpage F-03 cung cấp; Drug Picker chỉ đảm bảo không "nuốt" tổ hợp phím này.)*
- **AC-6 (đo được)**: Nhập trọn 1 dòng thuốc (chọn thuốc + số lượng) đạt được **chỉ bằng bàn phím**, không quá **1 lần** dùng chuột cho cả form (lý tưởng 0). Kịch bản nghiệm thu: nhập 10 dòng PO liên tục không chạm chuột.

### US-DP-03 — Chọn thuốc + lô tồn kho khi điều chỉnh (biến thể B)
- **AC-1**: Given AdjustmentForm đã chọn **Kho** ở header, When người dùng chọn thuốc trên 1 dòng (biến thể A), Then hệ thống gọi `GET /pharmacy/stocks?warehouse_id={kho}&drug_id={id}` và hiện danh sách **lô đang tồn** để chọn: mỗi lô hiển thị `batch_no`, `HSD (dd/MM/yyyy)`, `tồn khả dụng {quantity_available} {unit}`.
- **AC-2**: Given danh sách lô hiện ra, When chọn 1 lô bằng ↓/↑ + Enter, Then form ghi `items[idx].batch_no = lô.batch_no`; ô hiển thị "`{tên thuốc} · Lô {batch_no} · HSD {…} · tồn {…}`".
- **AC-3**: Given thuốc đã chọn **không có lô nào tồn > 0** trong kho, Then hiện dòng thông báo "Thuốc chưa có tồn trong kho này" và **không** cho chọn lô (dòng điều chỉnh vô nghĩa) — trừ khi lý do = `STOCKTAKE` (kiểm kê cho phép nhập lô mới phát hiện thừa, xem edge case §7).
- **AC-4**: Lô **đã hết hạn** (`days_to_expiry < 0`) vẫn hiển thị nhưng gắn badge `critical` "Hết hạn" (được phép điều chỉnh vì có thể là dòng huỷ hàng hết hạn — lý do `EXPIRED`).

### US-DP-04 — Đổi lại / xoá thuốc đã chọn
- **AC-1**: Given 1 dòng đã chọn thuốc, When người dùng focus lại ô Drug Picker và gõ, Then ô chuyển về chế độ tìm kiếm (xoá lựa chọn cũ khỏi hiển thị) và cho chọn thuốc khác; `drug_id` chỉ cập nhật khi chọn xong.
- **AC-2**: Given người dùng bỏ dở (đã xoá text, chưa chọn lại), When blur khỏi ô, Then nếu `drug_id` cũ vẫn còn hợp lệ → khôi phục hiển thị tên thuốc cũ; nếu chưa từng chọn → để trống + báo lỗi khi submit.
- **AC-3**: Nút xoá dòng (icon thùng rác) giữ nguyên; không xoá được dòng cuối cùng nếu form yêu cầu tối thiểu 1 dòng (giữ hành vi hiện tại của 3 form).

### US-DP-05 — Trạng thái rỗng / lỗi
- **AC-1**: Given `q ≥ 2` nhưng API trả mảng rỗng, Then dropdown hiện "Không tìm thấy thuốc" (đã có) + gợi ý phụ "Kiểm tra chính tả hoặc dùng mã thuốc".
- **AC-2**: Given API `/drugs/search` lỗi mạng/5xx, Then dropdown hiện dòng lỗi "Không tải được danh sách thuốc" + không kẹt spinner; người dùng gõ lại để retry (React Query refetch).
- **AC-3**: Trong lúc chờ API, hiện skeleton (đã có) ≤ 3 dòng; không chặn ô input (vẫn gõ tiếp được).

---

## 7. Luồng UX bàn phím — batch input (trọng tâm PO)

Mô tả luồng "tay không rời bàn phím" cho form nhiều dòng (lấy PO làm ví dụ, 3 field/dòng):

```
[Ô Thuốc]  --gõ≥2--> dropdown --↓/↑ chọn--> Enter (chốt thuốc)
   │ Tab
   ▼
[Số lượng] --gõ số--> Tab
   │
   ▼
[Đơn giá]  --gõ số--> Enter (ô cuối dòng cuối)
                         │
                         ▼
              append dòng mới + auto-focus [Ô Thuốc] dòng mới
                         │
                    (lặp lại)
                         │
                   Ctrl/Cmd+S submit
```

Nguyên tắc thiết kế (bám `design-system-standards §7` + `§4 mật độ cao, ít click`):
1. **Tab** = sang field kế trong dòng; **Enter khi dropdown mở** = chọn gợi ý; **Enter khi ở ô cuối dòng cuối** = tạo dòng mới; **Esc** = đóng dropdown (không submit form).
2. **Auto-highlight dòng đầu** khi mở dropdown → đa số trường hợp chỉ cần gõ + Enter là xong 1 thuốc.
3. **Auto-append + auto-focus** dòng mới → loại bỏ thao tác với chuột bấm "Thêm dòng" giữa chừng.
4. Ô Drug Picker của dòng vừa append được **focus và cuộn vào tầm nhìn** (scrollIntoView) để danh sách dài không che.
5. Không dùng phím tắt xung đột trình duyệt; tổ hợp submit `Ctrl/Cmd+S` do trang Fullpage lo, Drug Picker không chặn.
6. Touch target ≥ 44px cho mỗi dòng gợi ý (tablet, đeo găng — §8 design system).

---

## 8. Nội dung hiển thị mỗi dòng gợi ý (chuẩn hoá)

| Vùng | Nội dung | Nguồn field |
|---|---|---|
| Chính | `name_vi` **+** `strength` | `DrugMasterResponse.name_vi`, `.strength` |
| Badge | Hướng thần / Gây nghiện / OTC / **Ngừng KD** | `is_psychotropic`, `is_narcotic`, `!requires_prescription`, `status==="INACTIVE"` |
| Phụ | `form` · `manufacturer` · `price/unit` | `.form`, `.manufacturer`, `.price`, `.unit` |
| Tồn (nếu có) | `Tồn: {on_hand} {unit}` — badge `warning` nếu sắp hết, `critical` nếu = 0 | phụ thuộc §9 (BE bổ sung `stock_on_hand`) |
| Mã | `code` (mono, phải) | `.code` |

Biến thể B thêm dòng phụ lô: `Lô {batch_no} · HSD {dd/MM/yyyy} · tồn {quantity_available} {unit}`.

---

## 9. Phụ thuộc API (rõ tái dùng vs cần thêm)

| API | Trạng thái | Ghi chú cho đợt này |
|---|---|---|
| `GET /api/v1/drugs/search?q=&limit=` | **Tái dùng, không đổi** | Lõi biến thể A. |
| `GET /api/v1/pharmacy/stocks?warehouse_id=&drug_id=&batch_no=` | **Tái dùng, không đổi** | Lõi biến thể B (chọn lô) + hiển thị tồn. |
| Lọc `status=INACTIVE` trong `/drugs/search` | **Cần BE làm rõ** | Hiện search trả cả INACTIVE. Chọn 1 trong 2: (a) BE thêm param `active_only=true` (mặc định true cho picker), hoặc (b) BE luôn trả `status` để FE gắn badge "Ngừng KD" + không cho chọn. **Đề xuất (b)** để không giấu dữ liệu, kèm chặn chọn ở FE. → cần architect/BE xác nhận. |
| Khớp theo hoạt chất `generic_name` / `atc_code` trong search | **Cần BE xác nhận** | US-DP-01 AC-5 yêu cầu gõ hoạt chất ra kết quả. Nếu `/drugs/search` hiện chỉ khớp `name_vi`+`code` thì cần mở rộng WHERE sang `generic_name`, `atc_code`. |
| `stock_on_hand` (tổng tồn theo kho) trong `/drugs/search` | **Nice-to-have (P1)** | Để hiện tồn ngay trong dropdown mà không N+1 query. Nếu chưa có → **Phase 1 bỏ cột tồn trong dropdown**, chỉ hiện tồn ở biến thể B (đã có qua `/pharmacy/stocks`). Không chặn go-live. |

> **Không cần migration DB, không đổi contract API tạo PO/GRN/Adjustment** (vẫn gửi `drug_id`/`batch_no` như cũ — Drug Picker chỉ đổi cách người dùng nhập ra các field đó).

---

## 10. Edge cases

- **Thuốc ngừng kinh doanh (INACTIVE)**: hiển thị badge "Ngừng KD" xám; **không cho chọn** trong PO/GRN (đặt/nhập thuốc đã ngừng là vô lý). Riêng Adjustment cho phép (điều chỉnh/huỷ tồn thuốc đã ngừng vẫn hợp lệ) → badge cảnh báo nhưng chọn được.
- **Lô hết hạn (biến thể B)**: badge `critical` "Hết hạn"; cho chọn (dùng cho lý do `EXPIRED`/`DAMAGED`), không mặc định ẩn.
- **Trùng tên thuốc khác hàm lượng/hãng** (VD Paracetamol 500mg và 650mg, 2 hãng): mỗi kết quả phải phân biệt bằng `strength` + `manufacturer` + `code` để chọn đúng SKU; không gộp.
- **Cùng thuốc chọn 2 dòng**: cho phép (PO có thể tách dòng; GRN có thể 2 lô cùng thuốc). **Không** auto-chặn trùng như ICD-10. *(Nếu nghiệp vụ muốn cảnh báo trùng ở PO → xếp P2.)*
- **Chưa chọn kho ở Adjustment** mà đã chọn thuốc: chưa gọi được `/pharmacy/stocks` → hiện nhắc "Chọn kho trước khi chọn lô", disable phần chọn lô.
- **Kiểm kê (STOCKTAKE) phát hiện thừa lô chưa có trong tồn**: cho phép nhập `batch_no` tay (fallback text) kể cả khi `/pharmacy/stocks` rỗng — vì kiểm kê có thể ghi nhận lô ngoài sổ. Các lý do khác thì bắt buộc chọn từ lô tồn.
- **Danh mục thuốc rỗng / phòng khám mới**: dropdown "Không tìm thấy thuốc" + link phụ tới `/pharmacy/drugs` (nhập/đồng bộ danh mục) — chỉ hiện với quyền `drug.write`.
- **Gõ nhanh (race condition kết quả)**: kết quả cũ về sau kết quả mới → hiển thị đúng theo `query` hiện tại (React Query key theo `q` đã xử lý; đảm bảo không set state từ response cũ).
- **BHYT/viện phí**: Drug Picker không phân biệt nguồn chi trả — đây là bước chọn mặt hàng kho, không liên quan cột BHYT (BHYT xử lý ở kê đơn/thu ngân). Ghi nhận để tránh nhầm phạm vi.

---

## 11. Non-functional & Accessibility

- **Debounce 250ms**, min **2 ký tự**; `limit=20` kết quả; React Query `staleTime ≥ 10s` (đã có) để gõ lùi không refetch.
- **Điều hướng bàn phím** đầy đủ: ↓/↑/Enter/Esc/Tab; `role="listbox"`, `role="option"`, `aria-selected`, `aria-activedescendant` trỏ dòng highlight; `aria-expanded` trên input.
- **Focus ring** hiện rõ khi tab (`--focus-ring`); không `outline:none` trần.
- **Touch target** dòng gợi ý ≥ 44px (tablet).
- **Không truyền tin chỉ bằng màu**: badge trạng thái (Ngừng KD/Hết hạn) luôn kèm chữ + icon.
- **Token-first**: không hardcode màu; dùng `bg-accent`, `text-muted-foreground`, `--status-warning/critical`.
- **i18n**: mọi label/placeholder/empty/error tiếng Việt có dấu (`vi.json`); ngày `dd/MM/yyyy`, tiền `vi-VN`.
- **Performance**: dropdown render ≤ 20 dòng, không lag khi gõ; đóng dropdown huỷ query thừa.

---

## 12. Ưu tiên & thứ tự triển khai

> PO chốt: **tích hợp vào 3 form kho dược TRƯỚC** (nơi đang UUID thô, đau nhất).

| Thứ tự | Việc | Ghi chú |
|---|---|---|
| **P0-1** | Nâng cấp `DrugAutocomplete` → thêm điều hướng bàn phím (US-DP-01 AC-2/3/4) + trạng thái "đã chọn" (US-DP-04) + ngưỡng ≥2 ký tự | Nền tảng, chặn mọi form. |
| **P0-2** | Tích hợp biến thể A vào **PurchaseOrderForm** (Thuốc → SL → Giá) + luồng batch US-DP-02 | Form đơn giản nhất, nghiệm thu luồng bàn phím ở đây. |
| **P0-3** | Tích hợp biến thể A vào **GrnForm** (Thuốc; batch_no vẫn nhập tay — lô mới) | |
| **P0-4** | Biến thể B (Stock-Lot) cho **AdjustmentForm** (Thuốc + Lô từ `/pharmacy/stocks`) | Phụ thuộc chọn Kho ở header. |
| **P1** | Cột "Tồn kho" trong dropdown biến thể A (cần BE `stock_on_hand`) | Bỏ được ở Phase 1 nếu BE chưa sẵn. |
| **P1** | Chuẩn hóa dùng chung: rename/expose `DrugPicker` cho màn kê đơn dùng lại đúng 1 component | Sau khi 3 form kho ổn định. |
| **P2** | "Gần đây / Yêu thích" theo pattern `Icd10Picker` (localStorage) | Tuỳ chọn. |
| **P2** | Cảnh báo chọn trùng thuốc trong PO | Tuỳ nghiệp vụ. |

---

## 13. Out of scope (đợt này)

- Layout Fullpage của 3 form kho dược (thuộc F-03, do Nam làm — architect-decision §3).
- Tạo/sửa danh mục thuốc (`DrugForm`), import Excel, đồng bộ Cục QLD — đã có màn riêng.
- Kiểm tra tương tác thuốc (DDI) khi chọn — thuộc kê đơn, không thuộc kho.
- Gợi ý số lượng đặt hàng theo tồn tối thiểu / lịch sử tiêu thụ (BI) — sprint sau.
- Barcode/QR scan để chọn thuốc — sprint sau (có thể tái dùng field query của picker).

---

## 14. Dependencies & Risks

**Dependencies**
- Component `DrugAutocomplete.tsx` + hook `useDrugSearch` (có sẵn).
- API `/drugs/search`, `/pharmacy/stocks` (có sẵn).
- 3 form Fullpage F-03: nên **tích hợp sau/song song** khi Nam chuyển 3 form sang Fullpage, để tránh sửa 2 lần. Drug Picker không phụ thuộc chặt vào F-03 (có thể gắn ngay cả khi còn Dialog), nhưng **khuyến nghị đồng bộ 1 đợt**.
- BE xác nhận: (a) search khớp `generic_name/atc_code`, (b) cách xử lý `status=INACTIVE` trong search (§9).

**Risks**
| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| `useFieldArray` + focus tự động dòng mới bị lệch (React re-render) | Trung bình | Dùng `ref` theo `field.id`, `setFocus`/`scrollIntoView` sau append; QC test kịch bản 10 dòng liên tục. |
| Enter trong ô Drug Picker vô tình submit form | Cao nếu bỏ qua | Chặn default Enter khi dropdown mở / khi append dòng; chỉ Ctrl+S submit. |
| Search trả INACTIVE → chọn nhầm thuốc ngừng KD | Trung bình | Badge "Ngừng KD" + chặn chọn ở PO/GRN (§10). |
| Không có tồn trong dropdown (BE chưa làm) khiến người đặt thiếu thông tin | Thấp | Phase 1 chấp nhận; biến thể B vẫn có tồn theo lô; bổ sung P1. |
| N+1 query tồn nếu FE tự gọi `/pharmacy/stocks` cho từng gợi ý | Trung bình | KHÔNG gọi per-row; chỉ gọi khi đã chọn thuốc (biến thể B) hoặc chờ BE gộp `stock_on_hand`. |

---

## 15. Đối chiếu PO Checklist (WORKFLOW.md §7 mục)

1. **Completeness**: feature rõ (Drug Picker), actor (DuocSi/KeToan/BacSi), Trigger→Action→Result trong US-DP-01/02, AC dạng Given/When/Then, module = Pharmacy. ✔
2. **Reality check**: happy path (chọn thuốc) + nhiều edge case (INACTIVE, hết hạn, trùng tên, kiểm kê thừa lô); BHYT/viện phí xác định ngoài phạm vi (§10); tái khám không liên quan kho; luật: liên quan gián tiếp TT 27/2021 (kê đơn dùng chung picker). ✔
3. **Integration**: API tái dùng `/drugs/search`, `/pharmacy/stocks`; 2 điểm cần BE xác nhận (§9); RBAC `drug.read`/`pharmacy.*`; không đẩy ĐTQG/XML BHYT; audit không phát sinh mới (thao tác chọn không ghi audit riêng). ✔
4. **System impact**: đổi behavior 3 form (UUID → picker) nhưng contract API giữ nguyên; không migration; rollback = revert component. ✔
5. **Risk**: phụ thuộc F-03 (khuyến nghị đồng bộ), 2 xác nhận BE; không cần seed mới; lỗi production mức minor (form kho, tần suất thấp–trung bình). ✔
6. **UX**: **bàn phím-first** là trọng tâm (US-DP-02); loading/empty/error đủ; touch ≥44px; responsive tablet. ✔
7. **Definition of Done**: mỗi US có ≥1 AC đo được; AC không dùng "nhanh/tốt" (đã định lượng debounce 250ms, ≥2 ký tự, "10 dòng không chạm chuột"); đã liệt kê edge case; đã chỉ rõ role. ✔
