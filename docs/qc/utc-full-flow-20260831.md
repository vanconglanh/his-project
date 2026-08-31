# UTC — Unit Test Case (試験仕様書) · Full-flow 1 bệnh nhân
**Dự án:** Pro-Diab HIS · **Ngày lập:** 2026-08-31 · **Nhánh:** `develop` · **Người lập:** QC

---

## 1. Phạm vi

Bộ case này bám **đúng hành trình thật của 1 bệnh nhân đi khám**, không test rời rạc từng màn:

```
Tiếp đón → Hồ sơ bệnh nhân → Khám bệnh (EMR) → Sinh hiệu → CLS (XN/CĐHA)
   → Kê đơn → Thu ngân → Cấp phát thuốc → Tái khám
```

Các tính năng mới (2 ngày qua) được **lồng vào đúng điểm phát sinh trong luồng**, không tách riêng:

| Tính năng mới | Lồng vào bước nào |
|---|---|
| Quét QR CCCD + chống trùng 3 case (mục I) | Tiếp đón |
| EMR template hoá + ký số + snapshot (L-3) | Khám bệnh |
| InBody OCR + Bug B (BMI) + GAP-1/3 | Sinh hiệu |
| Lab/Rad OCR + Bug A (cờ XN) + GAP-2/8 | CLS |
| Smart-upload nhiều tệp / ZIP (P-6, P-7) | Hồ sơ bệnh nhân |
| QR động, gói dịch vụ (H-9, H-12, H-14) | Thu ngân |
| Đa chi nhánh (E/Đợt 0-5) | Xuyên suốt (đổi chi nhánh giữa chừng) |
| 2FA bắt buộc cho admin (N-1) | Đăng nhập |

## 2. Môi trường

| Hạng mục | Giá trị |
|---|---|
| Stack | Docker Compose local (`ops/docker-compose.yml` + `docker-compose.local-app.yml`) |
| Backend | `prodiab-backend` :5000 — **rebuild 2026-08-31 14:2x** (bắt buộc: image cũ 08:23 chưa có fix GAP/Bug A) |
| Frontend | `prodiab-frontend` :3000 — **rebuild 2026-08-31** (image cũ 30/08 chưa có FE GAP-1/3/7/8) |
| DB | MySQL 8 `prodiab_his`, charset utf8mb4 |
| Chi nhánh | `1 = MAIN` (Phòng khám ĐTĐ DiaBetis HCM), `2 = CN02` (Quận 7), `4 = CN-CLONE-TEST` (DRAFT) |
| Tài khoản | 5 role `Test@123` (`db/migrations/9137`) + `qc.admin` (bắt buộc 2FA) |

> **Lưu ý env parity:** đây là môi trường DEV local, timezone UTC trong container. Dữ liệu ít (30 thuốc, 20 XN).
> Các kết luận về hiệu năng/khối lượng **không** suy ra được cho prod.

## 3. Quy ước

- **Loại:** P = Positive · N = Negative · B = Boundary (biên) · E = Edge
- **Mức:** Blocker / High / Med / Low
- **Kiểm 3 lớp:** UI (ảnh evidence) + API (HTTP + JSON) + DB (dump SQL) — chỉ UI đúng là **chưa đủ**.

---

## 4. Ma trận case

### 4.1 Nhóm AUTH — Đăng nhập & 2FA

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng (UI / API / DB) | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-AUTH-01 | Panel dev bật | Mở `/login` | — | Hiện 6 nút vai trò test | P | Low |
| UTC-AUTH-02 | — | Bấm "Lễ tân" | `letan.test@prodiab.test` / `Test@123` | Vào dashboard; JWT có 32 quyền | P | Blocker |
| UTC-AUTH-03 | `qc.admin` chưa bật 2FA | Đăng nhập admin | `qc.admin@prodiab.test` / `Test@123` | **KHÔNG** cấp accessToken; trả `mfaSetupRequired=true` + `mfaSetupToken` | P | Blocker |
| UTC-AUTH-04 | Có `mfaSetupToken` | `POST /users/me/2fa/setup` → `/enable` | mã TOTP đúng | 200 + 10 mã khôi phục; DB `two_fa_enabled=1` | P | High |
| UTC-AUTH-05 | Đã bật 2FA | Đăng nhập lại | đúng mật khẩu | `requires2fa=true`, accessToken rỗng, có `mfaPendingToken` | P | Blocker |
| UTC-AUTH-06 | Có `mfaPendingToken` | `POST /auth/2fa/verify` | code `000000` (SAI) | **401** `AUTH_MFA_INVALID_CODE` + message tiếng Việt | N | Blocker |
| UTC-AUTH-07 | " | `POST /auth/2fa/verify` | mã TOTP ĐÚNG | 200 + accessToken đầy đủ | P | Blocker |
| UTC-AUTH-08 | — | Gọi API nghiệp vụ không token | `POST /lab-results/ocr-extract` | 401 | N | High |

