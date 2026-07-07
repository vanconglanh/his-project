# Bộ tiêu chuẩn Design System — Pro-Diab HIS

> **Nguồn chân lý DUY NHẤT** cho giao diện Pro-Diab HIS. Mọi màn hình, component, report mới hoặc sửa đều phải bám tài liệu này. Khi các spec khác (`research-his-ui-patterns.md`, `input-form-layout-spec.md`, `report-print-a4.md`) mâu thuẫn → tài liệu này thắng.
>
> Tác giả: Linh (designer) · Cập nhật: 2026-07-07 · Phiên bản: 1.1 (R12: làm rõ Section title/Card title + bảng tint KPI báo cáo → token)
> Đối tượng thực thi: Nam (frontend), Thảo (backend/report) · Gác cổng: Chi (qc)

---

## Nguyên tắc nền tảng (5 quy tắc bất biến)

1. **Token-first**: KHÔNG hardcode màu (`#0EA5A4`, `bg-green-100`, `rgb(...)`) trong component. Mọi màu đi qua CSS variable/token định nghĩa tại `frontend/app/globals.css`.
2. **Một hệ token duy nhất** cho cả light + dark; đổi mode chỉ đổi giá trị biến, không đổi tên biến, không định nghĩa trùng.
3. **Không truyền tin chỉ bằng màu** (WCAG 1.4.1): trạng thái luôn kèm icon hoặc chữ.
4. **Mật độ cao, ít click**: người dùng y tế thao tác lặp lại nhiều giờ → ưu tiên tốc độ, phím tắt, giảm nhập tay.
5. **Kiểm chứng cả 2 mode + cả tablet**: mọi thay đổi UI phải xem thực tế ở light, dark, và màn ngang tablet.

---

## 1. Màu & Token

**Nền tảng công nghệ:** Tailwind v4 (CSS-first, không có `tailwind.config`), token khai báo trong `@theme inline` + các block theme của `frontend/app/globals.css`. Theme chuyển bằng `next-themes` với `attribute="class"` → chỉ toggle class `.dark`. **Vì vậy mọi token phải định nghĩa trên `:root` (light) và `.dark` (dark) — KHÔNG dùng selector `[data-theme="..."]`** (selector này không bao giờ được set → token chết ở light mode).

### 1.1. Bảng token chuẩn (nguồn chân lý)

| Nhóm | Token (biến) | Vai trò | Light | Dark |
|------|--------------|---------|-------|------|
| **Accent** | `--accent-primary` | Action chính, link active | `#0F9488` | `#14B8A6` |
| | `--accent-primary-hover` | Hover action chính | `#0D7A70` | `#0F9488` |
| | `--focus-ring` | Viền focus bàn phím | `#CA8A04` | `#FDE047` |
| **Status** (nghiệp vụ) | `--status-waiting` | Chờ | `#D97706` | `#F59E0B` |
| | `--status-progress` | Đang xử lý | `#0D9488` | `#0EA5A4` |
| | `--status-done` | Hoàn tất / đã thanh toán | `#059669` | `#10B981` |
| | `--status-warning` | Cảnh báo (sắp hết hạn/HSD) | `#EA580C` | `#F97316` |
| | `--status-critical` | Nguy cấp / quá hạn / xóa | `#DC2626` | `#EF4444` |
| | `--status-insurance` | BHYT | `#2563EB` | `#3B82F6` |
| **Surface** | `--bg-base` | Nền trang | `#F8FAFC` | `#0B1220` |
| | `--bg-surface` | Nền card/panel | `#FFFFFF` | `#111827` |
| | `--bg-elevated` | Nền popover/dropdown | `#F1F5F9` | `#1F2937` |
| **Border** | `--border-subtle` | Đường kẻ nhạt | `#E2E8F0` | `#1F2937` |
| | `--border-default` | Viền mặc định | `#CBD5E1` | `#374151` |
| **Text** | `--text-primary` | Chữ chính | `#0F172A` | `#F9FAFB` |
| | `--text-secondary` | Chữ phụ | `#334155` | `#D1D5DB` |
| | `--text-muted` | Metadata / placeholder | `#64748B` | `#9CA3AF` |
| **Chart** | `--chart-1` | Series 1 (teal) | `#0D9488` | `#14B8A6` |
| | `--chart-2` | Series 2 (xanh dương) | `#2563EB` | `#3B82F6` |
| | `--chart-3` | Series 3 (cam) | `#D97706` | `#F59E0B` |
| | `--chart-4` | Series 4 (tím) | `#7C3AED` | `#A78BFA` |
| | `--chart-5` | Series 5 (hồng) | `#DB2777` | `#F472B6` |
| | `--chart-6` | Series 6 (xám) | `#64748B` | `#94A3B8` |

