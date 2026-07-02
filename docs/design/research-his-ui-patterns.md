# Research — UI patterns cho HIS Pro-Diab

> Tác giả: Linh (UX/UI Designer) — 2026-05-26
> Phạm vi: tổng hợp pattern từ Epic, Oracle Health (Cerner), Athenahealth, Doctolib, NHS Design System, VNPT-HIS, Viettel-HIS, FPT.eHospital 2.0+, eClinic, Medisoft và khuyến nghị áp dụng cho Pro-Diab HIS (Next.js 15 + Tailwind + shadcn/ui, dark mode mặc định, dùng tablet/laptop tại quầy lễ tân và phòng khám).

## TL;DR (5 khuyến nghị chính)

- **Màu sắc:** dùng nền dark navy (#0B1220) + accent teal y tế (#0EA5A4) thay vì xanh dương Epic/NHS — giảm chói, fit dark mode mặc định; semantic colors theo 4 trạng thái BHYT/CLS/đơn thuốc (chờ / đang xử lý / hoàn tất / cảnh báo). Luôn đạt WCAG AA 4.5:1.
- **Font:** **Inter** cho UI + **JetBrains Mono** cho số (mã BHYT, liều thuốc, giá tiền). Bật `font-variant-numeric: tabular-nums` cho mọi cột số trong table. Body 14px, table 13px, KPI 28-32px.
- **Layout:** form 2-3 cột (label trên input), **sticky action bar dưới cùng**, không bao giờ ẩn nút Lưu phía cuối scroll; tab order tuyến tính trái→phải, không nhảy cột; phím tắt F2 (lưu), F4 (tìm BN), Ctrl+N (mới), Esc (đóng).
- **Hạn chế input tay:** ưu tiên **lookup BHYT/CCCD trả về full hồ sơ** > template ICD-10 yêu thích theo bác sĩ > copy đơn cũ 1 click > autocomplete ATC/ICD-10 server-side > OCR CCCD camera tablet > voice-to-text cho phần "diễn biến bệnh" > smart default (chẩn đoán hôm nay = chẩn đoán lần khám gần nhất nếu cùng đợt điều trị).
- **Report:** dashboard 12-col grid, top row là 4 KPI card (số to + sparkline + delta %), giữa là 2 chart chính (line doanh thu, bar lượt khám theo bác sĩ), cuối là 1 table top thuốc + 1 donut cơ cấu BHYT/viện phí; drill-down click vào card mở filter panel; export PDF/Excel ở góc phải mỗi widget.

---

## 1. Màu sắc

### Pattern khảo sát

| Hệ thống | Approach | Ghi chú |
|---|---|---|
| **Epic** | nền trắng kem (#F5F1E6), text đen, accent xanh dương + đỏ alert. Dense, rất nhiều màu status (15+) → người mới khó nhớ | Bị chê chói khi ca đêm |
| **Oracle Health (Cerner Millennium)** | nền xám trung tính, accent cam-đỏ. Voice-first 2025 → giảm chrome | Tốt cho ca dài nhưng "buồn tẻ" |
| **Doctolib** | trắng sạch + xanh navy (#1A4480), accent xanh lá cho confirmed | UX tươi, fit phòng khám tư |
| **NHS Design System** | xanh `#005eb8`, đỏ `#d5281b`, vàng focus `#ffeb3b`, xanh lá `#007f3b`. Tất cả AA+ | Chuẩn accessibility, ổn cho gov |
| **VNPT-HIS / Viettel-HIS** | xanh dương đậm + xám, dense table cũ kỹ phong cách Win Forms | Bị chê mỏi mắt, font nhỏ |
| **FPT.eHospital 2.0+** | xanh teal-cyan + trắng, modern hơn | Đang là xu hướng VN |

### Khuyến nghị cho Pro-Diab

Dùng **dual theme** (dark mặc định, light tuỳ chọn cho phòng đông ánh sáng). Pro-Diab phục vụ ca 8-12h → dark giảm mỏi mắt nhưng phải giữ contrast cao.

**Semantic theo nghiệp vụ HIS:**

| Trạng thái nghiệp vụ | Token | Hex (dark) | Khi nào dùng |
|---|---|---|---|
| Chờ tiếp đón / chờ khám | `--status-waiting` | `#F59E0B` (amber) | hàng đợi lễ tân |
| Đang khám / đang xử lý CLS | `--status-progress` | `#0EA5A4` (teal) | encounter active |
| Hoàn tất / đã thanh toán | `--status-done` | `#10B981` (emerald) | bill paid, đơn đã cấp |
| Cảnh báo (HSD thuốc, BHYT sắp hết hạn, dị ứng) | `--status-warning` | `#F97316` (orange) | banner phía trên hồ sơ |
| Nguy cấp / chống chỉ định / dị ứng | `--status-critical` | `#EF4444` (red 500) | tag dị ứng, conflict thuốc |
| Thông tin BHYT (badge thẻ) | `--status-insurance` | `#3B82F6` (blue 500) | badge "BHYT" cạnh tên BN |

**Cảnh báo:** không bao giờ dùng riêng màu để truyền tin (chuẩn WCAG 1.4.1) — phải kèm icon + label tiếng Việt. Ví dụ dị ứng = icon cảnh báo đỏ + chữ "Dị ứng Penicillin".

### Token đề xuất (CSS variable + Tailwind class)

Sửa file `frontend/app/globals.css` và `frontend/tailwind.config.ts`:

```css
/* globals.css — dark theme mặc định */
:root[data-theme="dark"] {
  --bg-base: #0B1220;          /* nền app */
  --bg-surface: #111827;       /* card, table row */
  --bg-elevated: #1F2937;      /* modal, popover */
  --border-subtle: #1F2937;
  --border-default: #374151;
  --text-primary: #F9FAFB;     /* contrast 16:1 trên bg-base */
  --text-secondary: #D1D5DB;   /* 9.8:1 */
  --text-muted: #9CA3AF;       /* 5.7:1 — đủ AA cho body */
  --accent-primary: #14B8A6;   /* teal — brand Pro-Diab */
  --accent-primary-hover: #0F9488;
  --focus-ring: #FDE047;       /* vàng NHS-style, contrast cao */

  --status-waiting: #F59E0B;
  --status-progress: #0EA5A4;
  --status-done: #10B981;
  --status-warning: #F97316;
  --status-critical: #EF4444;
  --status-insurance: #3B82F6;
}
```

Tailwind tokens: `bg-surface`, `text-primary`, `border-default`, `text-status-critical` … (extend trong `theme.extend.colors`).

**File FE cần tạo/sửa:**
- `frontend/app/globals.css` — thêm CSS variables ở trên
- `frontend/tailwind.config.ts` — map vào `colors`
- `frontend/components/ui/status-badge.tsx` — component mới chuẩn hoá 6 status

---

## 2. Font chữ

### Pattern khảo sát

- **Epic:** Verdana + Arial — legacy, kerning kém ở 12px.
- **Oracle Health:** Source Sans Pro — modern, đọc tốt.
- **NHS:** Frutiger (licensed) → web fallback Arial. Body 19px desktop (rất to).
- **Doctolib / Tebra:** Inter / system sans, body 14-15px.
- **HIS Việt Nam:** đa số Tahoma / Arial 12-13px → text tiếng Việt có dấu bị "rít" do hinting kém.

### Khuyến nghị cho Pro-Diab

- **Font UI:** `Inter` (variable, hỗ trợ dấu tiếng Việt đầy đủ, optical sizing, có `tabular-nums`). Self-host trong `frontend/public/fonts/` để tránh phụ thuộc Google Fonts (PDPL VN).
- **Font số/mono:** `JetBrains Mono` cho mã BHYT (`GD4010110000001`), số CCCD, mã đơn QR, log audit — chữ 0 có gạch chéo, dễ phân biệt O/0, I/l/1.
- **Fallback:** `system-ui, "Segoe UI", -apple-system, "Helvetica Neue", Arial, "Liberation Sans"`.

**Scale (rem base 16):**

| Token | Size | Use |
|---|---|---|
| `text-xs` | 12px / 16px | label phụ, badge |
| `text-sm` | 13px / 18px | **table dense** (encounter list, drug list) |
| `text-base` | 14px / 20px | body form, mặc định |
| `text-md` | 15px / 22px | label form quan trọng |
| `text-lg` | 18px / 26px | section title |
| `text-xl` | 22px / 28px | page title |
| `text-kpi` | 32px / 36px | số KPI dashboard |
| `text-kpi-lg` | 44px / 48px | số tổng doanh thu dashboard chính |

**Weight:**
- Table header / button: `600` (semibold)
- Body: `400` (regular)
- KPI number: `700` (bold) + `tabular-nums`
- Tên bệnh nhân, mã đơn: `500` (medium)

**Quy tắc:**
- Số liệu y tế (liều, cân nặng, nhiệt độ, HA) **luôn** `font-variant-numeric: tabular-nums slashed-zero`.
- Cảnh báo dị ứng / chống chỉ định in `font-weight: 600` + uppercase, không bao giờ < 14px.
- Tiếng Việt có dấu → `line-height` tối thiểu 1.45 (dấu mũ/ngã không bị cắt).

**File cần sửa:**
- `frontend/app/layout.tsx` — `next/font/local` load Inter + JetBrains Mono
- `frontend/tailwind.config.ts` — `fontFamily`, `fontSize` extend
- `frontend/app/globals.css` — `@layer base { table td, .num { font-variant-numeric: tabular-nums; } }`

---

## 3. Layout dễ input

### Pattern khảo sát

- **Epic SmartForm:** form 1 cột rộng, label bên trái → quét mắt chậm nhưng ít sai.
- **Athenahealth:** 2 cột, sticky save bar dưới, có "next field hint" → giảm 30% click.
- **Doctolib agenda:** modal nhỏ inline, chỉ 4-5 field/lần → ít dữ liệu nhưng nhanh.
- **VNPT/Viettel HIS:** form 3-4 cột chen chúc, button Save lẫn trong field — lễ tân hay bấm nhầm.
- **NHS Service Manual:** "one thing per page" cho công dân, **không hợp** cho clinician.

### Khuyến nghị cho Pro-Diab

**Nguyên tắc:** clinician không rời bàn phím. Mọi action chính có phím tắt + nút.

**Layout chuẩn cho 3 màn chính:**

#### 3.1 Tiếp đón (`frontend/app/(dashboard)/reception/page.tsx`)

```
┌─ Search bar (BHYT/CCCD/SĐT) ──────────[F3 focus]─┐
│  [____________________] [Quét QR] [OCR CCCD]    │
└──────────────────────────────────────────────────┘
┌─ Hồ sơ BN (auto-fill sau lookup) ─┬─ Lượt khám ──┐
│  Họ tên, ngày sinh, giới, ĐC, SĐT │  Phòng khám  │
│  BHYT số, hạn, nơi KCB, mức hưởng │  Lý do khám  │
│  Dị ứng (nổi bật đỏ)              │  STT in giấy │
└───────────────────────────────────┴──────────────┘
┌─ Sticky action bar ──────────────────────────────┐
│  [Esc Huỷ]  [Ctrl+S Lưu nháp]  [F2 Tiếp nhận →] │
└──────────────────────────────────────────────────┘
```

- Tab order: search → hồ sơ → lượt khám → F2.
- Search field auto-focus khi vào trang (`autoFocus` + `useEffect`).
- Lookup BHYT trả về → field disabled trừ SĐT/địa chỉ (cho phép cập nhật).

#### 3.2 Khám bệnh (`frontend/app/(dashboard)/encounters/[id]/page.tsx`)

3-pane layout:

```
┌───────────┬──────────────────────────┬──────────┐
│ Sidebar   │ Trung tâm: SOAP note     │ Right    │
│ - Hồ sơ   │ - Sinh hiệu (1 dòng)     │ - Lịch sử│
│ - Dị ứng  │ - Triệu chứng (textarea) │ - CLS    │
│ - BHYT    │ - Chẩn đoán ICD-10 chip  │ - Đơn cũ │
│ (sticky)  │ - Chỉ định CLS           │ (tabs)   │
│           │ - Kê đơn (table inline)  │          │
└───────────┴──────────────────────────┴──────────┘
[Sticky bottom: F2 Hoàn tất khám | F8 Kê đơn | F9 In]
```

#### 3.3 Kê đơn (`frontend/app/(dashboard)/prescriptions/new/page.tsx`)

Table inline có thể typeahead trực tiếp trong row (giống Excel):

| # | Thuốc (ATC autocomplete) | Liều | Cách dùng | SL | Đơn vị | Ghi chú |
|---|---|---|---|---|---|---|

- Enter trong cell cuối → thêm row mới.
- Ctrl+D → duplicate row.
- Ctrl+Shift+V → paste từ template đơn cũ.
- Validate inline: thuốc hết tồn → row chuyển sang `bg-status-warning/10`.

**Quy tắc chung:**
- Label luôn **trên** input (không bên trái — tiếng Việt dài, sai cân bằng).
- Required field: dấu `*` đỏ + `aria-required`.
- Inline validation, **không** modal lỗi.
- Sticky action bar: `position: sticky; bottom: 0; bg-surface; border-t`.
- Tablet (1024px) → form 2 cột; laptop (≥1440px) → 3 cột.

**File cần tạo:**
- `frontend/components/forms/sticky-action-bar.tsx`
- `frontend/components/forms/field-group.tsx` (label trên, error inline, hint)
- `frontend/hooks/use-keyboard-shortcuts.ts` (đăng ký F2/F3/F4/F8/F9 global)

---

## 4. Hạn chế input tay (automation patterns) — **viết kỹ nhất**

Đây là trục then chốt giảm thời gian khám từ ~8 phút xuống ~3-4 phút. Tham khảo nghiên cứu: physician dành 1/3 thời gian cho EHR; KLAS 2022 ghi nhận 70% user muốn vendor giảm click.

### 4.1 Lookup BHYT/CCCD → full hồ sơ (ưu tiên #1)

**Flow:**
1. Lễ tân quét QR thẻ BHYT (camera tablet hoặc đầu đọc USB) → parse 10 ký tự mã thẻ.
2. Gọi `POST /api/v1/bhyt/lookup` (BE wrap cổng VSS / dịch vụ tra cứu trung gian, cache Redis 24h).
3. Trả về JSON: họ tên, ngày sinh, giới, địa chỉ, nơi KCB ban đầu, mức hưởng, hạn thẻ, mã hộ gia đình.
4. FE auto-fill toàn form `ReceptionForm`, disable field BHYT (chỉ cho phép sửa SĐT/email).
5. Nếu trả về 404 → cho phép nhập tay + flag `bhyt_unverified=true`.

**API mới BE cần làm:**
- `POST /api/v1/bhyt/lookup` body `{ card_number, full_name?, dob? }` → standard envelope.
- `POST /api/v1/cccd/ocr` body `multipart image` → trả về CCCD parsed (dùng FPT.AI/VietOCR).
- Cache theo `tenant_id` để chia sẻ trong phòng khám (tránh tra trùng).

### 4.2 Autocomplete ICD-10 + ATC (drug code)

- Endpoint `GET /api/v1/icd10/search?q=&limit=10` server-side, index theo cả mã + tên VI + tên EN.
- Component `<DiagnosisCombobox>` (shadcn `Command` + `cmdk`):
  - Debounce 200ms.
  - Hiển thị: `E11.9 — Đái tháo đường type 2 không biến chứng`.
  - Chấp nhận gõ "DTD2" → fuzzy match.
  - Multi-select dạng chip có thể tag "chính"/"phụ".
- Tương tự `<DrugCombobox>` dùng ATC + tên thương mại + tên hoạt chất; hiển thị tồn kho + giá BHYT inline.

### 4.3 Template bệnh án / đơn thuốc

- Bác sĩ tạo template trong `account/templates`:
  - Template chẩn đoán: combo ICD-10 + lời dặn + xét nghiệm thường order.
  - Template đơn thuốc: list thuốc + liều + cách dùng cho bệnh thường gặp (THA, ĐTĐ, viêm họng…).
- Hotkey `/` trong textarea → mở picker template, chọn → expand inline.
- Bảng `diab_his_clinical_template` (tenant_id, doctor_id, type, name, content_json).

### 4.4 Copy đơn cũ (1 click)

- Trong panel "Lịch sử" bên phải encounter, mỗi lần khám cũ có nút **[Sao chép đơn]**.
- Click → load vào prescription editor, đánh dấu row màu vàng `bg-status-waiting/10` để bác sĩ review.
- Tự cảnh báo nếu thuốc đã ngừng sản xuất / hết tồn.

### 4.5 OCR CCCD bằng camera tablet

- Component `<CccdScanner>` dùng `getUserMedia` + canvas crop + gửi BE OCR.
- Một lần quét < 3s, autofill 9 field (họ tên, ngày sinh, giới, quê quán, thường trú, số CCCD, ngày cấp, nơi cấp).
- Nếu BN đã có hồ sơ (match theo CCCD) → merge thay vì tạo mới.

### 4.6 Voice-to-text cho "diễn biến bệnh"

- Tích hợp Web Speech API (Chrome) hoặc Azure Speech (tiếng Việt y khoa).
- Nút mic trong textarea SOAP. Bác sĩ đọc → text fill vào ô triệu chứng.
- Giai đoạn 2 (sau MVP): dùng Azure OpenAI GPT-4o trích xuất ICD-10 candidate từ diễn biến.

### 4.7 Smart defaults theo bác sĩ / khoa

- Mỗi user có `user_preferences` (lưu trong `diab_his_user_pref`):
  - `default_visit_duration` (mặc định 15p).
  - `default_lab_panel` (combo xét nghiệm hay order).
  - `favorite_drugs` (top 20 thuốc hay kê).
  - `last_diagnosis` (lần khám gần nhất với BN này → đề xuất nếu < 30 ngày).
- Form mới mở → prefill từ preference, bác sĩ chỉ confirm hoặc sửa.

### 4.8 Gợi ý AI (tùy chọn, sau MVP)

- Sau khi nhập triệu chứng + sinh hiệu → gọi `POST /api/v1/ai/suggest-diagnosis` (Azure OpenAI).
- Trả 3 candidate ICD-10 + confidence + lý do. Hiển thị card có thể accept/dismiss.
- **Bắt buộc** có disclaimer "Gợi ý AI — bác sĩ quyết định cuối cùng" + log mọi accept/dismiss vào audit.

### 4.9 Bảng tổng hợp patterns

| Pattern | Tiết kiệm | Ưu tiên | Phụ thuộc BE |
|---|---|---|---|
| Lookup BHYT | ~2 phút/BN | P0 | API lookup VSS |
| OCR CCCD | ~1 phút/BN mới | P0 | OCR service |
| Autocomplete ICD-10/ATC | ~30s/đơn | P0 | search API |
| Copy đơn cũ | ~2 phút/tái khám | P0 | có sẵn |
| Template bệnh án | ~3 phút/khám | P1 | CRUD template |
| Smart default | ~30s/khám | P1 | user_pref table |
| Voice-to-text | ~1 phút/khám | P2 | Azure Speech |
| AI gợi ý chẩn đoán | review-only | P3 | Azure OpenAI |

**File FE cần tạo:**
- `frontend/components/lookup/bhyt-lookup-input.tsx`
- `frontend/components/lookup/cccd-scanner.tsx`
- `frontend/components/combobox/icd10-combobox.tsx`
- `frontend/components/combobox/drug-combobox.tsx`
- `frontend/components/templates/template-picker.tsx`
- `frontend/hooks/use-voice-input.ts`

---

## 5. Report bắt mắt

### Pattern khảo sát

- **Epic Reporting Workbench:** table-heavy, ít chart. Khó scan.
- **Cerner HealtheAnalytics:** dashboard có drill-down, donut + KPI tile rất Power BI.
- **Tebra / Athenahealth:** card KPI + sparkline + line chart doanh thu — clean.
- **Bold BI / Power BI 2025:** Card visual mới có native sparkline, traffic-light conditional formatting (green/amber/red), khuyến nghị 4-6 KPI top row.
- **NHS Performance dashboards:** color-blind safe palette, RAG status mọi outcome.

### Khuyến nghị cho Pro-Diab

**Grid:** 12-col, gap 16px. Tablet 1024 → 6-col, KPI card xếp 2x2.

**Layout dashboard chủ (`frontend/app/(dashboard)/reports/page.tsx`):**

```
Row 1 (KPI cards, mỗi card 3-col):
[ Lượt khám ↑12%  ] [ Doanh thu ↑8% ] [ Đơn BHYT ↓3% ] [ Tồn cảnh báo 5 ]
  142 (sparkline)     32.4M (sparkline)  87 (sparkline)    icon đỏ

Row 2 (chart chính):
[ Line — Doanh thu 30 ngày (8-col) ] [ Donut — BHYT vs Viện phí (4-col) ]

Row 3:
[ Bar — Lượt khám theo bác sĩ (6-col) ] [ Bar — Top 10 thuốc (6-col) ]

Row 4:
[ Heatmap — giờ cao điểm trong tuần (12-col) ]

Row 5:
[ Table — Công nợ BHYT đối soát (12-col) — export Excel ]
```

**Chọn chart type:**

| Loại dữ liệu | Chart |
|---|---|
| Xu hướng theo thời gian (doanh thu, lượt khám) | Line + area fill |
| So sánh cá nhân (bác sĩ, thuốc) | Horizontal bar |
| Cơ cấu (BHYT/Viện phí, nguồn thu) | Donut (tối đa 5 lát) |
| Mật độ giờ x thứ | Heatmap |
| Funnel (tiếp đón → khám → thanh toán) | Funnel chart |
| Phân phối tuổi BN | Histogram |
| KPI vs target | Bullet chart |

**Tránh:** pie chart > 5 lát, 3D chart, dual-axis (gây hiểu nhầm).

**Màu chart (dark mode, color-blind safe — Okabe-Ito inspired):**

```css
--chart-1: #14B8A6;  /* teal — doanh thu/chính */
--chart-2: #3B82F6;  /* blue — BHYT */
--chart-3: #F59E0B;  /* amber — viện phí */
--chart-4: #A78BFA;  /* violet — phụ */
--chart-5: #F472B6;  /* pink — phụ */
--chart-6: #94A3B8;  /* slate — baseline */
```

Với chart tài chính: dương = `#10B981`, âm = `#EF4444` (chuẩn quốc tế, không đảo).

**KPI Card pattern:**

```
┌──────────────────────────┐
│ Lượt khám hôm nay        │  ← text-sm text-muted
│ 142                      │  ← text-kpi font-bold tabular-nums
│ ↑ 12% so với hôm qua     │  ← text-xs text-status-done
│ ▁▂▃▅▆▇ (sparkline 7 ngày)│  ← chart-1
└──────────────────────────┘
[click → drill-down filter trang Encounters]
```

**Tương tác:**
- Hover chart → tooltip dày dạn (ngày, giá trị, % thay đổi).
- Click KPI card → mở trang chi tiết với pre-filter.
- Export: nút "..." góc phải mỗi widget → PNG / Excel / PDF / Copy CSV.
- Filter dải ngày toàn page sticky top: 7 ngày / 30 ngày / Quý / Năm / Tuỳ chỉnh.

**File FE cần tạo:**
- `frontend/app/(dashboard)/reports/page.tsx` (refactor)
- `frontend/components/reports/kpi-card.tsx`
- `frontend/components/reports/chart-wrapper.tsx` (Recharts + Tremor wrapper, dark theme)
- `frontend/components/reports/date-range-picker.tsx` (sticky top)
- `frontend/lib/chart-theme.ts` (palette + Recharts defaults)

**API BE cần (nhiều endpoint nhỏ, cache Redis 5p):**
- `GET /api/v1/reports/kpi?range=30d`
- `GET /api/v1/reports/revenue-trend?range=30d&group=day`
- `GET /api/v1/reports/top-drugs?limit=10&range=30d`
- `GET /api/v1/reports/doctor-performance?range=30d`
- `GET /api/v1/reports/peak-hours?range=30d` (heatmap matrix)

---

## Reference

- [NHS Digital Service Manual — Colour](https://service-manual.nhs.uk/design-system/styles/colour)
- [NHS Design System](https://service-manual.nhs.uk/design-system)
- [Making the NHS design system fit for the future (2025)](https://digital.nhs.uk/blog/design-matters/2025/making-the-nhs-design-system-fit-for-the-future)
- [Epic vs Oracle Health 2025-2035 forecast](https://healthcarereimagined.net/2025/09/14/the-future-of-ehr-oracle-health-vs-epic-systems-a-10-year-forecast-2025-2035/)
- [Epic vs Cerner: Technical AI Comparison](https://intuitionlabs.ai/articles/epic-vs-cerner-ai-comparison)
- [Cerner vs Epic 2025 Guide — Surety Systems](https://www.suretysystems.com/insights/cerner-vs-epic-comparison-surety-systems/)
- [EHR Usability Optimization — Reduce Clicks (Thinkitive)](https://www.thinkitive.com/blog/ehr-usability-optimization-for-reducing-clicks-and-burnout/)
- [The Hidden Cost of Clicks — Aeon Health](https://www.aeon.health/blog/hidden-cost-of-clicks-emr-usability)
- [EMR Usability Tips — EMRSystems](https://www.emrsystems.net/blog/tips-to-improve-ehr-usability/)
- [EMR Usability and Patient Safety — NCBI PMC](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC12081653/)
- [Key EHR Usability Challenges — Building Better Healthcare](https://buildingbetterhealthcare.com/key-ehr-usability-challenges-and-the-solutions-204242)
- [Clinical Table Search Service — ICD-10-CM API (NLM)](https://clinicaltables.nlm.nih.gov/apidoc/icd10cm/v3/doc.html)
- [Top 10 HIS Việt Nam — FPT IS](https://fpt-is.com/goc-nhin-so/phan-mem-quan-ly-benh-vien/)
- [FPT.eHospital 2.0+](https://fpt-is.com/ehospital-2-0/)
- [VNPT-HIS](https://vnpt.vn/doanh-nghiep/giai-phap-cntt/dich-vu-phan-mem-quan-ly-benh-vien-vnpt-his/)
- [Tra cứu BHYT — BHXH Việt Nam](https://baohiemxahoi.gov.vn/tracuu/Pages/tra-cuu-thoi-han-su-dung-the-bhyt.aspx)
- [VssID — Google Play](https://play.google.com/store/apps/details?id=com.bhxhapp)
- [Healthcare Dashboards 2025 — DashboardBuilder](https://dashboardbuilder.net/use-case/healthcare-dashboards)
- [Top 12 Healthcare Dashboard Examples — Bold BI](https://www.boldbi.com/dashboard-examples/healthcare/)
- [Power BI KPI Visuals & Dashboard Cards 2026](https://www.epcgroup.net/power-bi-kpi-visuals-dashboard-guide-2026)
- [13 Healthcare Dashboards and KPIs — NetSuite](https://www.netsuite.com/portal/resource/articles/erp/healthcare-dashboards.shtml)
- [IBM Plex Sans vs Inter — FontFYI](https://fontfyi.com/blog/ibm-plex-vs-inter/)
- [Best Fonts for Dense Dashboards — FontAlternatives](https://fontalternatives.com/blog/best-fonts-dense-dashboards/)
