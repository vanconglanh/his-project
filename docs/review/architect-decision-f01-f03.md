# Quyết định kiến trúc — Audit layout F-01 & F-03

> Người quyết định: Lành (architect) · Ngày: 2026-07-02
> Nguồn: `docs/design/layout-audit-forms-2026-07.md` (F-01, F-03) + `docs/design/design-system-standards.md` (mục 7)
> Phạm vi: chỉ ra quyết định kiến trúc + kế hoạch triển khai cho Nam (frontend). KHÔNG sửa code trong lần review này.
> Trạng thái audit gốc: cả F-01 và F-03 đang "⏳ Refactor đợt sau — cần Đăng/Lành xác nhận".

---

## 1. Bối cảnh chung

Audit của Linh (designer) chỉ ra 2 vi phạm chuẩn "Màn hình nhập liệu" (mục 7 design-system-standards): form nhiều field / có sub-list phải là **Fullpage**, không nhét vào Dialog. Cả hai đề xuất đều đúng hướng chuẩn, nhưng khi đọc code tôi phát hiện mỗi đề xuất **thiếu một mắt xích luồng dữ liệu** mà nếu triển khai nguyên văn sẽ gây regress UX hoặc lỗi runtime. Vì vậy cả hai đều là **ĐỒNG Ý CÓ ĐIỀU CHỈNH**, không phải đồng ý nguyên văn.

Các file đã đọc kiểm chứng:
- `frontend/components/domain/ReceptionCheckInForm.tsx`
- `frontend/components/domain/PatientForm.tsx`
- `frontend/app/(dashboard)/patients/new/page.tsx`
- `frontend/app/(dashboard)/patients/_components/PatientEditorLayout.tsx`
- `frontend/app/(dashboard)/reception/page.tsx`
- `frontend/lib/hooks/use-patients.ts`
- `frontend/components/domain/{PurchaseOrderForm,GrnForm,AdjustmentForm}.tsx`
- `frontend/app/(dashboard)/pharmacy/_components/{WarehouseTab,AdjustmentTab,PharmacyPageClient}.tsx`
- `frontend/app/(dashboard)/prescriptions/new/page.tsx` (pattern tham chiếu)

---

## 2. F-01 — Dialog "Tạo bệnh nhân mới" ở Tiếp đón

### 2.1. Phân tích (đã đọc code)

| Giả định của audit | Kết quả kiểm chứng | Kết luận |
|---|---|---|
| `PatientForm.tsx` là bản sao thu gọn, thiếu 6 field | ĐÚNG. Thiếu `id_card_issued_date`, `id_card_issued_place`, `marital_status`, `patient_type`, `visit_type`, `nationality` (so `PatientEditorLayout.tsx:20,63-68`). Ngoài ra 3/4 tab (BHYT, khẩn cấp, dị ứng) chỉ là placeholder rỗng "thêm sau" (`PatientForm.tsx:309-325`). | Đây là **rủi ro toàn vẹn dữ liệu**, không chỉ là vi phạm layout: cùng 1 thực thể (bệnh nhân) có 2 đường tạo với field-set lệch nhau. Bệnh nhân tạo qua Tiếp đón thiếu dữ liệu hành chính so với tạo qua trang chính. |
| Chỉ 1 call site | ĐÚNG. Duy nhất `ReceptionCheckInForm.tsx:262-267`. | Xóa an toàn, không ảnh hưởng nơi khác. |
| `/patients/new` đã hỗ trợ `returnTo` | ĐÚNG. `patients/new/page.tsx:13,18-22` đọc `returnTo`, sau khi tạo push về `${returnTo}?selectPatient={id}`; huỷ thì push về `returnTo`. | Cơ chế điều hướng đã có sẵn, không cần thêm API/route. |
| Reception đã điều hướng sang `/patients/new?returnTo=/reception` | ĐÚNG một phần. Nút "Thêm bệnh nhân" (`reception/page.tsx:68`) và phím F2 (`:24`) đã push đúng URL này. | Nhưng đây là **nút cấp trang**, khác với nút "Tạo bệnh nhân mới" **bên trong dropdown tìm kiếm** của form check-in (`ReceptionCheckInForm.tsx:146-153`) mà audit muốn đổi. |