> **Chart palette là color-blind safe** — thứ tự dùng bắt buộc theo số (1→6), không tự chọn màu khác.

### 1.2. Quy tắc token
- **Cấm hardcode màu** trong `.tsx`/`.css` component. Dùng: `bg-[color:var(--bg-surface)]`, `text-[color:var(--text-primary)]`, hoặc utility shadcn (`bg-card`, `text-muted-foreground`, `bg-primary`) đã map sẵn.
- Badge/nền mờ trạng thái: `bg-[color:var(--status-x)]/10 text-[color:var(--status-x)] border-[color:var(--status-x)]/30`.
- **Một nguồn chân lý cho `primary` và `chart-*`**: hệ shadcn (`--primary`, `--chart-1..5` OKLCH) và hệ HIS (`--accent-primary`, `--chart-1..6` hex) KHÔNG được định nghĩa mâu thuẫn cùng lúc. Quy ước: `--chart-*` dùng bảng HIS ở trên; `--primary` shadcn phải khớp `--accent-primary`.
- Chart (Recharts) đọc màu qua `var(--chart-n)` (dùng `getComputedStyle` hoặc class Tailwind), KHÔNG hardcode mảng hex trong component.

---

## 2. Typography

| Thành phần | Token / class | Cỡ | Ghi chú |
|-----------|---------------|-----|---------|
| Font UI | `--font-sans` = Inter | — | subset `latin` + `vietnamese` |
| Font số liệu | `--font-mono` = JetBrains Mono | — | giá tiền, mã đơn, số thẻ BHYT |
| Page title | `text-xl` + `font-bold` | 1.375rem | 1 dòng |
| Section title | `text-lg` + `font-semibold` | 1.125rem | |
| Body mặc định | `text-base` | 0.875rem (14px) | mật độ cao |
| Phụ / metadata | `text-sm` | 0.8125rem | + `text-muted` |
| Ghi chú nhỏ | `text-xs` | 0.75rem | badge, caption |
| KPI card | `text-kpi` / `text-kpi-lg` | 2rem / 2.75rem | dashboard |

**Quy tắc:**
- Chỉ dùng thang `--text-*` đã định nghĩa; **cấm** `text-[13px]`, `text-[15px]` tùy tiện.
- Số liệu (bảng, tiền, KPI) bật `font-variant-numeric: tabular-nums slashed-zero` (đã set sẵn cho `table td`, `.tabular`, `.num`).
- Tiếng Việt có dấu: line-height tối thiểu 1.45 để không cụt dấu (thang đã đảm bảo).
- Không dùng `font-weight` > 700; heading tối đa `font-bold`.

**Làm rõ "Section title" vs "Card title" (hết mâu thuẫn khi audit):**
- **Section title của trang** (tiêu đề khu vực lớn, không nằm trong `Card`, ví dụ tiêu đề nhóm trên dashboard) → `text-lg font-semibold`.
- **Card title bên trong component `Card`/`CardHeader`** (shadcn `CardTitle`) → `text-sm font-semibold`. Đây là quy ước hợp lệ vì `Card` là đơn vị mật độ cao, lặp lại nhiều lần/màn (dashboard, report, danh sách); dùng `text-lg` cho từng `CardTitle` sẽ vỡ mật độ và không nhất quán với toàn app hiện đang dùng `text-sm` cho `CardTitle`.
- Quy tắc phân biệt nhanh: **có bao nhiêu Card trên màn quyết định cỡ chữ** — 1 tiêu đề cho cả trang/khu vực → `text-lg`; tiêu đề lặp lại trong từng Card → `text-sm`. Khi audit, KHÔNG flag `CardTitle: text-sm` là lỗi lệch thang chữ.