### 4.2 Nhóm REC — Tiếp đón (gồm quét QR CCCD)

Chuỗi QR chuẩn: `soCCCD|soCMNDCu|hoTen|ngaySinh(ddMMyyyy)|gioiTinh|diaChi|ngayCap(ddMMyyyy)`

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-REC-01 | Đăng nhập lễ tân | Mở `/reception` | — | Có ô quét CCCD, form tiếp đón, bảng hàng đợi | P | High |
| UTC-REC-02 | CCCD chưa có hồ sơ | `GET /patients/check-cccd-duplicate` | `id_number` mới | `case = NONE`, `field_diffs = []` | P | Blocker |
| UTC-REC-03 | — | `POST /patients` | Họ tên **có dấu** `Nguyễn Thị Bích Hạnh`, DOB 1985-03-15, CCCD 12 số | 201; DB có bản ghi; `id_number` trả về **đã che** `07********13` | P | Blocker |
| UTC-REC-04 | Đã có hồ sơ | Quét lại, dữ liệu **y hệt** | cùng 4 trường | `case = EXACT_MATCH`, `field_diffs = []`, trả `patient_id/code` | P | Blocker |
| UTC-REC-05 | Đã có hồ sơ | Quét lại, dữ liệu **lệch** | tên `…Bích Hằng`, địa chỉ `99 Nguyễn Huệ` | `case = FIELD_MISMATCH` + đúng **2** phần tử `field_diffs` (full_name, address) kèm old/new | P | Blocker |
| UTC-REC-06 | Case 3 | Dialog so sánh | — | 4 cột; checkbox **mặc định KHÔNG tích**; chỉ cập nhật trường đã tích | P | High |
| UTC-REC-07 | — | Quét chuỗi sai định dạng | 5 field thay vì 7 | Báo lỗi tiếng Việt, KHÔNG crash | N | Med |
| UTC-REC-08 | — | So sánh khác hoa/thường + thừa khoảng trắng | `  nguyễn thị   bích hạnh ` | Vẫn `EXACT_MATCH` (BR-DUP-005 chuẩn hoá) | E | Med |
| UTC-REC-09 | Có hồ sơ | `POST /reception/check-in` | phòng PK01, ưu tiên NORMAL | 201, ticket `WAITING`, có số thứ tự | P | Blocker |
| UTC-REC-10 | Đã check-in | `GET /reception/queue` | — | Ticket xuất hiện trong hàng đợi | P | High |
| UTC-REC-11 | " | Gọi số → tiếp nhận vào phòng | — | `admit` trả `encounter_id`, `created=true` | P | Blocker |
| UTC-REC-12 | Đã check-in hôm nay | Check-in **lại cùng phòng** | cùng bệnh nhân | 409 `RECEPTION_DUPLICATE_CHECKIN` | N | High |
| **UTC-REC-13** | Phòng còn trống | Check-in **bệnh nhân thứ 2 khác người** trong ngày | BN B, cùng phòng | **Phải 201** (phòng khám phải nhận được nhiều BN/ngày) | B | **Blocker** |

