# Input Form Layout Spec — Pro-Diab HIS

## Mục tiêu UX

Thống nhất cách trình bày form nhập liệu toàn hệ thống: người dùng biết ngay form nào mở toàn trang, form nào mở dialog, tránh bị "bẫy" trong dialog chật hẹp khi nhập nhiều dữ liệu phức tạp.

---

## 1. Audit hiện trạng

| Form | File hiện tại | Loại hiện tại | Số field | Đánh giá |
|------|---------------|---------------|----------|----------|
| Tạo / sửa bệnh nhân | `patients/_components/PatientEditorLayout.tsx` | Fullpage + sidebar tab | ~20 field (4 tab) | Đúng pattern |
| Tạo lượt khám | `encounters/_components/EncountersPageClient.tsx` — `CreateEncounterDialog` | Dialog `max-w-md` | 5 field (patient search + type + reason + complaint) | Cần convert → fullpage (có autocomplete phức tạp + textarea) |
| Kê đơn (tạo mới) | `/prescriptions/new` — `PrescriptionDetailClient.tsx` | Fullpage | Multi-item | Đúng pattern |
| Thuốc danh mục | `drugs/_components/DrugsPageClient.tsx` — `DrugForm` in Sheet | Sheet (slide-over) | ~10 field | Chấp nhận được — Sheet phù hợp edit inline |
| Mời người dùng | `components/domain/InviteUserForm.tsx` trong Dialog | Dialog | 4 field + role checkboxes | Biên giới — hiện ổn, cần cải thiện chiều rộng lên `max-w-lg` |
| Export BHYT | `components/domain/bhyt/BhytExportForm.tsx` | Dialog `sm:max-w-md` | 4 field | Biên giới — nếu thêm bước preview items phải convert fullpage multi-step |
| Check-in tiếp đón | `components/domain/ReceptionCheckInForm.tsx` | Inline trong page | ~3 field | Đúng — action nhanh |
| Phân công vai trò | `components/domain/AssignRolesForm.tsx` | Dialog | Role multiselect | Đúng pattern |
| Chỉnh sửa tenant | `admin/tenants` | Dialog/Sheet | ~8 field | Cần verify — nếu >5 field nên dùng Sheet `max-w-2xl` |
| Kết quả CLS | `labrad/_components/LabResultsTab.tsx` | Inline tab | Multi-result | Phù hợp |

---

## 2. Quy tắc phân loại form

### Rule 1 — Fullpage (route riêng)
Áp dụng khi **bất kỳ** điều kiện sau đúng:
- Tổng số field > 5, hoặc
- Form chia nhiều section/nhóm logic (BHYT, địa chỉ, liên hệ khẩn cấp…), hoặc
- Có sub-list (kê nhiều thuốc, nhiều dịch vụ CLS), hoặc
- Cần unsaved-changes warning, hoặc
- Cần keyboard shortcut (Ctrl+S)

### Rule 2 — Dialog (`Dialog` shadcn)
Áp dụng khi **tất cả** điều kiện sau đúng:
- ≤ 4 field đơn giản (text/select/date), **không có** sub-list, **không có** file upload, và
- Hành động đơn (1 bước, không multi-step), và
- Không cần xem dữ liệu khác trong khi điền

### Rule 3 — Sheet (side panel)
Áp dụng khi:
- Form ≤ 8 field + cần nhìn sang nội dung list phía sau (vd edit thuốc trong danh mục), hoặc
- View-while-edit: điều chỉnh kho, cấu hình nhanh

---

## 3. Wireframe — Fullpage Form Shell