### 2.2. Mắt xích còn thiếu (audit chưa nêu)

`/patients/new` push về `/reception?selectPatient={id}`, **nhưng `reception/page.tsx` và `ReceptionCheckInForm.tsx` KHÔNG hề đọc query param `selectPatient`**. Trong khi đó luồng Dialog hiện tại có auto-select bệnh nhân vừa tạo (`ReceptionCheckInForm.tsx:77-81`: `handleCreatePatient` → `selectPatient(newPatient)`).

→ Nếu chỉ xóa Dialog và đổi sang `router.push` như audit đề xuất nguyên văn, lễ tân tạo xong bệnh nhân sẽ quay về Tiếp đón với form **rỗng, không tự chọn bệnh nhân vừa tạo** → phải gõ tìm lại → **UX kém hơn hiện tại**. Đây là lý do phải "ĐỒNG Ý CÓ ĐIỀU CHỈNH".

### 2.3. Cân nhắc ngược (theo yêu cầu)

- **Mất context check-in đang nhập dở?** State của `ReceptionCheckInForm` (`room_id`, `reason_for_visit`, `note`, `priority`) là local state, sẽ mất khi điều hướng. Tuy nhiên bệnh nhân là **field đầu tiên** trong luồng thao tác — khi lễ tân bấm "Tạo bệnh nhân mới" thì thường chưa chọn phòng/lý do. Rủi ro mất context là **thấp** trong đa số ca. Trường hợp lễ tân đã chọn phòng trước rồi mới tạo bệnh nhân là hiếm; chấp nhận được.
- **Tạo nhanh giữa lúc check-in?** Đổi sang Fullpage tốn 1 lần chuyển trang, nhưng đổi lại có đủ field chuẩn + validation đầy đủ. Với phòng khám 2-5 bác sĩ, tần suất "tạo bệnh nhân mới hoàn toàn" tại quầy không cao (đa số tái khám → tìm thấy sẵn). Lợi ích toàn vẹn dữ liệu > chi phí 1 lần chuyển trang.

### 2.4. QUYẾT ĐỊNH F-01: **ĐỒNG Ý CÓ ĐIỀU CHỈNH**

Đồng ý bỏ Dialog `PatientForm.tsx` và gộp về `/patients/new`, **kèm điều kiện bắt buộc**: phải nối hoàn chỉnh vòng quay-về auto-select để không regress UX. Cụ thể:

1. **Đổi nút "Tạo bệnh nhân mới"** trong dropdown (`ReceptionCheckInForm.tsx:146-153`): thay `setShowPatientCreate(true)` bằng điều hướng `router.push("/patients/new?returnTo=/reception")` (dùng chung URL với nút cấp trang). Truyền kèm query tìm hiện tại làm gợi ý nếu muốn (tuỳ chọn, không bắt buộc).
2. **Nối vòng auto-select** (mắt xích thiếu): `reception/page.tsx` đọc `searchParams.get("selectPatient")`, truyền xuống `ReceptionCheckInForm` qua prop `preselectPatientId`. Trong form, dùng `usePatient(preselectPatientId)` + `useEffect` để gọi `selectPatient(patient)` khi data về → đạt **parity** với auto-select của luồng Dialog cũ. Sau khi tiêu thụ, strip param khỏi URL (`router.replace("/reception", { scroll: false })`) để refresh không chọn lại nhầm.
3. **Xóa `PatientForm.tsx`** và dọn import chết trong `ReceptionCheckInForm.tsx`: `PatientForm`, `useCreatePatient`, `handleCreatePatient`, state `showPatientCreate`, `createPatientMutation`, block JSX dòng 262-267.