### 4.3 Nhóm ENC/EMR — Khám bệnh, bệnh án, ký số

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-ENC-01 | Có encounter | `POST /encounters/{id}/start` | — | 200, status IN_PROGRESS; trừ định mức VISIT nếu có gói (best-effort, hết định mức KHÔNG chặn khám) | P | Blocker |
| **UTC-ENC-02** | Lễ tân admit, bác sĩ mở khám | Đọc `encounter.doctor_id` | — | Phải là **bác sĩ**, không phải người tiếp đón | P | High |
| UTC-EMR-01 | Encounter IN_PROGRESS | `GET /encounters/{id}/emr` | — | `data = null` (chưa có bệnh án) | P | Low |
| UTC-EMR-02 | — | `PUT …/emr` kèm `template_id` | 5 mục nội dung + `structured_values` | 200, version = 1, chụp `schema_snapshot_json` tại thời điểm lưu | P | Blocker |
| UTC-EMR-03 | Đã có v1 | Lưu lần 2 | sửa nội dung | version = 2 | P | High |
| UTC-EMR-04 | " | `GET …/emr/versions` | — | 2 bản ghi lịch sử | P | Med |
| UTC-EMR-05 | Có bệnh án | `POST …/emr/sign` | chữ ký base64 + cert id | 200, trạng thái đã ký; hash v2 gộp content+structured+schema | P | Blocker |
| UTC-EMR-06 | Đã ký | Ký lần 2 | — | 400 `EMR_ALREADY_SIGNED` | N | High |
| UTC-EMR-07 | Đã ký | Sửa bệnh án | nội dung mới | **409** `EMR_ALREADY_SIGNED` — không sửa được | N | Blocker |
| **UTC-EMR-08** | — | Kiểm mẫu hệ thống | `Mẫu bệnh án đái tháo đường` | Phải có `structured_json` để render form động | P | Med |

### 4.4 Nhóm VIT/INB — Sinh hiệu (nhập tay + máy InBody)

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-VIT-01 | Có encounter | Ghi sinh hiệu bình thường | T 36.8, M 82, HA 128/82, SpO2 98, 62.5kg/158cm | 201, BMI tự tính = 25.0, `record_sequence = 1` | P | Blocker |
| UTC-VIT-02 | — | Ghi giá trị **bất thường nhưng có thật** | T 41.5, M 190, HA 240/150, SpO2 70 | 201 (cho ghi) — cần cảnh báo trên UI | B | High |
| UTC-VIT-03 | — | Ghi giá trị **vô lý** | T 999, M −5, SpO2 500 | **422** `VITAL_INVALID_RANGE` + message tiếng Việt | N | High |
| UTC-INB-01 | Có encounter | Upload PDF InBody | `sample-inbody-full.pdf` | 201, đọc đúng **9/9** chỉ số, trạng thái `pending` (chưa ghi hồ sơ) | P | Blocker |
| UTC-INB-02 | Đã upload | Xác nhận (confirm) | tích dùng tất cả | 200, `extraction_status = success` | P | Blocker |
| **UTC-INB-03** | Đã confirm | Kiểm DB `indicator_reading` | — | **Có dòng `BMI`** (Bug B) + 7 chỉ số khác, `source = inbody_ocr` | P | High |
| UTC-INB-04 | " | Kiểm DB `vital_signs` | — | Thêm dòng `weight_kg = 68.40`, note "Nhập từ kết quả máy InBody (đã xác nhận)" | P | High |
| UTC-INB-05 | PDF có giá trị phi lý | Upload | chỉ số ngoài ngưỡng sinh học | `out_of_plausible_range = true` + ghi chú tiếng Việt (GAP-3) | B | High |
| UTC-INB-06 | Có báo cáo | `DELETE /inbody-reports/{id}?reason=…` | lý do | 200 `deleted:true`; danh sách không còn; DB **vẫn còn dòng** với `deleted_at/deleted_by/delete_reason` (soft-delete, GAP-1) | P | High |
| UTC-INB-07 | — | Upload PDF không phải InBody | file khác | Không crash; đánh dấu không đọc được | N | Med |