---

## 3. Spacing · Layout · Panel

| Vùng | Chuẩn |
|------|-------|
| Sidebar trái | 240px (expanded) / 64px (collapsed), sticky |
| Topbar | 56px — search global + notification + user |
| Content padding | `p-6` desktop · `p-4` tablet |
| Card / Panel | `rounded-lg` (radius gốc 10px), padding trong `p-4`~`p-6`, nền `bg-card`, viền `border-border` |
| Dialog (Modal) | `max-w-xl` form ngắn · `max-w-4xl` bảng phức tạp |
| Sheet (Drawer phải) | Mặc định `sm:max-w-xl` (detail read-only, form ≤8 field); form rộng 2 cột → `sm:max-w-2xl` (variant sheet-wide). **Luôn** `px-6 pb-6` (không sát mép). Không dùng width arbitrary `w-[###px]` |
| Grid dashboard/report | 12 cột, gap `gap-4`~`gap-6` |
| Radius | `--radius: 0.625rem` gốc; thang `sm/md/lg/xl/2xl` dẫn xuất — không đặt radius rời |

**Quy tắc:** khoảng cách chỉ dùng scale Tailwind (`gap-2/4/6`, `p-4/6`), không `p-[13px]`. Card cùng loại phải cùng padding trong một màn.

---

## 4. Component & Density

- **Density bảng**: dense `py-2` cho danh sách > 20 dòng; comfortable `py-3` cho dashboard.
- **Form field**: `gap-4` dọc, **label trên input** (không label-trái trừ filter bar).
- **Button**: primary (`bg-primary`) 1 hành động chính/màn; destructive (`--status-critical`) luôn kèm confirm dialog; touch target ≥ 44px.
- **StatusBadge nghiệp vụ** — dùng `HisStatusBadge` (`components/ui/status-badge.tsx`), LUÔN có icon + `aria-label`. 6 variant + màu token:

| Trạng thái | Variant | Token màu |
|-----------|---------|-----------|
| Chờ | `waiting` | `--status-waiting` |
| Đang xử lý | `progress` | `--status-progress` |
| Hoàn tất / Đã thanh toán | `done` | `--status-done` |
| Cảnh báo / Sắp hết hạn | `warning` | `--status-warning` |
| Nguy cấp / Quá hạn | `critical` | `--status-critical` |
| BHYT | `insurance` | `--status-insurance` |

> Ánh xạ nghiệp vụ: `WAITING→waiting`, `IN_PROGRESS→progress`, `DONE/PAID→done`, `PENDING→warning`, `OVERDUE/CANCELLED→critical`.
> **Chỉ một** component badge trạng thái cho toàn hệ thống — không tạo bản trùng dùng Tailwind palette cứng.

---

## 5. State (rỗng · tải · lỗi)

- **Empty**: icon outline 48px màu `text-muted`, tiêu đề 1 dòng + phụ đề 1 dòng tiếng Việt, CTA primary nếu có hành động. Dùng `components/ui/EmptyState.tsx`.
- **Loading**: skeleton cho card/table (`PageSkeleton`/`Skeleton`); spinner inline + disable cho button đang submit; loading toàn trang chỉ khi chuyển route.
- **Error**: inline dưới field cho validation; toast destructive cho lỗi mạng/API; full-page error có CTA "Thử lại" cho lỗi 5xx.

---

## 6. Report / In ấn A4 (logo + thông tin phòng khám)

**Mọi report, phiếu, hóa đơn, đơn thuốc in ra BẮT BUỘC có letterhead đầy đủ.** Nguồn dữ liệu: `GET /api/v1/tenants/me/letterhead` (bảng `diab_his_sys_tenants` + letterhead fields — migration `0065`).

### 6.1. Trường bắt buộc trong letterhead
| Trường | Nguồn cột | Bắt buộc |
|--------|-----------|----------|
| Logo | `logo_url` | ✔ (fallback: chữ cái đầu tên PK) |
| Tên phòng khám | `name` | ✔ |
| Tên pháp nhân | `company_name` | ✔ |
| **Mã CSKCB** | `cskcb_code` | ✔ *(đã bổ sung 2026-07-02 — finding F3)* |
| Địa chỉ | `address` | ✔ |
| Điện thoại | `phone` | ✔ |
| Email | `email` / `email_support` | ✔ |
| Mã số thuế | `tax_code` | Khuyến nghị (hóa đơn) |