### 2.5. Kế hoạch triển khai F-01

| Bước | File | Thay đổi |
|---|---|---|
| 1 | `frontend/components/domain/ReceptionCheckInForm.tsx` | Nhận `router` (đã có `useRouter`? — chưa, thêm `useRouter`); nút dropdown → `router.push("/patients/new?returnTo=/reception")`; thêm prop `preselectPatientId?: string`; thêm `usePatient` + `useEffect` auto-select; xóa toàn bộ nhánh tạo-trong-Dialog + import chết. |
| 2 | `frontend/app/(dashboard)/reception/page.tsx` | Đọc `useSearchParams().get("selectPatient")`, truyền `preselectPatientId` xuống form; sau khi form nhận, `router.replace("/reception")`. **Lưu ý:** `useSearchParams` trong App Router cần bọc `<Suspense>` (xem rủi ro). |
| 3 | `frontend/components/domain/PatientForm.tsx` | **Xóa file** (deprecate). Grep xác nhận 0 call site còn lại trước khi xóa. |

- **Luồng dữ liệu sau sửa:** Tiếp đón → bấm "Tạo bệnh nhân mới" → `/patients/new?returnTo=/reception` (Fullpage `PatientEditorLayout`, đủ field) → tạo xong → `/reception?selectPatient={id}` → form check-in tự chọn bệnh nhân → lễ tân chọn phòng → Tiếp đón.
- **API/DB:** KHÔNG đụng. `useCreatePatient` đã dùng chung endpoint; không thay đổi contract.
- **FHIR:** không liên quan (chỉ đổi luồng UI của cùng resource `Patient`).

### 2.6. Rủi ro F-01

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| `useSearchParams` chưa bọc `Suspense` ở `reception/page.tsx` → lỗi build/CSR bailout (Next.js 16, xem `frontend/AGENTS.md`) | Trung bình | Bọc phần đọc param trong `<Suspense>` như `patients/new/page.tsx:43-53` đã làm. |
| Mất context phòng/lý do khi chuyển trang | Thấp | Chấp nhận (bệnh nhân là field đầu). Nếu muốn zero-loss có thể lưu draft vào `sessionStorage` — xếp tùy chọn, không bắt buộc đợt này. |
| Vòng auto-select không nối → regress | Cao nếu bỏ qua bước 2 | Bước 2 là **bắt buộc**, QC phải verify sau khi tạo bệnh nhân form tự chọn đúng người. |
| Còn call site ẩn của `PatientForm` | Thấp | Grep `PatientForm` toàn `frontend/` trước khi xóa. |

**Effort F-01: S–M** (frontend-only, ~0.5–1 ngày). Không migration, không API.

---

## 3. F-03 — 3 form kho dược có sub-list trong Dialog

### 3.1. Phân tích (đã đọc code)

| Form | Header field | Sub-list | Phụ thuộc context | Ghi chú |
|---|---|---|---|---|
| `PurchaseOrderForm` | 4 (NCC, kho, ngày giao, ghi chú) | `items[]`: drug_id, SL, đơn giá (`useFieldArray`) | Độc lập | Create thuần, hợp Fullpage sạch. |
| `GrnForm` | 2 (thời gian nhận, ghi chú) | `items[]`: drug_id, lô, NSX, HSD, SL, giá (`useFieldArray`) | **BẮT BUỘC có `poId`** (`GrnForm.tsx:29-35`) | Luôn mở từ nút "Nhập kho" trên 1 dòng PO (`WarehouseTab.tsx:88,133`). |
| `AdjustmentForm` | 3 (kho, lý do, ghi chú) | `items[]`: drug_id, lô, chênh lệch (`useFieldArray`) | Độc lập | Đang có lifted-state phức tạp (xem dưới). |

