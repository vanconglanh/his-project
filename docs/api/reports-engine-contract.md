# API Contract - Report Engine (config-driven)

> Tai lieu ky thuat noi bo cho FE implement man hinh bao cao generic. Tham chieu: docs/prd/reports-catalog-prd.md.
> Backend: backend/src/ProDiabHis.Api/Controllers/ReportsController.cs (route /api/v1/reports/*).
> Engine: ProDiabHis.Application.Reports.Engine.* (Application) + ProDiabHis.Infrastructure.Reports.* (Infrastructure).

Muc tieu: 1 bo 4 endpoint dung chung cho moi bao cao config-driven (Nhom A da co SQL that: A1-A6), thay vi
moi bao cao phai sua controller/exporter/code-gen rieng. FE chi can biet code cua bao cao (vd revenue-daily)
va implement 1 man hinh generic tai su dung cho toan bo danh muc.

Toan bo response dung envelope chuan cua du an:
```
{ "data": { }, "meta": { } }
```
Loi dung envelope chuan:
```
{ "error": { "code": "REPORT_NOT_FOUND", "message": "Khong tim thay bao cao xxx", "details": {} } }
```

Toan bo endpoint yeu cau JWT hop le (Authorize) + quyen report.read (doc) hoac report.export (xuat file).
Da tenant duoc enforce o tang application (ITenantProvider) - moi cau SQL ben trong descriptor bat buoc
co WHERE tenant_id = @tenantId.

---

## 1. GET /api/v1/reports/catalog

Tra ve danh muc toan bo bao cao da dang ky trong IReportRegistry (sap xep theo group roi group_order).
FE dung de build menu bao cao + danh sach filter cho tung bao cao.

Quyen: report.read

Query params: khong co.

Response 200:
```json
{
  "data": [
    {
      "code": "revenue-daily",
      "title": "BAO CAO DOANH THU NGAY",
      "group": "Financial",
      "group_order": 1,
      "icon": "banknote",
      "orientation": "Landscape",
      "group_by_key": "collectorName",
      "filters": [
        { "key": "collectorId", "label": "Nguoi thu", "type": "Select", "options_source": "collectors", "required": false },
        { "key": "counterId",   "label": "Quay thu",  "type": "Select", "options_source": "counters",   "required": false },
        { "key": "variance",    "label": "Tien chenh lech", "type": "Enum", "options_source": null, "required": false }
      ]
    },
    {
      "code": "refund-receipts",
      "title": "BAO CAO HOAN TRA PHIEU THU",
      "group": "Financial",
      "group_order": 2,
      "icon": "rotate-ccw",
      "orientation": "Portrait",
      "group_by_key": null,
      "filters": [
        { "key": "collectorId", "label": "Nguoi thu", "type": "Select", "options_source": "collectors", "required": false },
        { "key": "counterId",   "label": "Quay thu",  "type": "Select", "options_source": "counters",   "required": false }
      ]
    }
  ]
}
```

Ghi chu:
- orientation do backend tu suy theo so cot (>=11 cot => Landscape) - FE khong can tu tinh.
- filters khong bao gom fromDate/toDate - 2 field nay LUON bat buoc, co dinh cho moi bao cao (theo
  PRD muc 1.1), FE tu render 1 lan cho khung chung.
- Voi filters[].type = Enum ma options_source = null (vd variance), FE tu hardcode danh sach lua chon
  theo nghiep vu (xem PRD muc 5.3: ALL/DIFF/NODIFF).

---

## 2. GET /api/v1/reports/{code}/data

Lay du lieu luoi (grid) cua 1 bao cao theo code + khoang ngay + filter rieng.

Quyen: report.read

Path param: code - ma bao cao (vd revenue-daily, refund-receipts, void-receipts, advances,
fee-detail, lab-summary).

Query params:

| Ten | Kieu | Bat buoc | Ghi chu |
|-----|------|:---:|---------|
| from | date (yyyy-MM-dd) | co | |
| to   | date (yyyy-MM-dd) | co | to >= from, khoang cach <= 366 ngay |
| page | int | khong | mac dinh 1 |
| page_size | int | khong | mac dinh 100, toi da 5000 |
| filter rieng | string | khong | tuy theo catalog[].filters[].key (vd collectorId, counterId, variance) |

Vi du: GET /api/v1/reports/revenue-daily/data?from=2026-07-01&to=2026-07-07&counterId=xxxx&variance=DIFF

Response 200 - bao cao CO group (group_by_key khac null, vd revenue-daily):
```json
{
  "data": {
    "columns": [
      { "key": "date", "label": "Ngay", "type": "DateTime", "align": "Left", "width": 1.1, "is_group_subtotal": false },
      { "key": "receiptNo", "label": "So Phieu", "type": "Text", "align": "Left", "width": 0.9, "is_group_subtotal": false },
      { "key": "netCollected", "label": "Thuc thu", "type": "Money", "align": "Right", "width": 0.9, "is_group_subtotal": true }
    ],
    "groups": [
      {
        "key": "Nguyen Thi Ke Toan",
        "label": "Nguyen Thi Ke Toan",
        "count": 14,
        "rows": [
          {
            "date": "2026-07-01T08:12:00",
            "receiptNo": "HD-202607-0001",
            "patientCode": "BN00001",
            "patientName": "Tran Van A",
            "description": "Thu phi dich vu (Kham Benh: 350000)",
            "totalCost": 350000,
            "discount": 0,
            "surcharge": 0,
            "refund": 0,
            "advance": 0,
            "cash": 350000,
            "bankTransfer": 0,
            "card": 0,
            "netCollected": 350000,
            "patientSource": "Vang lai"
          }
        ],
        "subtotals": {
          "totalCost": 4895000, "discount": 350000, "surcharge": 0, "refund": 0, "advance": 0,
          "cash": 2795000, "bankTransfer": 1750000, "card": 0, "netCollected": 4545000
        }
      }
    ],
    "rows": null,
    "totals": {
      "totalCost": 4895000, "discount": 350000, "surcharge": 0, "refund": 0, "advance": 0,
      "cash": 2795000, "bankTransfer": 1750000, "card": 0, "netCollected": 4545000
    },
    "kpis": [
      { "label": "TONG THUC THU", "tint": "#F0FDFA", "value": 4545000, "is_money": true },
      { "label": "SO PHIEU THU", "tint": "#FFFBEB", "value": 14, "is_money": false },
      { "label": "TB / PHIEU", "tint": "#F0FDFA", "value": 324642.86, "is_money": true }
    ]
  },
  "meta": { "page": 1, "page_size": 100, "total": 14 }
}
```

Response 200 - bao cao KHONG group (group_by_key = null, vd refund-receipts, fee-detail):
```json
{
  "data": {
    "columns": [ "... giong cau truc tren ..." ],
    "groups": null,
    "rows": [
      { "date": "2026-07-02T10:00:00", "receiptNo": "HD-202607-0002", "patientCode": "BN00002",
        "patientName": "Le Thi B", "reason": "Nop nham phieu", "refundAmount": 150000,
        "performedBy": "Nguyen Thi Ke Toan" }
    ],
    "totals": { "refundAmount": 150000 },
    "kpis": [
      { "label": "TONG TIEN HOAN", "tint": "#FEF2F2", "value": 150000, "is_money": true },
      { "label": "SO PHIEU HOAN", "tint": "#FFFBEB", "value": 1, "is_money": false }
    ]
  },
  "meta": { "page": 1, "page_size": 100, "total": 1 }
}
```

Quy uoc quan trong cho FE:
- Khi groups != null -> render luoi co group-header + dong Cong nhom (dung group.subtotals), dong
  TONG CONG cuoi bang lay tu data.totals. page/page_size KHONG ap dung phan trang trong che do group
  (toan bo du lieu trong ky duoc tra ve 1 lan, gioi han an toan o tang SQL - xem muc 6). meta.total = tong so
  dong du lieu goc (khong phai so nhom).
- Khi groups == null -> dung data.rows (da phan trang theo page/page_size), dong TONG CONG (neu co
  cot is_group_subtotal = true) tinh tren toan bo tap ket qua (khong chi trang hien tai) - lay tu data.totals.
- column.type: Text hoac Money hoac Number hoac Date hoac DateTime hoac Enum - dung de format hien thi
  (Money: dinh dang co dau phay ngan cach hang nghin, Date: dd/MM/yyyy, DateTime: dd/MM/yyyy HH:mm).
- column.align: Left hoac Right hoac Center.
- Gia tri null trong rows/groups[].rows hien thi dau gach ngang.

Loi:
- 400 REPORT_NOT_FOUND - code khong ton tai trong registry.
- 400 REPORT_INVALID_DATE_RANGE - from > to hoac khoang cach > 366 ngay.

---

## 3. GET /api/v1/reports/{code}/export

Xuat bao cao ra file PDF (A4, tu chon Portrait/Landscape theo catalog[].orientation) hoac Excel (.xlsx).

Quyen: report.export

Path param: code - nhu muc 2.

Query params: giong muc 2 (from, to, filter rieng) + format (pdf mac dinh hoac excel).
Khong dung page/page_size - export luon lay TOAN BO du lieu trong ky (gioi han an toan o SQL, muc 6).

Vi du:
```
GET /api/v1/reports/revenue-daily/export?from=2026-07-01&to=2026-07-07&format=pdf
GET /api/v1/reports/revenue-daily/export?from=2026-07-01&to=2026-07-07&format=excel
```

Response 200: file binary.
- format=pdf -> Content-Type: application/pdf, ten file la ma-bao-cao ket hop reportCode, vi du
  revenue-daily-RPT-RVD-20260707-0001.pdf. PDF dung khung chuan diaB: letterhead mau xanh dIaB,
  barcode CODE128 ma bao cao, KPI cards, bang co group-header/Cong nhom/TONG CONG, chu ky + so trang.
- format=excel -> Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,
  ten file dang code-from-to.xlsx. Freeze 2 hang dau + cot dau, co dong Cong nhom/TONG CONG.

Loi: giong muc 2.

---

## 4. GET /api/v1/reports/options/{source}

Lay danh sach lua chon cho filter dropdown (dung options_source tra ve tu muc 1).

Quyen: report.read

Path param: source - 1 trong: collectors (nguoi thu: role Ke toan/Le tan/Admin), counters (quay thu -
diab_his_bil_counters), doctors (bac si - role bac_si), clinics (tenant hien tai), patients
(benh nhan, toi da 200 ket qua gan nhat).

Response 200:
```json
{
  "data": [
    { "value": "b6f1c6b0-0000-0000-0000-000000000001", "label": "Nguyen Thi Ke Toan" },
    { "value": "6f2e4a10-0000-0000-0000-000000000002", "label": "Tran Van Le Tan" }
  ]
}
```

Loi: 400 REPORT_INVALID_OPTIONS_SOURCE - source khong hop le.

---

## 5. Danh sach bao cao Nhom A da dang ky (dot nay)

| code | title | group_by_key | orientation | Trang thai schema |
|------|-------|---------------|-------------|-------------------|
| revenue-daily (A1) | BC Doanh Thu Ngay | collectorName | Landscape (15 cot) | SQL that day du |
| refund-receipts (A2) | BC Hoan Tra Phieu Thu | khong co | Portrait | SQL that, 2 cot xap xi (xem muc 6) |
| void-receipts (A3) | BC Huy Phieu Thu | khong co | Portrait | SQL that, 1 cot xap xi (xem muc 6) |
| advances (A4) | BC Tam Ung | khong co | Portrait | Chua co bang du lieu - luon tra rong |
| fee-detail (A5) | BC Chi Tiet Vien Phi | khong co | Portrait | SQL that day du |
| lab-summary (A6) | BC Tong Hop Xet Nghiem | khong co | Portrait | SQL that, doanh thu co the bang 0 neu chua phat sinh hoa don (xem muc 6) |

---

## 6. Ghi chu schema con thieu (de backend bo sung o dot sau)

1. A2 (refund-receipts): diab_his_bil_payments chua co cot rieng refund_reason va refunded_by.
   Hien dung tam payments.note lam Ly do hoan va payments.paid_by lam Nguoi thuc hien (xap xi, co the
   khong dung nguoi xu ly hoan tien thuc te).
2. A3 (void-receipts): diab_his_bil_billing chua co cot rieng voided_by va voided_at. Hien dung tam
   updated_by va updated_at khi status bang VOID (xap xi).
3. A4 (advances): he thong CHUA CO bang luu tam ung/dat coc benh nhan. Can them bang moi, ten goi y
   diab_his_bil_advances gom cac cot id, tenant_id, patient_id, billing_id, amount, applied_amount,
   remaining_amount, status, created_at, created_by. Descriptor advances hien tra ve SQL luon rong
   (tenant-scoped, an toan, khong loi) cho den khi bo sung bang.
4. A6 (lab-summary): doanh thu uu tien lay tu diab_his_bil_billing_items (join qua ref_id la id cua
   diab_his_cli_lab_orders, item_type bang LAB); neu chua co dong billing item tuong ung, revenue tra ve 0
   thay vi suy tu don gia danh muc diab_his_dict_lab_tests (tranh gay hieu nham so lieu bia). unitPrice van
   fallback ve don gia danh muc de hien thi tham khao.

Tat ca cac query tren deu chay duoc (khong loi SQL) tren schema hien tai - cac diem ghi chu o tren chi la
xap xi ngu nghia (semantic best-effort), KHONG phai loi runtime.

---

## 6b. Nhom B-E — 17 descriptor moi dang ky (dot nay), ghi chu schema con thieu

Da dang ky them 17 descriptor trong `ReportRegistry.cs` (Nhom B/C/D/E), tai su dung dung engine hien co
(khong sua `GenericReportDataService` / controller / exporter). Toan bo query van bat buoc `WHERE tenant_id = @tenantId`.

| code | title | orientation | Trang thai schema |
|------|-------|-------------|-------------------|
| ctdv-kham-benh (B1) | CTDV BN Khám Bệnh | Portrait (7 cột) | SQL thật đầy đủ (diab_his_enc_encounters + enc_diagnoses + bil_billing) |
| ctdv-sieu-am (B2) | CTDV BN Siêu Âm | Portrait (7 cột) | SQL thật (diab_his_cli_rad_orders modality='US' + cli_rad_results) |
| ctdv-xquang (B3) | CTDV BN XQuang | Portrait (7 cột) | SQL thật (modality='XRAY') |
| ctdv-noi-soi (B4) | CTDV BN Nội Soi | Portrait (7 cột) | SQL thật (modality='ENDO') — xem ghi chú quy ước modality bên dưới |
| ctdv-thu-thuat (B5) | CTDV BN Thủ Thuật | Portrait (6 cột) | SQL best-effort — xem mục schema thiếu #5 |
| ctdv-xet-nghiem (B6) | CTDV BN Xét Nghiệm | Portrait (7 cột) | SQL thật (diab_his_cli_lab_orders + cli_lab_results) |
| so-kham-benh (C1) | Sổ Khám Bệnh | Portrait (9 cột) | SQL thật, cột Địa chỉ xấp xỉ — xem mục #6 |
| so-sieu-am (C2) | Sổ Siêu Âm | Portrait (7 cột) | SQL thật (modality='US') |
| so-xquang (C3) | Sổ XQuang | Portrait (7 cột) | SQL thật (modality='XRAY') |
| so-noi-soi (C4) | Sổ Nội Soi | Portrait (7 cột) | SQL thật (modality='ENDO') |
| so-thu-thuat (C5) | Sổ Thủ Thuật | Portrait (6 cột) | SQL best-effort — xem mục schema thiếu #5 |
| so-xet-nghiem (C6) | Sổ Xét Nghiệm | Portrait (7 cột) | SQL thật (diab_his_cli_lab_orders + cli_lab_results) |
| so-dien-tim (C7) | Sổ Điện Tim | Portrait (7 cột) | SQL thật (modality='ECG') |
| luot-kham-theo-bs (D1) | Lượt Khám Theo BS | Portrait (6 cột) | SQL thật (CTE tổng hợp theo bác sĩ) |
| luot-kham-theo-pk (D2) | Lượt Khám Theo PK | Portrait (4 cột) | SQL thật (CTE tổng hợp theo phòng khám, % dùng window function) |
| benh-dien-tien (E1) | Bệnh Diễn Tiến | Portrait (7 cột) | SQL thật, ghép từ nhiều bảng — xem mục schema thiếu #7 |
| nghi-huong-bhxh (E2) | Nghỉ Hưởng BHXH | Portrait (9 cột) | CHƯA CÓ bảng dữ liệu — luôn trả rỗng, xem mục schema thiếu #8 |

Ghi chú schema con thieu bo sung (noi tiep muc 6 o tren, danh so tiep):

5. B5/C5 (ctdv-thu-thuat / so-thu-thuat): he thong CHUA CO bang chi dinh/ket qua thu thuat rieng (vd
   `diab_his_cli_procedure_orders`). Hien dung tam `diab_his_bil_billing_items` voi `item_type = 'PROCEDURE'`
   lam nguon du lieu (ten thu thuat = `bi.name`, thanh tien = `bi.line_total`); bac si thuc hien suy tu
   `encounter.doctor_id` cua hoa don lien ket (co the NULL neu hoa don khong gan encounter_id).
6. C1 (so-kham-benh): cot "Dia chi" dung tam `diab_his_pat_patients.street` — he thong CHUA CO bang danh
   muc dia gioi hanh chinh (tinh/huyen/xa) de resolve `province_code`/`district_code`/`ward_code` sang ten
   day du. Neu can dia chi day du, bo sung bang `dict_province` / `dict_district` / `dict_ward` va join lai.
7. E1 (benh-dien-tien): he thong CHUA CO bang theo doi dien tien lam sang rieng (vd
   `diab_his_cli_treatment_monitoring` — bang nay chi ton tai trong file dump tham chieu `db/diab_his_cli_treatment_monitoring.sql`,
   KHONG co trong cac migration dang chay `db/migrations/9000+`). Hien ghep tam tu
   `diab_his_enc_encounters` + `diab_his_enc_diagnoses` (chan doan) + `diab_his_enc_vital_signs` (HA, Glucose,
   BMI tinh tu can nang/chieu cao) + `diab_his_cli_lab_orders`/`cli_lab_results` (HbA1c, test_code = 'HBA1C').
   Ghi chu ho so (`note`) tam lay tu `encounter.chief_complaint`.
8. E2 (nghi-huong-bhxh): dung nhu PRD `reports-catalog-prd.md` muc 5.4 da xac nhan — he thong CHUA CO bang
   luu Giay chung nhan nghi viec huong BHXH (mau C65-HD1, TT 56/2017/TT-BYT). Descriptor nay LUON tra ve tap
   rong (tenant-scoped, an toan, khong loi SQL) cho den khi bo sung bang moi goi y
   `diab_his_cli_sick_leaves` (id, tenant_id, patient_id, encounter_id, cert_no, insurance_card_no_enc,
   icd10_code, days_off, leave_from, leave_to, doctor_id, issued_at, created_at, created_by).
9. B2-B4/C2-C4/C7 (sieu am/xquang/noi soi/dien tim): dung chung bang `diab_his_cli_rad_orders` +
   `cli_rad_results`, phan biet theo cot `modality` (VARCHAR tu do, khong co CHECK constraint). Quy uoc gia
   tri dang dung trong seed `db/migrations/0031_create_lab_rad_orders.sql`: `US` = Sieu am, `XRAY` = X-Quang,
   `ECG` = Dien tim; `ENDO` (Noi soi) duoc liet ke trong comment cot modality nhung CHUA co du lieu seed mau —
   neu quy trinh nhap lieu thuc te dung ma khac (vd `NS`, `ENDOSCOPY`), can dieu chinh gia tri filter modality
   trong descriptor `ctdv-noi-soi` / `so-noi-soi` cho khop.

---

## 7. Cach FE them 1 bao cao moi (Nhom B den E o cac dot sau)

FE khong can sua code khi backend dang ky them descriptor moi trong ReportRegistry - chi can:
1. Goi lai GET /api/v1/reports/catalog de lay code moi trong danh sach.
2. Dung lai dung 1 man hinh generic (filter form + grid + nut In Phieu/Xuat Excel) da build cho Nhom A,
   truyen code tuong ung vao 3 endpoint con lai.
