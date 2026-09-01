# Evidence — F-02 Đối soát ngân hàng/POS (import sao kê + auto-matching)

> Ngày: 2026-09-01 · Verify LIVE bởi leader qua API thật (curl) + browser thật (login ke_toan).

## Phạm vi
Hoàn tất phần còn thiếu của F-02: import file sao kê ngân hàng (Excel/CSV) + auto-matching với
`diab_his_bil_payments` + màn hình kế toán khớp/gỡ khớp thủ công.

## File test
`sao-ke-test.csv` — 6 dòng: 4 dòng khớp được với payment seed (tenant 1), 2 dòng rác.

| Dòng CSV | Ngày | Số tiền | Ref | Kỳ vọng | Lý do |
|---|---|---|---|---|---|
| 1 | 28/08 | 2.000.000 | VCB20260826001 | MATCHED | khớp ref chính xác |
| 2 | 31/08 | 1.100.000 | VCB20260829001 | MATCHED | khớp ref chính xác |
| 3 | 30/08 | 900.000 | (rỗng) | MATCHED | amount+ngày duy nhất → TCB20260828001 |
| 4 | 28/08 | 1.500.000 | VNPAY20260827A01 | MATCHED | khớp ref, paid 29/08 lệch 1 ngày (trong ±1) |
| 5 | 31/08 | 12.345.000 | XYZ999888 | UNMATCHED | không có payment |
| 6 | 27/08 | 999.000 | ABC111222 | UNMATCHED | không có payment |

## Kết quả verify

### 1. Migration (idempotent)
`9195_bank_reconciliation.sql` apply 2 lần liên tiếp vào DB dev thật → exit 0 cả 2 lần, 0 lỗi.
Tạo `diab_his_bil_bank_statements` + `diab_his_bil_bank_statement_lines`.

### 2. API import + auto-match (curl, token ke_toan)
`POST /api/v1/bil/bank-statements/import` (multipart, file CSV) → 201
`{ total_lines: 6, matched_lines: 4, unmatched_lines: 2 }` — ĐÚNG kỳ vọng.

`GET /{id}/lines` → 6 dòng, đúng từng dòng: 4 MATCHED (mỗi dòng gắn đúng payment: reference,
method, amount, paid_at, billing_id), 2 UNMATCHED. Trường ±1 ngày (dòng 4) khớp đúng.

### 3. Manual-match flow (curl)
`GET /lines/{lineId}/candidates` → trả 5 payment COMPLETED bank/card CHƯA khớp (loại đúng 4 payment
đã khớp). `POST /lines/{lineId}/manual-match {payment_id}` → MANUAL_MATCHED. `POST .../unmatch` →
về UNMATCHED. `POST .../ignore` khả dụng.

### 4. Browser thật (login ke_toan @ /cashier/bank-reconciliation)
- Nav "Đối soát ngân hàng" (icon Landmark) hiển thị trong nhóm Thu ngân.
- Empty state "Chưa có sao kê nào được tải lên" đúng.
- Dialog "Tải lên sao kê ngân hàng" (file .xlsx/.csv + mã NH + kỳ) render đúng.
- Bảng lịch sử: Tổng dòng 6 · Đã khớp **4** (badge xanh) · Chưa khớp **2** (badge vàng) · người tải "KT. Test Demo".
- Bảng chi tiết dòng: 4 dòng badge xanh "Đã khớp" kèm khoản thu (VCB20260826001 BANK_TRANSFER 2.000.000đ...),
  2 dòng badge vàng "Chưa khớp". Dòng khớp có nút "Gỡ khớp"; dòng chưa khớp có "Khớp thủ công" + "Bỏ qua".
- Test khớp thủ công qua UI: click "Khớp thủ công" dòng ABC111222 → dialog list 5 ứng viên → chọn
  VCB20260825001 1.200.000đ → toast "Đã khớp thủ công dòng sao kê với khoản thu" → badge đổi
  "Khớp thủ công", header cập nhật "Đã khớp: 5 / Chưa khớp: 1". OK.

## Screenshots
Xem các ảnh chụp màn hình trong phiên (mô tả ở trên). Data test đã xoá khỏi DB sau khi verify.

## Gate
- BE: `dotnet build` 0 lỗi; `dotnet test` 2165/2165 pass (giữ baseline).
- FE: `npx tsc --noEmit` sạch.
- Contract BE↔FE đối chiếu field-by-field trên response THẬT (không chỉ đọc code) — khớp hoàn toàn.