### 4.5 Nhóm CLS — Chỉ định + nhập kết quả bằng OCR

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-CLS-01 | Encounter IN_PROGRESS | Tạo đợt chỉ định | 2 XN (GLU_F, HBA1C) + 1 CĐHA (siêu âm bụng) | 201, `status=OPEN`, `payment_status=UNPAID`, tổng tiền = 335.000 | P | Blocker |
| UTC-CLS-02 | Đợt OPEN | Chốt đợt (submit) | — | 200, `status=SUBMITTED` | P | High |
| UTC-CLS-03 | Đợt SUBMITTED | OCR đọc file KQ XN | `phieu-ket-qua-xn-test.pdf` | 200; đọc được HbA1c 8.10; trả `source_file_id` (GAP-8) | P | Blocker |
| UTC-CLS-04 | Đợt **CHƯA thanh toán** | Xác nhận lưu kết quả | 1 kết quả | **Chặn**: `CLS_ORDER_UNPAID` — cổng thanh toán G02 | N | Blocker |
| UTC-CLS-05 | — | Thu tiền đợt CLS | CASH 335.000 | 200, `payment_status = PAID` | P | Blocker |
| UTC-CLS-06 | Đã thanh toán | Xác nhận lưu kết quả | HbA1c 8.1 | 200, `created_count = 1` | P | Blocker |
| **UTC-CLS-07** | Đã lưu KQ | Kiểm cờ trong DB | HbA1c 8.1, khoảng 4.0–5.6 | **flag ≠ NORMAL** → `CRITICAL`; `reference_range_low/high` được lưu (Bug A) | P | **Blocker** |
| UTC-CLS-08 | " | Kiểm `source_file_id` + `ocr_raw_value` | — | Cả 2 có giá trị (GAP-8 lưu file gốc, GAP-2 lưu bản OCR gốc) | P | High |
| UTC-CLS-09 | XN vượt nhẹ ngưỡng | Tính cờ | GLU_F 5.9 (3.9–5.5) | flag = `H` | B | High |
| UTC-CLS-10 | XN trong khoảng | Tính cờ | GLU_F 5.0 | flag = `NORMAL` (không báo động giả) | B | High |
| UTC-CLS-11 | XN dưới ngưỡng | Tính cờ | GLU_F 2.0 | flag ∈ {L, LL, CRITICAL} | B | High |
| UTC-CLS-12 | XN không có khoảng tham chiếu | Tính cờ | CBC | `NORMAL`, không ném lỗi | E | Med |
| UTC-CLS-13 | Mã XN không tồn tại | Tra cứu khoảng | `KHONG_TON_TAI` | Không ném lỗi | E | Med |
| UTC-CLS-14 | PDF có giá trị OCR sai dấu thập phân | OCR extract | HbA1c **81.0** | `out_of_plausible_range = true` + ghi chú tiếng Việt (GAP-3); FE bắt tích checkbox | B | High |
| **UTC-CLS-15** | Phiếu XN có dòng "Glucose (đường huyết)" | OCR extract | phiếu chuẩn | Phải đọc được giá trị cho XN `GLU_F` đang chờ | P | **High** |
| UTC-CLS-16 | Có chỉ định CĐHA | OCR phiếu CĐHA | `phieu-ket-qua-cdha-test.pdf` | Tách đúng Mô tả / Kết luận / Đề nghị, giữ dấu tiếng Việt | P | High |
| UTC-CLS-17 | Đã OCR CĐHA | Xác nhận lưu | kèm `rad_order_id` | 201, bản ghi `DRAFT` | P | High |

### 4.6 Nhóm DOC — Tải tài liệu tự nhận diện (smart-upload)

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-DOC-01 | Ở hồ sơ BN | Mở "Tải tài liệu lên (tự nhận diện)" | — | Dialog nhận **nhiều tệp** PDF/ảnh hoặc **1 ZIP** | P | High |
| UTC-DOC-02 | — | Upload 4 tệp khác loại cùng lúc | InBody / hồ sơ cũ / KQ XN / KQ CĐHA | 200; **kết quả riêng từng tệp**; 1 tệp lỗi không hỏng tệp khác | P | High |
| UTC-DOC-03 | " | Kiểm phân loại | — | InBody → `InBody` 0.9; hồ sơ cũ → `Legacy` 0.5; CĐHA → `RadResult` 0.9 | P | High |
| **UTC-DOC-04** | BN có ≤2 XN đang chờ | Upload phiếu KQ XN thật | `case3-ket-qua-xet-nghiem.pdf` | Phải nhận ra là `LabResult` (không rơi về `Unknown`) | P | Med |
| UTC-DOC-05 | — | Upload >20 tệp | 21 tệp | 413 `DOC_TOO_MANY_FILES` + hướng dẫn dùng legacy-import | B | Med |
| UTC-DOC-06 | — | Upload tệp >20MB | — | 413 `DOC_TOO_LARGE` | B | Med |
| UTC-DOC-07 | — | Upload ZIP hỗn hợp | `batch-zip-3-files.zip` | Giải nén an toàn, xử lý từng tệp | P | Med |