**Phát hiện thêm (audit chưa nêu):**
- **GRN không thể là route độc lập.** Đề xuất `/pharmacy/grn/new` nguyên văn sẽ thiếu `poId` → form không mutate được (`useCreateGrn(poId)` cần id). Route đúng phải mang context: `/pharmacy/grn/new?poId={id}`.
- **AdjustmentForm đang có nợ kỹ thuật dual-dialog:** `PharmacyPageClient.tsx:41,84-93` nâng state `adjustOpen` lên cấp trang và render Dialog Adjustment **2 nơi** (một trong `AdjustmentTab`, một ở page-level khi tab khác đang active), qua prop `externalOpen`/`onExternalOpenChange`. Convert sang route Fullpage sẽ **xóa được** mớ này → lợi kép (đạt chuẩn + dọn nợ).
- **Cả 3 form dùng input `drug_id` UUID thô** (placeholder "drug_id...") — chưa có drug-picker. Đây là dấu hiệu form còn sơ khai, tần suất dùng thực tế thấp (back-office procurement/kiểm kê), không phải luồng lâm sàng cao tần như kê đơn.

### 3.2. Cân nhắc ngược (theo yêu cầu)

- **Tần suất:** cả 3 là thao tác hậu cần kho, tần suất thấp–trung bình (PO/GRN vài lần/tuần, kiểm kê định kỳ). Luận điểm "Fullpage để thao tác nhanh, mật độ cao" (mục 7) **yếu hơn** so với kê đơn (nhiều lần/ngày). Tức lợi ích chính ở đây là **đúng chuẩn + không vỡ layout sub-list dài**, không phải tốc độ.
- **Độ dài thực tế:** PO và Adjustment có sub-list động không giới hạn dòng → trong Dialog `max-h-[90vh]` sẽ phải cuộn kép (cuộn dialog + cuộn list), UX kém khi >5 dòng. GRN mỗi dòng là 1 card 6 field → càng dài. Đây là lý do chính đáng để Fullpage.
- **Chi phí:** 3 route mới + điều hướng + khôi phục `?tab=` khi quay lại (`PharmacyPageClient` dùng tab query). Vừa phải, nhưng nếu copy-paste shell `prescriptions/new` cho cả 3 sẽ **nhân bản nợ kỹ thuật F-09** (3 form Fullpage hiện đã tự copy header/footer/sticky-bar). Đây là điểm architect phải chặn.

### 3.3. QUYẾT ĐỊNH F-03: **ĐỒNG Ý CÓ ĐIỀU CHỈNH**

Đồng ý chuyển sang Fullpage, nhưng **không nguyên văn 3 route độc lập** và **không copy-paste shell**. Điều chỉnh theo từng form:

1. **`PurchaseOrderForm` → Fullpage `/pharmacy/purchase-orders/new`** — đồng ý đầy đủ. `returnTo` = `/pharmacy?tab=warehouse`. Sau tạo, push về đó.
2. **`AdjustmentForm` → Fullpage `/pharmacy/adjustments/new`** — đồng ý, **và ưu tiên** vì convert kèm dọn nợ dual-dialog: xóa prop `externalOpen`/`onExternalOpenChange` của `AdjustmentTab`, xóa block Dialog page-level trong `PharmacyPageClient.tsx:84-93`, nút "Tạo điều chỉnh" chuyển thành `Link`/`router.push` tới route. `returnTo` = `/pharmacy?tab=adjustment`.
3. **`GrnForm` → Fullpage NHƯNG mang context: `/pharmacy/grn/new?poId={id}`** — **điều chỉnh so với đề xuất**. Không tạo `/pharmacy/grn/new` trống. Nút "Nhập kho" trên dòng PO (`WarehouseTab.tsx:88`) đổi thành điều hướng kèm `poId` + `returnTo=/pharmacy?tab=warehouse`. Route đọc `poId` từ query, truyền vào `GrnForm`.
   - *Phương án chấp nhận tối thiểu (nếu cắt scope):* giữ GRN dạng **Sheet rộng `sm:max-w-2xl px-6 pb-6`** (đúng mức fallback mà chính audit cho phép ở cột "Sửa thành") — vì GRN luôn là sub-action của 1 PO và ngắn hơn PO/Adjustment. Xem trade-off mục 3.5.
