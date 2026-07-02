# Audit layout form nhập liệu — Pro-Diab HIS

> Người audit: Linh (designer) · Ngày: 2026-07-02
> Đối chiếu: `docs/design/design-system-standards.md` (mục 7) + `docs/design/input-form-layout-spec.md`
> Phạm vi: Bệnh nhân, Tiếp đón, Khám, Kê đơn, Kho dược, Thu ngân, Admin (users/tenants)
> Nguyên tắc: chỉ ghi finding đã đọc tận mắt code, có file:dòng cụ thể.

## Trạng thái xử lý (cập nhật 2026-07-02, cùng ngày)

| Finding | Trạng thái | Ghi chú |
|---------|-----------|---------|
| F-01 Dialog tạo BN ở Tiếp đón | ⏳ Refactor đợt sau | Gộp luồng về `/patients/new`, cần Đăng/Lành xác nhận |
| F-02 DrugForm create dùng Dialog | ✅ Đã sửa | → Sheet `sm:max-w-2xl px-6 pb-6` (giống khối Sửa) |
| F-03 PO/GRN/Adjustment trong Dialog | ⏳ Refactor đợt sau | Tạm nâng Dialog → `max-w-4xl` (L-05); Fullpage route để đợt sau |
| F-04 TenantForm dùng Dialog | ✅ Đã sửa | Create + Edit → Sheet `sm:max-w-2xl px-6 pb-6` |
| F-05 Sheet detail admin | ✅ Đã sửa | 4 màn → `sm:max-w-xl px-6 pb-6` |
| F-06 Form sinh hiệu `gap-3` | ✅ Đã sửa | → `gap-4` |
| F-07 DiabetesAssessmentForm thiếu sticky bar | ✅ Đã sửa | Đã bọc `StickyActionBar` (lần dùng đầu tiên của component này) |
| F-08 Ctrl+S quảng cáo suông | ✅ Đã sửa | Listener thật cho `encounters/new` + `prescriptions/new` |
| F-09 StickyActionBar/FieldGroup 0% dùng | ⏳ Nợ kỹ thuật | Bắt buộc dùng cho form mới; mâu thuẫn Sheet mục 3 vs 7 đã hóa giải trong standards mục 3 |

**Lưu ý đối chiếu chuẩn:** `design-system-standards.md` mục 3 (bảng Spacing·Layout·Panel) ghi Sheet chuẩn là `sm:max-w-2xl`, trong khi mục 7 (Màn hình nhập liệu) ghi Sheet chuẩn `sm:max-w-xl` — hai mục trong CÙNG một tài liệu đang lệch nhau. Audit này dùng mục 7 (chuẩn form) làm gốc cho form nhập liệu, và ghi nhận `sm:max-w-2xl` là chấp nhận được khi form cần 2 cột (variant `sheet-wide` trong `input-form-layout-spec.md` mục 7/10). Đề nghị Đăng/Lành thống nhất lại 2 mục này ở lần cập nhật doc tiếp theo (không thuộc phạm vi sửa code đợt này).

---

### F-01 — Tiếp đón: Dialog "Tạo bệnh nhân mới" (`PatientForm.tsx`) dùng trong `ReceptionCheckInForm`

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P1 — **refactor, đợt sau** | `frontend/components/domain/PatientForm.tsx:134` | `<DialogContent className="!max-w-6xl sm:!max-w-6xl w-[95vw] h-[90vh] max-h-[90vh] overflow-hidden flex flex-col !p-0 !gap-0">` chứa form 13 field, 4 tab (trùng gần như 100% field-set của `PatientEditorLayout`) | Rule 1 (input-form-layout-spec mục 2): form >5 field, nhiều section → **Fullpage**, không phải Dialog. Dialog chuẩn tối đa `max-w-4xl` cho bảng phức tạp (mục 3 design-system-standards), `!max-w-6xl` vượt cả mức đó. | Bỏ Dialog này, đổi luồng "Tạo bệnh nhân mới" trong `ReceptionCheckInForm.tsx:146-153` thành điều hướng tới route đã có sẵn và đúng chuẩn: `router.push("/patients/new?returnTo=/reception")` (giống nút "Thêm bệnh nhân" ở `reception/page.tsx:68`). Xoá/deprecate `PatientForm.tsx` (chỉ 1 call site duy nhất tại `ReceptionCheckInForm.tsx:262-267`). |

