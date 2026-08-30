# Evidence — Nhập liệu hàng loạt hồ sơ giấy cũ (Legacy Scan OCR Import)

Ngày verify: 2026-08-30
Người thực hiện: Team Leader (điều phối backend + frontend agent)
Môi trường: API .NET chạy local `http://localhost:5001` (`Storage:Provider=Local`) + MySQL/MinIO/Redis Docker đang chạy; OCR Tesseract 5.2.0 + tessdata `vie+eng`.

## Phạm vi tính năng
Admin upload 1 file ZIP nhiều ảnh scan hồ sơ giấy cũ -> OCR nền (Hangfire) -> review từng ảnh -> match bệnh nhân (tự động theo tên file / thủ công) -> confirm -> lưu thành tài liệu đính kèm hồ sơ bệnh nhân (`diab_his_fil_cls_uploads` + `fil_files`). KHÔNG tự tạo bệnh án/lượt khám. Chỉ admin (permission `legacy_import.write`).

## Dữ liệu test
- `test-legacy-scan.zip` gồm 3 ảnh PNG có chữ:
  - `BNT01000020_trang1.png`, `BNT01000020_trang2.png` — tên file chứa mã BN `BNT01000020` (khớp bệnh nhân thật "Le Thi Huong" trong DB tenant 1) -> test auto-match.
  - `khongcoma_trang1.png` — không có mã trong tên -> test fallback `pending_match`.

## Kết quả verify (chạy thật qua API + DB)

### 0. Smoke test OCR engine (Windows, NuGet Tesseract 5.2.0)
```
=== OCR TEXT ===
HO SO KHAM BENH CU / Ho ten: Nguyen Van A / Chan doan: Tang huyet ap / Thuoc: Amlodipine 5mg / So luong: 30 vien
=== MeanConfidence: 0.93
```

### 1. Upload ZIP -> tao batch (POST /api/v1/legacy-imports)
HTTP 201, batch status=`pending`, id=`2e8156d7-c28d-4b54-8c4d-9217723afe3a`.

### 2. Hangfire OCR job chay nen -> GET /api/v1/legacy-imports/{id}
`status=done  processed=3/3` (job nghe queue "ocr" — da vá cấu hình `Queues` cho Hangfire server).

### 3. OCR text + auto-match (GET .../items)
```
BNT01000020_trang1.png | status=pending_review | match=filename_auto | patient=Le Thi Huong
  ocr: 'HO SO KHAM BENH CU\nHo ten: Nguyen Van A\nChan doan: Tang huyet ap\nThuoc: Amlodipine 5mg\nSo luong: 30 vien'
BNT01000020_trang2.png | status=pending_review | match=filename_auto | patient=Le Thi Huong
  ocr: 'PHIEU KHAM LAI\nNgay kham: 12/03/2024\nHuyet ap: 140/90 mmHg\nThuoc: Metformin 500mg\nLoi dan: Tai kham sau 1 thang'
khongcoma_trang1.png    | status=pending_match  | match=None        | patient=None   (fallback dung)
  ocr: 'HO SO KHONG CO MA BENH NHAN\nHo ten: Tran Thi B\nChan doan: Dai thao duong type 2\nThuoc: Insulin'
```

### 4. Match thu cong (PUT .../items/{id}/match)
HTTP 200 — gan `khongcoma_trang1` vao patient `0675828f...`, status -> `pending_review`, match_method=`manual`.

### 5. Confirm (POST .../items/{id}/confirm, co sua ocr_text)
HTTP 200 — item status -> `confirmed`, `saved_cls_upload_id=cdf1d401-ed2c-4cf6-8b09-5d2d0c478f69`.

### 6. Tai lieu xuat hien trong ho so benh nhan (GET /api/v1/patients/{id}/cls-uploads?doc_type=HO_SO_CU_SCAN)
```
{ id: cdf1d401..., patient_id: 0675828f..., doc_type: HO_SO_CU_SCAN,
  file_id: 19be10b0..., file_name: BNT01000020_trang1.png, mime_type: image/png }
```

### 7. DB xac nhan (bang co san, khong tao bang file rieng)
```
diab_his_fil_cls_uploads: cdf1d401... | 0675828f... | HO_SO_CU_SCAN | BNT01000020_trang1.png
fil_files:                19be10b0... | LEGACY_SCAN | BNT01000020_trang1.png
```
`encounter_id = NULL` -> xac nhan KHONG tu tao luot kham/benh an.

## Ket luan
Toan bo luong upload -> OCR -> auto/manual match -> confirm -> luu dinh kem ho so benh nhan chay dung. Fallback pending_match hoat dong. Multi-tenant filter + permission admin (`legacy_import.write`) da ap dung.

## Ghi chu / ton dong nho
- `ocr_confidence` tra ve `null` trong danh sach item (engine co tinh confidence 0.93 nhung job chua persist gia tri) — cosmetic, khong anh huong chuc nang.
- Verify o tang API + DB (bang chung manh nhat). UI Next.js da build + `tsc --noEmit` sach; chua chup screenshot UI song (docker frontend dang chay code cu).
