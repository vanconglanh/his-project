# PrintableReportA4 — Spec in báo cáo khổ A4 dọc

## Mục tiêu UX
Người dùng (kế toán, bác sĩ trưởng, dược sĩ) cần xem trước bố cục đúng chuẩn nhận diện dIaB trước khi in hoặc tải PDF — tránh in sai trang, thiếu chữ ký, lệch bảng.

---

## Wireframe (A4 dọc, 210x297mm, margin 15mm)

```
┌─────────────────────────────────────────────────────┐  ← 210mm
│ ┌─────────────────────────────────────────────────┐ │
│ │  [LOGO tròn]  TÊN TRUNG TÂM / CÔNG TY (trắng)  │ │  Header ~28mm
│ │  bg:#0F766E   Địa chỉ · ☎ SĐT · Web · Email    │ │
│ └─────────────────────────────────────────────────┘ │
│                                                     │
│        BÁO CÁO DOANH THU  ← in hoa, đậm 14pt       │  Title ~18mm
│   ┌────────────────────────────────────────────┐    │
│   │ ||||| CODE128 BARCODE ||||| RPT-FIN-xxx    │    │
│   └────────────────────────────────────────────┘    │
│                                                     │
│  Kỳ: 01/05 – 25/05/2026   Người xuất: Nguyễn A     │  Meta ~10mm
│  Ngày xuất: 26/05/2026     Phòng khám: dIaB HCM    │
│  ─────────────────────────────────────────────────  │
│  ┌───┬────────────────────────┬──────┬───────────┐  │
│  │STT│ Nội dung               │ SL   │ Thành tiền│  │  bg teal-50
│  ├───┼────────────────────────┼──────┼───────────┤  │
│  │ 1 │ ...                    │  1   │ 250.000   │  │  row py-1.5
│  │ 2 │ ...                    │  1   │ 150.000   │  │
│  ├───┴────────────────────────┴──────┴───────────┤  │
│  │                              Tổng: 400.000 ₫  │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│              Ngày 26 tháng 05 năm 2026              │  Footer ~28mm
│                  NGƯỜI LẬP BÁO CÁO                  │
│              (Ký, ghi rõ họ tên)                    │
│                                             Trang 1/1│
└─────────────────────────────────────────────────────┘
```

---

## Token

| Property | Token | Giá trị | Lý do |
|---|---|---|---|
| Header background | `teal-700` | `#0F766E` | Nhận diện dIaB, contrast trắng/teal 4.6:1 đạt AA |
| Header text | `white` | `#FFFFFF` | |
| Table header bg | `teal-50` | `#F0FDFA` | Phân biệt row header, không chói |
| Table header text | `gray-900` | `#111827` | Contrast 14:1 trên teal-50 — đạt AAA |
| Table border | `gray-300` | `#D1D5DB` | Mảnh, không lấn át nội dung |
| Body text | `gray-800` | `#1F2937` | |
| Meta/muted | `gray-500` | `#6B7280` | Ngày xuất, người xuất |
| Font body | Inter, Arial fallback | 11pt | Đảm bảo min 10pt print |
| Font title | Inter Bold | 14pt | Tiêu đề báo cáo in hoa |
| Font clinic name | Inter Bold | 16pt | Tên trung tâm trong header |
| Font address header | Inter | 9pt | Dòng địa chỉ nhỏ |
| Font tabular | `font-mono` | 11pt | Số tiền, mã đơn, HSD |
| Page margin | CSS `@page` | `15mm` | |
| Section gap | `mb-6` | ~8mm | Giữa các khối |

---

## Variant matrix — cột bảng theo loại

| Variant | Tiêu đề in hoa | Barcode prefix | Cột bảng | Tỉ lệ cột | Summary row |
|---|---|---|---|---|---|
| Financial | `BÁO CÁO DOANH THU` | `RPT-FIN-` | STT / Số HĐ / Bệnh nhân / Dịch vụ / Thành tiền | 5/15/25/35/20% | Tổng cộng (VND, font-mono, right) |
| Clinical | `BÁO CÁO LƯỢT KHÁM` | `RPT-CLN-` | STT / Bệnh nhân / Bác sĩ / ICD-10 / Ngày khám | 5/25/20/20/15% (cộng 85%) | Tổng lượt |
| Pharmacy | `BÁO CÁO TỒN KHO` | `RPT-PHA-` | Mã thuốc / Tên thuốc / Lô / HSD / Tồn / Đơn vị | 12/30/12/14/10/12% | Không có |

---

## State

| State | Mô tả | Xử lý |
|---|---|---|
| Loading | Skeleton 3 row, barcode ẩn | `<Skeleton className="h-4 w-full" />` |
| Empty | Icon `FileSearch` 48px, text bên dưới | Ẩn nút In |
| Error API | Toast destructive | Nút "Thử lại" reload |
| Print ready | Toolbar `no-print` trên cùng (nút In + Tải PDF) | Ẩn hoàn toàn khi `@media print` |

---

## Print CSS snippet

```css
/* frontend/app/globals.css */
@page {
  size: A4 portrait;
  margin: 15mm;
}

@media print {
  body { background: white; color: black; }
  .no-print { display: none !important; }
  tr { break-inside: avoid; page-break-inside: avoid; }
  thead { display: table-header-group; }
  nav, aside, [data-sidebar], [data-topbar] { display: none !important; }
}
```

---

## Microcopy

| Element | Tiếng Việt | Ghi chú |
|---|---|---|
| Nút mở preview | Xem trước & In | Icon `Printer`, `variant="outline"` |
| Nút in | In báo cáo | `window.print()` |
| Nút tải PDF | Tải PDF | `GET /api/v1/reports/{type}/pdf` |
| Footer label | NGƯỜI LẬP BÁO CÁO | In hoa, căn giữa |
| Footer sub | (Ký, ghi rõ họ tên) | 9pt, italic |
| Tổng cộng (Financial) | Tổng cộng: | Right-align, font-mono, đậm |
| Empty title | Không có dữ liệu | |
| Empty sub | Không tìm thấy dữ liệu trong khoảng thời gian đã chọn. | |
| Error toast | Không tải được dữ liệu báo cáo. Vui lòng thử lại. | `variant="destructive"` |

---

## A11y checklist

- Contrast header (trắng / #0F766E): 4.6:1 — đạt WCAG AA
- Contrast table header text (gray-900 / teal-50): 14:1 — đạt AAA
- Font body tối thiểu 11pt (~14.7px) — vượt min 10pt print
- Focus ring trên nút "In" và "Tải PDF" ở chế độ preview (không áp dụng khi print)
- Touch target nút ≥ 44px trên màn hình preview
- `aria-label="Mã báo cáo: RPT-FIN-xxx"` trên vùng barcode
- `role="table"` + `scope="col"` trên mọi `<th>`
- `<title>` trang: `In báo cáo doanh thu — dIaB HIS`

---

## Hand-off cho frontend

**className snippet tham khảo:**
```
// Table header cell
"bg-teal-50 text-gray-900 font-semibold text-[11pt] py-2 px-3 border border-gray-300"

// Table data cell
"py-1.5 px-3 text-[11pt] border border-gray-300"

// Số tiền, mã
"font-mono text-right"

// Tiêu đề báo cáo
"text-center text-[14pt] font-bold uppercase tracking-wide mt-6 mb-4"
```