```
┌──────────────────────────────────────────────────────────────────────┐
│ HEADER sticky (h-14, border-b, backdrop-blur)                        │
│  [← Quay lại]   [Page Title — Section Name]   [Huỷ] [Lưu / Tạo]    │
├────────────────────────────────────────────────────────────────────  │
│                                                                       │
│  SIDEBAR (lg+, w-52, sticky top-14)  │  CONTENT (flex-1, overflow-y) │
│  ┌──────────────────────────────┐    │  ┌───────────────────────────┐ │
│  │ • Tab 1                      │    │  │ max-w-5xl mx-auto px-8    │ │
│  │ ● Tab 2 (active)        [2]  │    │  │ py-8                      │ │
│  │ • Tab 3                      │    │  │                           │ │
│  │ • Tab 4                      │    │  │ ┌─ Section Card ────────┐ │ │
│  └──────────────────────────────┘    │  │ │ [icon] Title           │ │ │
│                                      │  │ │ Description            │ │ │
│  MOBILE: bottom tab bar              │  │ │ ─────────────────────  │ │ │
│  fixed bottom-16, scroll-x          │  │ │ [Field] [Field]        │ │ │
│                                      │  │ └───────────────────────┘ │ │
│                                      │  │ ┌─ Section Card 2 ──────┐ │ │
│                                      │  │ │ ...                    │ │ │
│                                      │  │ └───────────────────────┘ │ │
│                                      │  └───────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────┤
│ FOOTER sticky bottom (lg+, border-t, h-14, backdrop-blur)            │
│  [Ctrl+S lưu · Esc quay lại]              [Huỷ]  [Lưu thay đổi]    │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 4. Wireframe — Dialog Form

```
┌────────────────────────────────────────┐
│ DialogHeader                            │
│  Tiêu đề ngắn gọn                [✕]  │
├────────────────────────────────────────┤
│ Body (px-6 pb-6, space-y-4)            │
│  Label                                  │
│  ┌─────────────────────────────────┐   │
│  │ Input / Select                  │   │
│  └─────────────────────────────────┘   │
│  [error message]                        │
│  ...                                    │
├────────────────────────────────────────┤
│ DialogFooter (flex justify-end gap-2)   │
│                         [Huỷ] [Submit] │
└────────────────────────────────────────┘
```

Max width: `sm:max-w-md` (480px) cho form ≤4 field; `sm:max-w-lg` (512px) nếu có checkbox grid (vd role list).

---

## 5. Wireframe — Sheet Form

```
                    ┌─────────────────────────────────────────┐
                    │ SheetHeader (px-6 pt-6 pb-4)            │
                    │  Tiêu đề                           [✕]  │
                    │  [Subtitle muted]                        │
                    ├──────────────────────────────────────────┤
                    │ Body (px-6, flex-1, overflow-y-auto)     │
                    │  ...fields...                            │
                    ├──────────────────────────────────────────┤
                    │ Footer (px-6 pb-6, border-t)             │
                    │                    [Huỷ] [Lưu]          │
                    └──────────────────────────────────────────┘