Ghi chú: `PatientForm.tsx` là bản sao thu gọn của `PatientEditorLayout` (thiếu các field `id_card_issued_date`, `id_card_issued_place`, `marital_status`, `patient_type`, `visit_type`, `nationality` — dữ liệu bệnh nhân tạo qua tiếp đón sẽ thiếu so với tạo qua `/patients/new`). Đây là lý do nên gộp về 1 luồng thay vì chỉnh riêng className.

---

### F-02 — Kho dược: `DrugForm` dùng 2 loại container khác nhau cho Tạo/Sửa cùng 1 form

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P1 — **sửa ngay** (đổi container, cơ học) | `frontend/app/(dashboard)/drugs/_components/DrugsPageClient.tsx:215-220` | "Tạo thuốc mới" bọc trong `<Dialog><DialogContent className="max-w-2xl">` — trong khi `DrugForm` (`frontend/components/domain/DrugForm.tsx`) có **15 field** (code, name_vi, name_en, generic_name, atc_code, strength, unit, form, manufacturer, country, price, dtqg_drug_code + 3 switch) | Rule 2: Dialog chỉ dùng khi ≤4 field đơn giản. 15 field vượt xa cả ngưỡng Sheet (≤8). "Sửa thuốc" cùng form này đã đúng chuẩn — dùng Sheet (dòng 223-232) | Đổi khối tạo (dòng 215-220) từ `Dialog`/`DialogContent` sang `Sheet`/`SheetContent` giống hệt khối sửa: `<Sheet open={createOpen} onOpenChange={setCreateOpen}><SheetContent className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"><SheetHeader className="px-0"><SheetTitle>Tạo thuốc mới</SheetTitle></SheetHeader><div className="mt-4"><DrugForm onSuccess={...} onCancel={...}/></div></SheetContent></Sheet>` |

---

### F-03 — Kho dược: Form có sub-list (đặt hàng NCC / nhập kho GRN / điều chỉnh tồn) nhét trong Dialog nhỏ

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P1 — **refactor, đợt sau** | `frontend/app/(dashboard)/pharmacy/_components/WarehouseTab.tsx:117-124` (Create PO), `:127-136` (GRN); `frontend/app/(dashboard)/pharmacy/_components/AdjustmentTab.tsx:145-152` (Điều chỉnh tồn kho) | Cả 3 Dialog đều `max-w-2xl` (có nơi thêm `max-h-[90vh] overflow-y-auto`), nhưng nội dung là `PurchaseOrderForm.tsx` (dòng 90-116), `GrnForm.tsx` (dòng 66-110), `AdjustmentForm.tsx` (dòng 106-156) — mỗi form đều có **field-array thêm/xoá dòng thuốc động** (nút "Thêm dòng"/"Thêm lô") | Rule 1: "Có sub-list (kê nhiều thuốc, nhiều dịch vụ CLS) → Fullpage". Đây đúng là pattern sub-list mà `prescriptions/new` đã làm đúng (Fullpage, `useFieldArray`) | Convert 3 form này sang Fullpage route (`/pharmacy/purchase-orders/new`, `/pharmacy/grn/new`, `/pharmacy/adjustments/new`) theo pattern `prescriptions/new/page.tsx`, hoặc tối thiểu nâng lên Sheet rộng `sm:max-w-2xl px-6 pb-6` có scroll riêng cho vùng sub-list (giống mức chấp nhận của `DrugForm`). Đây là việc tạo route mới + điều hướng, không phải đổi className đơn thuần → xếp loại refactor. |

---