### 6.2. Layout in
- Khổ **A4 dọc**, margin 15mm (`@page`), footer "Trang X / Y" góc phải.
- Header nền teal `#0F766E` (token in `--print-header`), chữ trắng; logo trái 64px, thông tin phải.
- Barcode/QR mã báo cáo (prefix `RPT-FIN/CLN/PHA-…`) hoặc mã đơn.
- Footer: nơi ký, ngày in, người in.
- **Đồng bộ 2 nơi render**: FE `frontend/components/print/**` (xem trên màn) và BE `QuestPdfReportExporter.cs` / `InvoicePdfService.cs` / `ReceiptPdfService.cs` (PDF thật) phải cùng bố cục + cùng trường letterhead.

### 6.3. "Đủ thông tin nhưng dễ nhìn"
- Cỡ chữ in tối thiểu **11pt** (bảng ≥ 10pt), số liệu căn phải + `tabular-nums`.
- Bảng phân nhóm rõ (header teal-50), zebra nhẹ, không quá 7 cột/bảng A4 dọc — cột nhiều thì xoay A4 ngang hoặc tách bảng.
- Tổng cộng/tiêu điểm in đậm; tránh nhồi chữ sát mép; `break-inside: avoid` cho mỗi dòng.

### 6.4. Bảng màu tint KPI báo cáo → token (chuẩn hoá 2026-07-07)

Backend Report Engine trả tint nền cho từng ô KPI dưới dạng hex cố định (5 màu). FE **không được** dùng hex thẳng qua inline `style` — phải map sang token theo bảng dưới. Nếu BE đổi sang trả `tint_token` (enum) thay vì hex, ánh xạ vẫn giữ nguyên ý nghĩa semantic này.

| Ý nghĩa semantic | Hex BE hiện trả | Token CSS | className FE gợi ý |
|---|---|---|---|
| **Brand** — KPI trung tính, mang tính thương hiệu (vd "Tổng số", "Tổng lượt") — KHÔNG phải tín hiệu hoàn tất/thành công | `#F0FDFA` (teal-50) | `--accent-primary` | `bg-[color:var(--accent-primary)]/10 text-[color:var(--accent-primary)]` |
| **Done** — đã hoàn tất / đã thanh toán / đạt | `#ECFDF5` | `--status-done` | `bg-[color:var(--status-done)]/10 text-[color:var(--status-done)]` |
| **Warning** — cảnh báo nhẹ, sắp hết hạn/HSD | `#FFFBEB` | `--status-warning` | `bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)]` |
| **Critical** — nguy cấp, quá hạn, âm/lỗi | `#FEF2F2` | `--status-critical` | `bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)]` |
| **Insurance** — số liệu liên quan BHYT | `#EFF6FF` | `--status-insurance` | `bg-[color:var(--status-insurance)]/10 text-[color:var(--status-insurance)]` |
| **Neutral** — KPI không có tín hiệu nghiệp vụ nào (fallback khi BE không trả tint) | — (không có hex cố định) | `--text-muted` | `bg-muted/40 text-[color:var(--text-muted)]` |

**Quyết định về `#F0FDFA` (teal-50):** map **`brand`** (`--accent-primary`), **KHÔNG** map `--status-done`. Lý do:
1. Về mặt màu sắc, `#F0FDFA` thuộc họ **teal** — cùng hue với `--accent-primary` (`#0F9488`/`#14B8A6`), không cùng họ **green** với `--status-done` (`#059669`/`#10B981`). Map đúng hue giữ được liên hệ thị giác nhất quán với brand color toàn hệ thống.
2. Về mặt nghiệp vụ, các KPI dùng tint này thường là **số đếm trung tính** (tổng số bệnh nhân, tổng lượt khám, tổng đơn...) — không mang nghĩa "đã hoàn tất/thành công". Nếu gộp chung với `--status-done` (đã dùng riêng cho `#ECFDF5`), 2 hex khác nhau từ BE sẽ trỏ về cùng 1 token → mất khả năng phân biệt "tổng trung tính" và "đã hoàn tất" khi BE cần đổi màu độc lập cho 2 ý nghĩa này.
3. Giữ 1-hex-1-token (không gộp) đúng nguyên tắc "Token-first" và giúp audit sau này dễ verify: mỗi hex Report Engine trả về có đúng 1 ý nghĩa semantic, không suy luận lại.