### 4.7 Nhóm RX — Kê đơn

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| **UTC-RX-01** | Ở màn kê đơn | Tìm thuốc trong ô chọn thuốc | gõ `Metformin` | Hiện **đúng tên thuốc** `Metformin 500mg` | P | **Blocker** |
| UTC-RX-02 | — | Tạo đơn 2 thuốc | Metformin + Gliclazide | 201; DB có 2 dòng `prescription_items` | P | Blocker |
| UTC-RX-03 | Có đơn | `GET …/ddi-check` | — | 200, danh sách cảnh báo tương tác, cờ `has_contraindicated` | P | High |
| UTC-RX-04 | Có đơn | Ký số đơn | chữ ký + thumbprint | 200, `status = SIGNED` | P | Blocker |
| **UTC-RX-05** | Đơn đã tồn tại | `GET /prescriptions/{id}/dtqg/status` | — | **200** kèm trạng thái liên thông ĐTQG | P | **High** |
| UTC-RX-06 | Đơn không tồn tại | " | GUID rỗng | 404 `PRESCRIPTION_NOT_FOUND` | N | Med |
| UTC-RX-07 | Có đơn 2 thuốc | Đọc `total_amount` | — | Bằng tổng tiền thuốc (≠ 0) | P | Med |

### 4.8 Nhóm BIL — Thu ngân

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-BIL-01 | Encounter có dịch vụ | Tạo hoá đơn | `include_dispensing=true` | 201, có dòng CLS + thuốc; **cột Bệnh nhân có tên** | P | Blocker |
| UTC-BIL-02 | Hoá đơn DRAFT | Chốt hoá đơn | — | 200, tính đúng tổng phải thu | P | Blocker |
| UTC-BIL-03 | Đã chốt | Thu **một phần** | 40% tổng | 201; còn công nợ đúng phần chênh | P | Blocker |
| UTC-BIL-04 | Còn nợ | Tạo QR động | — | 200, `qr_payload` VietQR đúng số tiền còn phải thu | P | High |
| UTC-BIL-05 | " | Thu nốt | phần còn lại | 201, `balance = 0`, `status = PAID` | P | Blocker |
| **UTC-BIL-06** | Hoá đơn bất kỳ | Thu tiền số tiền **= 0** | `amount = 0` | **400** VALIDATION_ERROR | B | **Blocker** |
| **UTC-BIL-07** | " | Thu tiền số tiền **ÂM** | `amount = -50000` | **400** — cấm hoàn tiền trá hình qua đường thu | N | **Blocker** |
| **UTC-BIL-08** | " | Thu **vượt xa** số phải thu | `amount = 999.999.999` | Chặn hoặc cảnh báo; **không** để `balance` âm | B | **High** |
| UTC-BIL-09 | Có gói dịch vụ | Bán gói + thu đặt cọc | — | Ghi `pkg_payment_records`, không xuất khống phần chưa thu | P | High |
| UTC-BIL-10 | Gói hết hạn còn định mức | Gia hạn | — | Cộng thêm X ngày theo setting, không cộng dồn định mức | P | Med |

### 4.9 Nhóm DIS — Cấp phát thuốc

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-DIS-01 | Đơn đã ký, đã thu tiền | `GET /pharmacy/dispense/queue` | — | Đơn xuất hiện trong hàng chờ, kèm tên bệnh nhân | P | Blocker |
| UTC-DIS-02 | Có đơn chờ | Phát thuốc theo lô | chọn kho + lô | 200; tồn kho giảm đúng số lượng | P | Blocker |
| UTC-DIS-03 | — | Phát vượt tồn kho | SL > tồn | Chặn, báo lỗi rõ ràng | N | High |
| UTC-DIS-04 | — | Từ chối phát | kèm lý do | Ghi nhận lý do | N | Med |

### 4.10 Nhóm APM — Tái khám

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-APM-01 | Có bệnh nhân | Đặt lịch tái khám | 30/09/2026 08:30, `source=PHONE` | 201, `status = PENDING`, hiện đúng tên + SĐT bệnh nhân | P | High |
| UTC-APM-02 | — | Đặt lịch `source` không hợp lệ | `FOLLOW_UP` | 400 "Kênh đặt lịch không hợp lệ" | N | Med |
| UTC-APM-03 | Có lịch hẹn | Job nhắc lịch | tới ngưỡng giờ | Gửi SMS/Zalo 1 lần, `reminder_sent_at` chống gửi trùng | P | Med |