4. **Điều kiện chặn nợ kỹ thuật (liên quan F-09):** TRƯỚC khi tạo 3 page, tách shell dùng chung `FullPageFormShell` (header quay-lại + title + sticky action bar + Ctrl+S/Esc) theo đề xuất `input-form-layout-spec.md` mục 10 (hiện chưa tồn tại — F-09). 3 route kho dược là nơi lý tưởng để "khai sinh" shell này thay vì copy-paste `prescriptions/new`. Nếu không làm shell, tối thiểu **bắt buộc dùng `StickyActionBar`** (`components/ui/sticky-action-bar.tsx`) đang 0% sử dụng.

### 3.4. Kế hoạch triển khai F-03

| Bước | File tạo/sửa | Thay đổi |
|---|---|---|
| 0 (nền) | `frontend/components/ui/FullPageFormShell.tsx` (mới, tuỳ chọn nhưng khuyến nghị) | Shell dùng chung: header + `StickyActionBar` + listener Ctrl+S/Esc. Dùng lại `StickyActionBar` sẵn có. |
| 1 | `frontend/app/(dashboard)/pharmacy/purchase-orders/new/page.tsx` (mới) | Bọc `PurchaseOrderForm`; đọc `returnTo`; onSuccess → push `/pharmacy?tab=warehouse`. |
| 2 | `frontend/app/(dashboard)/pharmacy/adjustments/new/page.tsx` (mới) | Bọc `AdjustmentForm`; onSuccess → `/pharmacy?tab=adjustment`. |
| 3 | `frontend/app/(dashboard)/pharmacy/grn/new/page.tsx` (mới) | Đọc `poId` (bắt buộc) + `returnTo`; nếu thiếu `poId` → redirect về `/pharmacy?tab=warehouse` + toast "Chọn đơn đặt hàng trước". Bọc `GrnForm poId={poId}`. |
| 4 | `frontend/app/(dashboard)/pharmacy/_components/WarehouseTab.tsx` | Nút "Tạo đơn đặt hàng" → `Link`/`push` route PO; nút "Nhập kho" (dòng 88) → `push("/pharmacy/grn/new?poId="+po.id+"&returnTo=/pharmacy?tab=warehouse")`; xóa 2 Dialog (dòng 116-136) + import Dialog/Form không cần. |
| 5 | `frontend/app/(dashboard)/pharmacy/_components/AdjustmentTab.tsx` | Bỏ `externalOpen`/`onExternalOpenChange`; nút "Tạo điều chỉnh" → route; xóa Dialog (dòng 144-152). |
| 6 | `frontend/app/(dashboard)/pharmacy/_components/PharmacyPageClient.tsx` | Xóa state `adjustOpen`, nút page-level (dòng 55-64) và Dialog page-level (dòng 83-93); nút chuyển thành `Link` route. Giữ nguyên logic `?tab=`. |
| 7 | 3 form `PurchaseOrderForm/GrnForm/AdjustmentForm` | Không đổi logic form; chỉ đảm bảo `onSuccess` callback vẫn được page dùng để điều hướng. Cân nhắc bọc nút submit bằng `StickyActionBar` (hoặc để shell lo). |

- **Luồng dữ liệu:** không đổi API/hook (`useCreatePurchaseOrder`, `useCreateGrn(poId)`, `useCreateAdjustment` giữ nguyên). Chỉ đổi container Dialog → route + cơ chế điều hướng quay về đúng tab.
- **API/DB:** KHÔNG đụng contract, KHÔNG migration.
- **FHIR:** không liên quan (nghiệp vụ kho nội bộ, ngoài phạm vi FHIR resource lâm sàng).

### 3.5. Trade-off GRN: Fullpage `?poId=` vs Sheet rộng