### F-04 — Admin: `TenantForm` (7-11 field) nhét trong Dialog thay vì Sheet

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P1 — **sửa ngay** (đổi container, cơ học) | `frontend/app/(dashboard)/admin/tenants/page.tsx:238-254` (Create), `:257-278` (Edit) | `<Dialog><DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">` chứa `TenantForm` — bản Create có **11 field** (`code, subdomain, name, email, phone, cskcb_code, tax_code, address, storage_quota_gb, admin_email, admin_full_name`), bản Edit có **7 field** | `input-form-layout-spec.md` mục 1 (audit gốc) đã tự ghi nhận: "Chỉnh sửa tenant … Cần verify — nếu >5 field nên dùng Sheet `max-w-2xl`". Rule 3: Sheet áp dụng ≤8 field (Edit) hoặc sub-section 2 cột (Create, biến thể `sheet-wide`) | Đổi `Dialog`/`DialogContent` (2 khối) sang `Sheet`/`SheetContent className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"` — giữ nguyên `TenantForm` bên trong, chỉ đổi wrapper + thêm `px-6 pb-6`. |

---

### F-05 — Admin: Sheet "Chi tiết" dùng width tùy tiện theo px, thiếu `px-6 pb-6`

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P2 — **sửa ngay** (đổi className) | `frontend/app/(dashboard)/admin/users/page.tsx:254`, `frontend/app/(dashboard)/admin/tenants/page.tsx:282`, `frontend/app/(dashboard)/admin/roles/page.tsx:139`, `frontend/app/(dashboard)/admin/audit/page.tsx:156` | `<SheetContent className="w-[480px] sm:w-[480px] overflow-y-auto">` (roles: `w-[600px]`, audit: `w-[500px]`) — **hardcode px**, không dùng thang `max-w-*`, và **thiếu `px-6 pb-6`** bắt buộc | Mục 3 design-system-standards.md: "Sheet … luôn `px-6 pb-6` (không sát mép)"; khoảng cách/width chỉ dùng scale Tailwind, không `w-[13px]` tùy tiện | Đổi cả 4 chỗ thành `className="w-full sm:max-w-xl overflow-y-auto px-6 pb-6"` (nội dung hiện là chi tiết dạng key-value, không cần 2xl). Đây không phải form nhập liệu (chỉ xem chi tiết read-only) nhưng cùng vi phạm chuẩn Sheet nên nêu kèm để Nam sửa đồng loạt. |

---

### F-06 — Khám bệnh: Form nhập sinh hiệu dùng `gap-3` thay vì `gap-4`

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P3 — sửa ngay (đổi className) | `frontend/app/(dashboard)/encounters/[id]/_components/EncounterDetailClient.tsx:554,564,578` | `<div className="grid grid-cols-2 gap-3">` (3 hàng field mạch/nhiệt độ, HA/SpO2, cân nặng/chiều cao) | Mục 7 design-system-standards.md: "Label trên input, `gap-4`" | Đổi `gap-3` → `gap-4` ở cả 3 dòng. |

---

### F-07 — Khám bệnh: `DiabetesAssessmentForm` là form dài (5 section, ~20 field) nhưng không có sticky action bar

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P2 — sửa ngay (bọc lại nút submit bằng component có sẵn) | `frontend/components/domain/DiabetesAssessmentForm.tsx:223-225` (nút submit đơn, `min-h-[44px]`, căn trái theo flow); render tại `frontend/app/(dashboard)/encounters/[id]/_components/EncounterDetailClient.tsx:303-318` (`TabsContent value="diabetes"`) | Form có 5 Card section (chỉ số đường huyết, chức năng thận, tim mạch & nhân trắc, phân loại & biến chứng, mục tiêu điều trị) + 1 chart phía trên — người dùng phải cuộn hết trang mới thấy nút "Lưu đánh giá ĐTĐ" | Mục 7 design-system-standards.md: "Sticky action bar (`sticky-action-bar.tsx`) cho form dài; nút chính bên phải" — form này thỏa điều kiện "form dài" dù được render inline trong tab (không phải Fullpage/Dialog/Sheet riêng) | Bọc nút submit bằng component có sẵn nhưng **chưa từng được dùng** trong codebase: `import { StickyActionBar } from "@/components/ui/sticky-action-bar"` rồi thay khối `<Button type="submit" ...>` bằng `<StickyActionBar><Button type="submit" disabled={isLoading} className="min-h-[44px]">{...}</Button></StickyActionBar>` (sticky trong phạm vi tab-content scroll container). |

---