### 4.11 Nhóm BRN — Đa chi nhánh

| ID | Tiền điều kiện | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|---|
| UTC-BRN-01 | Dữ liệu tạo ở CN1 | Gọi API với `X-Branch-Id: 1` | — | Thấy đủ lượt khám + hàng đợi của CN1 | P | Blocker |
| UTC-BRN-02 | " | **Đổi sang** `X-Branch-Id: 2` | — | **0** lượt khám, **0** ticket — không rò rỉ chéo chi nhánh | P | Blocker |
| UTC-BRN-03 | User không thuộc CN | Truy cập dữ liệu CN khác | — | Bị chặn/không thấy | N | High |

### 4.12 Nhóm SEC — Bảo mật & phân quyền

| ID | Bước thực hiện | Input | Kỳ vọng | Loại | Mức |
|---|---|---|---|---|---|
| UTC-SEC-01 | Bác sĩ tạo bệnh nhân | `POST /patients` | 403 `PERMISSION_DENIED` (không có `patient.write`) | N | High |
| UTC-SEC-02 | Đọc hồ sơ bệnh nhân | `GET /patients/{id}` | `id_number` **luôn được che** trong response | P | Blocker |
| UTC-SEC-03 | Gọi API không token | bất kỳ | 401 | N | High |
| UTC-SEC-04 | SQLi vào ô tìm kiếm | `' OR 1=1--` | Không trả toàn bộ dữ liệu, không 500 | N | Blocker |
| UTC-SEC-05 | XSS vào trường text | `<script>alert(1)</script>` | Lưu/hiển thị an toàn, không thực thi | N | High |
| UTC-SEC-06 | Dược sĩ gọi API thu tiền | `POST /payments` | 403 | N | High |

---

## 5. Truy vết yêu cầu → case

| Nguồn yêu cầu | Case bao phủ |
|---|---|
| PRD quét QR CCCD (mục I-1, I-2) | UTC-REC-02→08 |
| L-3 EMR template + snapshot + hash v2 | UTC-EMR-02, 05, 08 |
| J-1/J-2 InBody OCR | UTC-INB-01→07 |
| O-1→O-6 Lab OCR | UTC-CLS-03, 06, 15 |
| Q-1→Q-5 Rad OCR | UTC-CLS-16, 17 |
| P-6/P-7 smart-upload nhiều tệp/ZIP | UTC-DOC-01→07 |
| R/Bug A (cờ XN) | UTC-CLS-07, 09→13 |
| R/Bug B (BMI) | UTC-INB-03 |
| R/GAP-1 (soft-delete InBody) | UTC-INB-06 |
| R/GAP-2 (diff OCR) + GAP-8 (file gốc) | UTC-CLS-08 |
| R/GAP-3 (ngoài ngưỡng vật lý) | UTC-CLS-14, UTC-INB-05 |
| N-1 (2FA thật) | UTC-AUTH-03→07 |
| E/Đợt 0-5 (đa chi nhánh) | UTC-BRN-01→03 |
| H-9 (QR động) | UTC-BIL-04 |
| FR-1203/1211 (gói dịch vụ) | UTC-BIL-09, 10 |

---

## 6. Phần CHƯA bao phủ (ghi rõ để PO quyết — nguyên tắc 80/20)

| Hạng mục | Lý do | Rủi ro tồn đọng |
|---|---|---|
| Xuất XML 4210 BHYT + đối soát giám định | Cần dữ liệu BHYT thật + cổng giám định | Cao — pháp lý |
| Liên thông ĐTQG thật (gọi API donthuocquocgia.vn) | Chưa có credential thật | Cao — pháp lý (TT 27/2021) |
| Hiệu năng / tải đồng thời | Env dev, dữ liệu ít | Trung bình |
| Điều chuyển kho giữa chi nhánh (E/Đợt 3) | Đã verify ở phiên trước | Thấp |
| Báo cáo BI / dashboard chuỗi | Ngoài luồng khám 1 bệnh nhân | Thấp |
| Khả năng truy cập (a11y) + kiểm thử trên tablet | Ngoài phạm vi vòng này | Trung bình |