| Tiêu chí | Fullpage `/pharmacy/grn/new?poId=` | Sheet `sm:max-w-2xl` |
|---|---|---|
| Đúng chuẩn Rule 1 (sub-list → Fullpage) | Đạt hoàn toàn | Đạt mức fallback audit cho phép |
| Không gian cho batch list dài | Rộng, thoải mái | Hẹp, cuộn dọc, đủ dùng vì GRN thường ít lô |
| Giữ context PO đang xem | Rời khỏi bảng PO (cần returnTo) | Ở lại bảng PO, overlay — trực quan hơn cho "nhập theo dòng PO" |
| Chi phí | 1 route mới + threading poId | Đổi className + wrapper, rẻ hơn |
| Nhất quán với PO/Adjustment | Cao (cả 3 cùng Fullpage) | Lệch (2 Fullpage + 1 Sheet) |

**Chọn:** Fullpage `?poId=` để nhất quán 3 form và đúng chuẩn hoàn toàn. Sheet rộng là **phương án lùi hợp lệ** nếu cần cắt scope/thời gian — vì GRN gắn chặt ngữ cảnh 1 PO và thường ít dòng lô. Ghi nhận cả hai để Đăng/Nam chốt theo dung lượng sprint.

### 3.6. Rủi ro F-03

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| `/pharmacy/grn/new` bị vào trực tiếp thiếu `poId` | Trung bình | Guard redirect + toast ở đầu page (bước 3). |
| Mất `?tab=` khi quay về pharmacy | Thấp | `returnTo` mang sẵn `?tab=warehouse`/`adjustment`; verify `PharmacyPageClient` đọc lại đúng. |
| Nhân bản copy-paste shell (F-09) | Trung bình | Bước 0: tách `FullPageFormShell`/dùng `StickyActionBar`. Bắt buộc cho form mới theo F-09. |
| 3 form còn dùng UUID thô, chưa có drug-picker | Ngoài phạm vi layout | Ghi nhận, chuyển Đăng (po-analyst): cần drug autocomplete để form dùng được thực tế — **không** gộp vào đợt refactor layout này. |
| Tăng surface test cho QC (3 route mới) | Thấp | Cùng pattern `prescriptions/new`, test theo checklist mục 7. |

**Effort F-03: M** (nếu bỏ bước 0) → **M–L** (nếu làm shell dùng chung, khuyến nghị). ~2–3 ngày frontend. Không migration, không API.

---

## 4. Tóm tắt quyết định

| Finding | Quyết định | Điều chỉnh cốt lõi so với đề xuất audit | Effort | Đụng API/DB |
|---|---|---|---|---|
| **F-01** | **ĐỒNG Ý CÓ ĐIỀU CHỈNH** | Bỏ Dialog + gộp về `/patients/new` **NHƯNG bắt buộc nối vòng auto-select** (`?selectPatient=` chưa được đọc ở reception) để không regress UX. | S–M | Không |
| **F-03** | **ĐỒNG Ý CÓ ĐIỀU CHỈNH** | PO + Adjustment → Fullpage (Adjustment kèm dọn nợ dual-dialog). GRN → Fullpage **mang `?poId=`** (không thể route trống) hoặc Sheet rộng làm phương án lùi. Tách shell dùng chung trước khi copy-paste (chặn F-09). | M–L | Không |

**Việc cần Đăng (po-analyst) xác nhận:**
1. F-01: chấp nhận đánh đổi "mất context phòng/lý do khi chuyển trang" (thấp) — hay cần lưu draft `sessionStorage`?
2. F-03/GRN: chốt Fullpage `?poId=` hay Sheet rộng theo dung lượng sprint.
3. F-03: đưa "drug-picker thay UUID thô" thành PRD riêng (ngoài phạm vi layout).

---

*Ghi chú: Không viết business logic trong review này. Mọi thay đổi ở trên là quyết định kiến trúc + hướng dẫn triển khai cho Nam (frontend); QC (Chi) gác cổng theo checklist mục 7 design-system-standards.*