### F-08 — Khám bệnh & Kê đơn: Footer quảng cáo phím tắt Ctrl+S nhưng không có listener thực

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P1 — sửa ngay (thêm useEffect, đã có mẫu sẵn trong repo) | `frontend/app/(dashboard)/encounters/new/page.tsx:313`, `frontend/app/(dashboard)/prescriptions/new/page.tsx:384` | `<span className="text-xs text-muted-foreground">Ctrl+S lưu · Esc quay lại</span>` — nhưng không có `document.addEventListener("keydown", ...)` nào trong 2 file này bắt `Ctrl+S`/`Esc` | Mục 7 design-system-standards.md: Fullpage form "Cần keyboard shortcut (Ctrl+S)" là điều kiện định danh Fullpage; `PatientEditorLayout.tsx:123-137` đã cài đúng (bắt `(ctrlKey|metaKey)+s` → submit, `Escape` → cancel) | Copy nguyên khối `useEffect` bắt phím từ `PatientEditorLayout.tsx:123-137` sang 2 file trên (đổi target `handleSubmit(onSubmit)` / `router.back()` tương ứng). |

---

### F-09 — Toàn hệ thống: Component tái sử dụng `StickyActionBar` và `FieldGroup` tồn tại nhưng 0% được dùng

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|---|---|---|---|---|
| P2 — quy trình, sửa dần (không breaking, nhưng nợ kỹ thuật tăng dần) | `frontend/components/ui/sticky-action-bar.tsx` (toàn file), `frontend/components/ui/field-group.tsx` (toàn file) — grep toàn `frontend/` chỉ thấy định nghĩa, **không có nơi nào import** (`StickyActionBar` / `FieldGroup`) | Mọi form Fullpage (`PatientEditorLayout.tsx`, `encounters/new/page.tsx`, `prescriptions/new/page.tsx`) tự viết lại footer sticky + label/error riêng thay vì dùng 2 component chuẩn hoá này → trùng lặp code, dễ lệch dần (vd F-06 gap-3 vs gap-4, F-07 thiếu sticky bar) | Mục 7 + hand-off (`input-form-layout-spec.md` mục 13): các form dài nên dùng `sticky-action-bar.tsx`; field nên dùng `field-group.tsx` cho label/error/hint đồng nhất | Không sửa gấp toàn bộ (breaking nếu đổi cả 3 fullpage form cùng lúc) — nhưng bắt buộc dùng `StickyActionBar` cho form MỚI (áp dụng ngay ở F-07). Đề xuất thêm: `FullPageFormShell`/`FormSection`/`FormFieldGrid` (đề xuất trong `input-form-layout-spec.md` mục 10) vẫn **chưa được tạo** — 3 file Fullpage vẫn copy-paste header/sidebar/footer thủ công; giữ nguyên trạng thái "đợt sau" như đã note sẵn trong spec mục 13, không lặp lại thành finding mới. |

---

## Đạt chuẩn

Các form sau đã kiểm tra và **khớp chuẩn** `design-system-standards.md` mục 7 + `input-form-layout-spec.md`, không có finding:

| Form | File | Loại | Ghi chú |
|---|---|---|---|
| Tạo/Sửa bệnh nhân | `patients/_components/PatientEditorLayout.tsx` | Fullpage, `max-w-5xl`, sidebar tab, sticky header/footer, Ctrl+S/Esc hoạt động thật | Đúng pattern gốc, xứng đáng là mẫu tham chiếu |
| Tạo lượt khám | `encounters/new/page.tsx` | Fullpage, `max-w-5xl`, section card `rounded-xl border bg-card p-6 space-y-4`, `gap-4` | Đã convert đúng theo đề xuất cũ trong `input-form-layout-spec.md`; chỉ còn thiếu Ctrl+S thật (F-08) |
| Kê đơn thuốc | `prescriptions/new/page.tsx` | Fullpage, cấu trúc y hệt encounters/new, có `useFieldArray` cho chẩn đoán | Đúng pattern; chỉ còn thiếu Ctrl+S thật (F-08) |
| Sửa thuốc trong danh mục | `drugs/_components/DrugsPageClient.tsx` (Sheet, dòng 223-232) | Sheet `sm:max-w-2xl px-6 pb-6` | Đúng chuẩn — dùng làm mẫu để đồng bộ khối Create (F-02) |
| Ký EMR | `components/domain/EmrSignDialog.tsx` | Dialog `sm:max-w-md`, confirm-style | Đúng |
| Phát thuốc | `components/domain/DispenseConfirmDialog.tsx` | Dialog `max-w-lg` | Đúng, action đơn bước |
| Thu tiền | `components/domain/PaymentDialog.tsx` | Dialog `max-w-lg`, field hiện có điều kiện | Đúng, phím tắt 1-7 chọn phương thức đã hoạt động thật |
| Mở/Đóng ca thu ngân | `components/domain/CashierShiftOpenDialog.tsx`, `CashierShiftCloseDialog.tsx` | Dialog `max-w-sm` | Đúng, action đơn bước |
| Mời người dùng | `components/domain/InviteUserForm.tsx` trong `admin/users/page.tsx:236-250` | Dialog `max-w-lg` | Đã áp dụng đúng đề xuất nâng cấp cũ trong spec |
| Gán vai trò | `components/domain/AssignRolesForm.tsx` trong `admin/users/page.tsx:263-275` | Dialog `max-w-lg` | Đúng |
| Export BHYT | `components/domain/bhyt/BhytExportForm.tsx` | Dialog `sm:max-w-md` | Đúng, 4 field |
| Chỉ định XN | `components/domain/LabOrderForm.tsx` | Inline trong tab, `gap-4` | Đúng pattern "inline tab" |
| Chỉ định CĐHA | `components/domain/RadOrderForm.tsx` | Inline trong tab, `gap-4` | Đúng pattern "inline tab" |
| Tiếp đón (check-in) | `components/domain/ReceptionCheckInForm.tsx` | Inline trong page, 5 field | Chấp nhận được — action nhanh, không có sub-list; phần tạo nhanh bệnh nhân bên trong xem F-01 |

---

## Tổng hợp

| Mức | Số lượng | Finding |
|---|---|---|
| P0 | 0 | — |
| P1 | 5 | F-01, F-02, F-03, F-04, F-08 |
| P2 | 3 | F-05, F-07, F-09 |
| P3 | 1 | F-06 |
| **Tổng** | **9** | |

### Đề xuất thứ tự xử lý (Nam — frontend)

1. **Sửa ngay trong tuần này** (cơ học, rủi ro thấp, không cần route mới):
   - F-08 (Ctrl+S thật cho `encounters/new`, `prescriptions/new`) — copy code có sẵn.
   - F-02 (đổi Dialog→Sheet cho "Tạo thuốc mới") — copy pattern có sẵn ngay bên cạnh (Sheet Sửa thuốc).
   - F-04 (đổi Dialog→Sheet cho `TenantForm`) — cùng pattern F-02.
   - F-05 (chuẩn hoá 4 Sheet detail admin về `sm:max-w-xl px-6 pb-6`).
   - F-06 (`gap-3`→`gap-4` trong form sinh hiệu).
   - F-07 (bọc `StickyActionBar` có sẵn quanh nút submit `DiabetesAssessmentForm`).
2. **Refactor đợt sau** (cần tạo route/component mới, ảnh hưởng luồng nghiệp vụ, cần Đăng/Lành xác nhận trước khi đổi):
   - F-01 (gộp luồng tạo bệnh nhân nhanh về `/patients/new`, xoá `PatientForm.tsx`).
   - F-03 (convert `PurchaseOrderForm`/`GrnForm`/`AdjustmentForm` sang Fullpage hoặc Sheet rộng).
3. **Theo dõi nợ kỹ thuật** (không chặn release):
   - F-09 (thúc đẩy dùng `StickyActionBar`/`FieldGroup` cho mọi form mới; xem xét dựng `FullPageFormShell`/`FormSection`/`FormFieldGrid` như đã đề xuất sẵn trong `input-form-layout-spec.md` mục 10/13).
   - Thống nhất lại mâu thuẫn Sheet width giữa mục 3 và mục 7 của `design-system-standards.md`.

Sau khi Nam sửa xong theo mức ưu tiên trên, chuyển Chi (qc) gác cổng xác nhận đã khắc phục.