Checklist "báo cáo mới đủ chuẩn" (đầy đủ hơn) xem tại `docs/design/report-ds-remediation-p1.md` mục cuối file.

---

## 7. Màn hình nhập liệu (dễ thao tác)

**Phân loại form** (theo `input-form-layout-spec.md`):
| Loại | Khi nào | Chuẩn |
|------|---------|-------|
| **Fullpage** | > 5 field / nhiều section / có sub-list (bệnh nhân, khám, kê đơn) | `max-w-5xl`, chia `FormSection`, sticky action bar dưới |
| **Dialog** | ≤ 4 field đơn giản | `sm:max-w-md` |
| **Sheet** | ≤ 8 field, cần xem-trong-khi-sửa | `sm:max-w-xl`, luôn `px-6 pb-6` |

**Quy tắc thao tác nhanh:**
- Label trên input, `gap-4`; **tab order** theo thứ tự đọc; Enter chuyển field/submit hợp lý.
- **Phím tắt** thống nhất: F2 (thêm/tiếp đón), F3 (tìm), F4 (sửa), F8 (lưu), F9 (in) — hiển thị trong `ShortcutsModal`.
- **Giảm nhập tay** (ưu tiên P0→P1): lookup thẻ BHYT/CCCD tự điền hành chính; autocomplete ICD-10/ATC; smart defaults (ngày hôm nay, phòng đang trực); copy đơn cũ / template EMR.
- **Sticky action bar** (`sticky-action-bar.tsx`) cho form dài; nút chính bên phải.
- **Auto-save draft** cho form dài (khám, kê đơn) tránh mất dữ liệu.
- Validation: inline dưới field, tiếng Việt có dấu, hiện ngay khi blur.

---

## 8. Accessibility & Responsive

- **Contrast** ≥ 4.5:1 cho text thường, ≥ 3:1 cho text lớn/icon (kiểm cả light + dark).
- **Focus ring** luôn hiện khi tab (`--focus-ring`), không `outline:none` trần.
- **Touch target** ≥ 44×44px (môi trường đeo găng).
- **Không truyền tin chỉ bằng màu**: trạng thái kèm icon/chữ; biểu đồ kèm nhãn/pattern.
- **`forced-colors`**: giữ block high-contrast trong `globals.css`.
- **Responsive**: thiết kế ưu tiên tablet ngang; breakpoint `md`/`lg`; sidebar collapse; bảng cuộn ngang trong `overflow-x-auto`, không vỡ layout.
- Ảnh/logo `max-w-full`; không để body cuộn ngang.

---

## Checklist nghiệm thu (dán vào mỗi PR UI)
- [ ] Không hardcode màu — chỉ dùng token/utility.
- [ ] Cỡ chữ theo thang `--text-*`; số liệu `tabular-nums`.
- [ ] Padding card/panel + max-width Dialog/Sheet đúng chuẩn mục 3.
- [ ] Trạng thái dùng `HisStatusBadge` (icon + aria-label).
- [ ] Empty/Loading/Error đủ 3 trạng thái.
- [ ] (Report) letterhead đủ 6 trường bắt buộc gồm **mã CSKCB**.
- [ ] (Report) tint KPI dùng đúng bảng mục 6.4 (brand/done/warning/critical/insurance/neutral) — không hex thẳng qua `style`.
- [ ] (Card) `CardTitle` dùng `text-sm font-semibold`; tiêu đề khu vực ngoài Card dùng `text-lg font-semibold` (mục 2).
- [ ] (Form) đúng phân loại Fullpage/Dialog/Sheet + phím tắt + tab order.
- [ ] Contrast ≥ 4.5:1, focus ring, touch target ≥ 44px — kiểm cả light + dark.