```

Width: `sm:max-w-xl` (mặc định) hoặc `sm:max-w-2xl` nếu cần 2 cột field.
**Bắt buộc** `px-6 pb-6` để nội dung không sát mép.

---

## 6. Token

| Property | Token | Giá trị | Lý do |
|----------|-------|---------|-------|
| Form max-width (fullpage) | `--form-max-w` | `max-w-5xl` (1024px) | Đọc thoải mái trên laptop 1366px+ |
| Content padding desktop | — | `px-8 py-8` | Breathing room |
| Content padding tablet | — | `px-4 py-6` | Tablet 768px |
| Section card gap | — | `space-y-6` | Phân tách rõ nhóm field |
| Field gap dọc trong section | — | `space-y-4` | Mật độ vừa phải |
| Field grid 2 cột | — | `grid grid-cols-2 gap-4` | Tối ưu form ngang |
| Sidebar width | — | `w-52` (208px) | Đủ cho label tab |
| Dialog max-w mặc định | — | `sm:max-w-md` | 480px — vừa màn tablet portrait |
| Dialog max-w role/checkbox | — | `sm:max-w-lg` | 512px — tránh bị chật checkbox 2 cột |
| Sheet max-w thường | — | `sm:max-w-xl` | 576px |
| Sheet max-w rộng | — | `sm:max-w-2xl` | 672px — khi cần xem dữ liệu kèm |
| Header height | — | `h-14` | Khớp global topbar |
| Footer height | — | `h-14` | Đối xứng header |

---

## 7. Variant matrix — loại form

| Variant | Khi nào dùng | className gợi ý |
|---------|-------------|-----------------|
| `fullpage-tabbed` | >5 field, multi-section, sub-list | `PatientEditorLayout` pattern |
| `fullpage-single` | >5 field, 1 section duy nhất (vd tạo dịch vụ mới) | `max-w-3xl mx-auto p-8` |
| `dialog-simple` | ≤4 field, 1 bước | `sm:max-w-md` |
| `dialog-confirm` | 0 field, chỉ confirm text | `sm:max-w-sm` |
| `sheet-edit` | Edit inline, cần xem list phía sau | `sm:max-w-xl` |
| `sheet-wide` | Edit + preview cạnh nhau | `sm:max-w-2xl` |

---

## 8. State

| State | Biểu hiện |
|-------|----------|
| Default | Field bình thường, border `border-input` |
| Focus | `ring-2 ring-ring` (shadcn default) — focus ring rõ, không xóa |
| Filled valid | Không thay đổi border (tránh "xanh lá" gây noise) |
| Error | Border `border-destructive`, message `text-xs text-destructive` bên dưới |
| Disabled | `opacity-50 cursor-not-allowed` |
| Loading (submit) | Button: spinner + disabled, text "Đang lưu..." |
| Unsaved changes | Badge hoặc dot vàng trên tab lỗi |

---

## 9. Form cần convert — đề xuất ưu tiên

| Form | Hiện tại | Đề xuất | Lý do |
|------|----------|---------|-------|
| **Tạo lượt khám** (`CreateEncounterDialog`) | Dialog `max-w-md` | **Fullpage** `/encounters/new` | Có patient search autocomplete (dropdown nổi trong dialog gây overflow), textarea chief complaint, dự kiến thêm field phòng khám + bác sĩ |
| **Mời người dùng** (`InviteUserForm`) | Dialog | Dialog — giữ nguyên nhưng nâng `sm:max-w-lg` | 4 field + checkbox grid 2 cột đang hơi chật ở `max-w-md` |
| **BHYT Export Form** | Dialog `sm:max-w-md` | Giữ dialog — nhưng nếu thêm "preview items" bước 2 phải convert **multi-step fullpage** | Hiện 4 field vẫn phù hợp dialog |

---

## 10. Component reusable — đề xuất cho Nam

### `<FullPageFormShell>`
```tsx
// Props gợi ý (snippet spec — không phải production code)
interface FullPageFormShellProps {
  title: string
  subtitle?: string
  breadcrumb?: { label: string; href?: string }[]
  actions: React.ReactNode        // Cancel + Submit buttons
  sidebarNav?: { id: string; label: string; errorCount?: number }[]
  activeTab?: string
  onTabChange?: (id: string) => void
  footer?: React.ReactNode        // keyboard hints + duplicate actions
  children: React.ReactNode
}
// Layout: min-h-screen flex flex-col
// Header: sticky top-0 z-40 h-14 border-b backdrop-blur
// Body: flex flex-1 (sidebar w-52 | main max-w-5xl mx-auto px-8 py-8)
// Footer: sticky bottom-0 z-30 h-14 border-t backdrop-blur (hidden mobile)
```

### `<FormSection>`
```tsx
interface FormSectionProps {
  icon?: React.ElementType   // lucide icon
  title: string
  description?: string
  children: React.ReactNode
}
// className: rounded-xl border p-6 space-y-4 bg-card
// Title: text-base font-semibold flex items-center gap-2
// Description: text-sm text-muted-foreground mt-0.5 mb-4
```

### `<FormFieldGrid>`
```tsx
interface FormFieldGridProps {
  columns?: 1 | 2 | 3   // default 2
  children: React.ReactNode
}
// grid grid-cols-1 sm:grid-cols-{columns} gap-4
```

---

## 11. Microcopy

| Element | Tiếng Việt | Ghi chú |
|---------|-----------|---------|
| Nút lưu (create) | Tạo [đối tượng] | "Tạo bệnh nhân", "Tạo lượt khám" |
| Nút lưu (edit) | Lưu thay đổi | Không dùng "Cập nhật" hoặc "Save" |
| Nút huỷ | Huỷ | Không dùng "Cancel" hoặc "Đóng" ở form |
| Loading submit | Đang lưu... | Có dấu ba chấm |
| Unsaved warning | Bạn có thay đổi chưa lưu. Rời trang sẽ mất dữ liệu. Tiếp tục? | `window.confirm` hoặc Dialog xác nhận |
| Error chung | Có lỗi xảy ra, vui lòng thử lại. | Toast destructive |
| Field bắt buộc | Dấu `*` màu destructive sau label | Không dùng "(bắt buộc)" |
| Placeholder | Ngắn, ví dụ cụ thể | "Nguyễn Văn A", "0901234567", "2026-05" |

---

## 12. A11y checklist

- [ ] Contrast text/background >= 4.5:1 (WCAG AA) — kiểm tra `text-muted-foreground` trên `bg-card`
- [ ] Focus visible: không xóa `outline`, dùng `ring-2 ring-ring ring-offset-2`
- [ ] Touch target >= 44px: button, tab sidebar phải `min-h-[44px]` trên mobile
- [ ] `aria-label` cho icon-only button (Quay lại, Huỷ icon)
- [ ] `aria-invalid` + `aria-describedby` kết nối field lỗi với message
- [ ] Keyboard nav: Tab/Shift+Tab qua toàn form, Enter submit, Esc cancel
- [ ] Tab sidebar có `role="navigation"` + `aria-label`
- [ ] Dialog/Sheet có `aria-modal="true"`, focus trap khi mở

---

## 13. Hand-off cho frontend (Nam)

### File cần sửa / tạo mới

| File | Việc cần làm |
|------|-------------|
| `frontend/components/layout/FullPageFormShell.tsx` | **Tạo mới** — extract từ `PatientEditorLayout` logic header/sidebar/footer |
| `frontend/components/layout/FormSection.tsx` | **Tạo mới** — card wrapper cho section trong fullpage form |
| `frontend/components/layout/FormFieldGrid.tsx` | **Tạo mới** — grid helper |
| `frontend/app/(dashboard)/encounters/_components/EncountersPageClient.tsx` | Xóa `CreateEncounterDialog`, thêm nút "Tạo lượt khám" route đến `/encounters/new` |
| `frontend/app/(dashboard)/encounters/new/page.tsx` | **Tạo mới** — fullpage encounter create dùng `FullPageFormShell` |
| `frontend/components/domain/InviteUserForm.tsx` | Đổi Dialog wrapper ngoài sang `sm:max-w-lg` (sửa ở file gọi `InviteUserForm`, không sửa form component) |
| `frontend/app/(dashboard)/admin/users/page.tsx` | Nâng Dialog chứa `InviteUserForm` lên `sm:max-w-lg` |

### className quan trọng cần đồng nhất

```
// Fullpage body container
"max-w-5xl mx-auto px-4 lg:px-8 py-8 pb-32 lg:pb-8"

// Section card
"rounded-xl border bg-card p-6 space-y-4"

// Field grid 2 cột
"grid grid-cols-1 sm:grid-cols-2 gap-4"

// Header sticky
"sticky top-0 z-40 h-14 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60"

// Footer sticky desktop
"hidden lg:flex sticky bottom-0 z-30 h-14 border-t bg-background/95 backdrop-blur px-6 items-center justify-between"

// Dialog mặc định
"sm:max-w-md"

// Dialog có checkbox grid
"sm:max-w-lg"

// Sheet thường
"sm:max-w-xl px-6 pb-6"
```
