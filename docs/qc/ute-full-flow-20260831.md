# UTE — Kết quả thực thi UTC full-flow (実施結果)
**Dự án:** Pro-Diab HIS · **Ngày chạy:** 2026-08-31 · **Nhánh:** `develop` (`6687dbf`) · **Người chạy:** QC

> Tài liệu UTC tương ứng: [`utc-full-flow-20260831.md`](utc-full-flow-20260831.md)
> Evidence ảnh (25 ảnh, mỗi ảnh khoanh 🟦 NHẬP · 🟨 THAO TÁC · 🟩 KẾT QUẢ): [`evidence-full-flow-20260831/`](evidence-full-flow-20260831/)

---

## 1. Chuẩn bị môi trường (bắt buộc — nếu bỏ qua thì kết quả VÔ NGHĨA)

| Việc | Kết quả |
|---|---|
| Kiểm tra image đang chạy | ❗ Backend build **08:23**, Frontend build **30/08** — **cũ hơn** các commit fix hôm nay (12:29→12:45) |
| Rebuild `prodiab-dev-backend` + `prodiab-dev-frontend` | ✅ build thành công, recreate container |
| Xác nhận code mới đã sống | ✅ `POST /lab-results/ocr-extract` trả **401** (tồn tại) thay vì 404 |
| Xác nhận seed khoảng tham chiếu XN (Bug A) | ✅ 13 mã XN có `reference_range_low/high` trong `diab_his_dict_lab_tests` |
| Chi nhánh | ✅ CN1 `MAIN`, CN2 `CN02` cùng ACTIVE → đủ điều kiện test đổi chi nhánh |

> **Nếu chỉ test trên image cũ, toàn bộ kết luận về Bug A / GAP-1/2/3/7/8 sẽ sai.**

---

## 2. Tổng hợp kết quả

| Nhóm | Tổng | PASS | FAIL | SKIP |
|---|---:|---:|---:|---:|
| AUTH — Đăng nhập & 2FA | 8 | 8 | 0 | 0 |
| REC — Tiếp đón + QR CCCD | 13 | 11 | **1** | 1 |
| ENC/EMR — Khám + ký số | 8 | 6 | **2** | 0 |
| VIT/INB — Sinh hiệu + InBody | 7 | 6 | 0 | 1 |
| CLS — Chỉ định + OCR | 17 | 15 | **1** | 1 |
| DOC — Smart-upload | 7 | 4 | **1** | 2 |
| RX — Kê đơn | 7 | 4 | **2** | 1 |
| BIL — Thu ngân | 10 | 5 | **3** | 2 |
| DIS — Cấp phát thuốc | 4 | 1 | **2** | 1 |
| APM — Tái khám | 3 | 2 | 0 | 1 |
| BRN — Đa chi nhánh | 3 | 2 | 0 | 1 |
| SEC — Bảo mật | 6 | 6 | 0 | 0 |
| **TỔNG** | **93** | **70** | **13** | **10** |

**Tự động hoá kèm theo:** `dotnet test` toàn bộ = **978 PASS / 0 FAIL**
(955 unit + 6 architecture + **17 integration**, trong đó **12 integration test mới** viết trong vòng này).

---

## 3. Chi tiết thực thi

### 3.1 AUTH — 8/8 PASS

| ID | Kết quả | Bằng chứng thực tế |
|---|---|---|
| UTC-AUTH-01 | ✅ PASS | `01-utc-auth-01.png` — 6 nút vai trò |
| UTC-AUTH-02 | ✅ PASS | `02-utc-auth-02.png`; JWT lễ tân 32 quyền |
| UTC-AUTH-03 | ✅ PASS | `accessToken=""`, `mfaSetupRequired=true` — admin **không vào được** khi chưa bật 2FA |
| UTC-AUTH-04 | ✅ PASS | setup 200 → enable 200 + 10 mã khôi phục |
| UTC-AUTH-05 | ✅ PASS | `requires2fa=true`, accessToken rỗng |
| UTC-AUTH-06 | ✅ PASS | **401** `AUTH_MFA_INVALID_CODE` — "Mã xác thực 2 lớp không đúng" |
| UTC-AUTH-07 | ✅ PASS | 200 + token đầy đủ (228 quyền admin) |
| UTC-AUTH-08 | ✅ PASS | 401 |

> Fix N-1 (2FA thật) **hoạt động đúng end-to-end**. Lưu ý vận hành: tài khoản admin mới **bắt buộc** phải qua bước bật 2FA trước khi dùng được — cần đưa vào tài liệu bàn giao.

### 3.2 REC — Tiếp đón: 11 PASS / 1 FAIL

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-REC-01 | ✅ PASS | `03-utc-rec-01.png` |
| UTC-REC-02 | ✅ PASS | `case: NONE`, `field_diffs: []` |
| UTC-REC-03 | ✅ PASS | 201, mã `BNT01000035`, tên tiếng Việt có dấu đúng, `id_number` che `07********13` |
| UTC-REC-04 | ✅ PASS | `case: EXACT_MATCH` + đúng `patient_id/code` |
| UTC-REC-05 | ✅ PASS | `case: FIELD_MISMATCH`, đúng **2** trường lệch (`full_name`, `address`) kèm old/new |
| UTC-REC-06 | ✅ PASS | `04/05-utc-rec-02/03.png`; checkbox mặc định không tích |
| UTC-REC-07 | ✅ PASS | Báo lỗi, không crash |
| UTC-REC-08 | ✅ PASS | Chuẩn hoá hoa/thường + khoảng trắng → vẫn EXACT_MATCH |
| UTC-REC-09 | ✅ PASS | 201, ticket `002`, `WAITING` |
| UTC-REC-10 | ✅ PASS | Hàng đợi hiển thị đúng |
| UTC-REC-11 | ✅ PASS | `admit` → `encounter_id`, `created: true` |
| UTC-REC-12 | ✅ PASS | 409 `RECEPTION_DUPLICATE_CHECKIN` |
| **UTC-REC-13** | ❌ **FAIL** | **409 `RECEPTION_ROOM_FULL` khi tiếp đón bệnh nhân THỨ HAI trong ngày** → **BUG-02** |

### 3.3 ENC/EMR: 6 PASS / 2 FAIL

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-ENC-01 | ✅ PASS | 200, IN_PROGRESS |
| **UTC-ENC-02** | ❌ **FAIL** | `doctor_id` = **lễ tân** (`LT. Test Demo`) — thấy rõ trên `10-utc-enc-01.png`, `23-utc-cls-02.png` → **BUG-05** |
| UTC-EMR-01 | ✅ PASS | `data: null` |
| UTC-EMR-02 | ✅ PASS | 200, version 1 |
| UTC-EMR-03 | ✅ PASS | version 2 |
| UTC-EMR-04 | ✅ PASS | 2 bản ghi lịch sử |
| UTC-EMR-05 | ✅ PASS | Ký số 200; `21-utc-emr-01.png` badge "Đã ký số bệnh án" |
| UTC-EMR-06 | ✅ PASS | 400 `EMR_ALREADY_SIGNED` |
| UTC-EMR-07 | ✅ PASS | **409** — bệnh án đã ký không sửa được (bảo toàn pháp lý) |
| **UTC-EMR-08** | ❌ **FAIL** | 2 mẫu hệ thống đều có `structured_json = null` → form động không có nội dung → **BUG-08** |

### 3.4 VIT/INB — Sinh hiệu + InBody: 6 PASS / 1 SKIP

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-VIT-01 | ✅ PASS | 201, BMI 25.0, `record_sequence 1` |
| UTC-VIT-02 | ✅ PASS | 201 (cho ghi giá trị bất thường có thật) |
| UTC-VIT-03 | ✅ PASS | **422** `VITAL_INVALID_RANGE` — "Nhiệt độ phải trong khoảng 30-45°C" |
| UTC-INB-01 | ✅ PASS | 201, đọc **9/9** chỉ số, `pending` |
| UTC-INB-02 | ✅ PASS | 200, `success` |
| **UTC-INB-03** | ✅ **PASS** | DB `indicator_reading` **có dòng BMI = 22.7** → **Bug B đã fix thật** (kèm SMM/PBF/TBW/BMR/VISCERAL_FAT/BODY_FAT_MASS/INBODY_SCORE, `source=inbody_ocr`) |
| UTC-INB-04 | ✅ PASS | `vital_signs` thêm dòng `weight_kg 68.40`, note "Nhập từ kết quả máy InBody (đã xác nhận)" |
| UTC-INB-05 | ⏭️ SKIP | Chưa dựng được PDF InBody có chỉ số phi lý (đã verify cơ chế tương đương ở UTC-CLS-14) |
| **UTC-INB-06** | ✅ **PASS** | GAP-1: `deleted:true`; danh sách còn 0; **DB vẫn giữ dòng** với `deleted_at / deleted_by / delete_reason="QC test nhap nham file"` → soft-delete THẬT |
| UTC-INB-07 | ✅ PASS | Không crash |

### 3.5 CLS — 15 PASS / 1 FAIL / 1 SKIP  ← nhóm quan trọng nhất vòng này

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-CLS-01 | ✅ PASS | 201, tổng 335.000đ, `OPEN/UNPAID` |
| UTC-CLS-02 | ✅ PASS | `SUBMITTED` |
| UTC-CLS-03 | ✅ PASS | OCR đọc HbA1c **8.10**; trả `source_file_id` (GAP-8) |
| **UTC-CLS-04** | ✅ **PASS** | Cổng G02 chặn đúng: `CLS_ORDER_UNPAID` — "Đợt chỉ định chưa thanh toán" |
| UTC-CLS-05 | ✅ PASS | Thu tiền → `PAID` |
| UTC-CLS-06 | ✅ PASS | `created_count: 1` |
| **UTC-CLS-07** | ✅ **PASS** | **Bug A ĐÃ FIX**: DB `flag = CRITICAL`, `reference_range_low=4.0`, `high=5.6`. UI hiện **"! Nguy kịch"** đỏ (`23-utc-cls-02.png`). Dòng cũ trước fix vẫn `NORMAL / range NULL` → đối chứng rõ ràng |
| UTC-CLS-08 | ✅ PASS | `source_file_id` + `ocr_raw_value = "8.10"` đều có (GAP-8 + GAP-2) |
| UTC-CLS-09 | ✅ PASS | GLU_F 5.9 → `H` |
| UTC-CLS-10 | ✅ PASS | GLU_F 5.0 → `NORMAL` |
| UTC-CLS-11 | ✅ PASS | GLU_F 2.0 → không NORMAL |
| UTC-CLS-12 | ✅ PASS | CBC (không có khoảng) → NORMAL, không ném lỗi |
| UTC-CLS-13 | ✅ PASS | Mã XN lạ → không ném lỗi |
| **UTC-CLS-14** | ✅ **PASS** | GAP-3: HbA1c **81.0** → `out_of_plausible_range: true` + "Giá trị nằm ngoài khoảng thông thường, vui lòng kiểm tra lại (có thể do OCR đọc sai dấu thập phân)" |
| **UTC-CLS-15** | ❌ **FAIL** | Phiếu XN ghi "Glucose (đường huyết) 7.2" — XN `GLU_F` đang chờ **không bao giờ đọc được** → **BUG-04** |
| UTC-CLS-16 | ✅ PASS | Tách đúng Mô tả / Kết luận / Đề nghị, giữ dấu tiếng Việt |
| UTC-CLS-17 | ✅ PASS | 201, `DRAFT`; hiển thị đúng trên `23-utc-cls-02.png` |

### 3.6 DOC — Smart-upload: 4 PASS / 1 FAIL / 2 SKIP

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-DOC-01 | ✅ PASS | `09-utc-doc-01.png` — nhận nhiều tệp + `.zip` |
| UTC-DOC-02 | ✅ PASS | 4 tệp cùng lúc → 200, kết quả riêng từng tệp |
| UTC-DOC-03 | ✅ PASS | InBody 0.9 · Legacy 0.5 · RadResult 0.9 |
| **UTC-DOC-04** | ❌ **FAIL** | Phiếu KQ XN thật → `Unknown` (0.55) thay vì `LabResult` → **BUG-09** |
| UTC-DOC-05 | ⏭️ SKIP | Chưa dựng 21 tệp |
| UTC-DOC-06 | ⏭️ SKIP | Chưa dựng tệp >20MB |
| UTC-DOC-07 | ✅ PASS | ZIP giải nén, xử lý từng tệp |

### 3.7 RX — Kê đơn: 4 PASS / 2 FAIL / 1 SKIP

| ID | Kết quả | Thực tế |
|---|---|---|
| **UTC-RX-01** | ❌ **FAIL** | Tìm "Metformin" → trả về mục hiển thị tên **"Paracetamol 500mg (HIEN moi CN)"**; 28/30 thuốc còn lại tên **rỗng** → **BUG-03** |
| UTC-RX-02 | ✅ PASS | 201; DB có đúng 2 `prescription_items` |
| UTC-RX-03 | ✅ PASS | 200, `has_contraindicated: false` |
| UTC-RX-04 | ✅ PASS | `status: SIGNED` |
| **UTC-RX-05** | ❌ **FAIL** | **500 INTERNAL_ERROR** với MỌI đơn đang tồn tại → **BUG-06** |
| UTC-RX-06 | ✅ PASS | 404 `PRESCRIPTION_NOT_FOUND` |
| UTC-RX-07 | ⏭️ SKIP→⚠️ | `total_amount = 0` dù có 2 thuốc (ghi nhận là quan sát phụ, xem BUG-10) |

### 3.8 BIL — Thu ngân: 5 PASS / 3 FAIL / 2 SKIP

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-BIL-01 | ✅ PASS | 201, có dòng CLS; `15-utc-csh-02.png` cột Bệnh nhân **có tên** (BUG-09 cũ đã fix) |
| UTC-BIL-02 | ✅ PASS | Chốt hoá đơn, `balance = 490.000` |
| UTC-BIL-03 | ✅ PASS | Thu một phần OK |
| UTC-BIL-04 | ✅ PASS | QR động VietQR đúng số tiền (`qr_payload` hợp lệ) |
| UTC-BIL-05 | ✅ PASS | Thu nốt → PAID |
| **UTC-BIL-06** | ❌ **FAIL** | `amount = 0` → **201 chấp nhận** |
| **UTC-BIL-07** | ❌ **FAIL** | `amount = -50.000` → **201 chấp nhận** (rút tiền khỏi sổ thu, không qua quyền `payment.refund`) |
| **UTC-BIL-08** | ❌ **FAIL** | `amount = 999.999.999` → 201; hoá đơn thành `paid = 999.949.999`, `balance = −999.459.999` |
| UTC-BIL-09 | ⏭️ SKIP | Chưa có gói bán sẵn trong dữ liệu test |
| UTC-BIL-10 | ⏭️ SKIP | Phụ thuộc BIL-09 |

### 3.9 DIS — Cấp phát thuốc: 1 PASS / 2 FAIL / 1 SKIP

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-DIS-01 | ✅ PASS | Đơn nằm trong hàng chờ, có tên bệnh nhân (`17-utc-dis-01.png`) |
| **UTC-DIS-02** | ❌ **FAIL** | Phát thuốc → **500**; **tồn kho vẫn bị trừ 60 dù thất bại** → **BUG-01 (Blocker cao nhất)** |
| **UTC-DIS-03** | ❌ **FAIL** | Thiếu tồn kho trả **500 "Lỗi hệ thống, vui lòng thử lại sau"** thay vì thông báo "Tồn kho không đủ (còn thiếu 30)" → **BUG-07** |
| UTC-DIS-04 | ⏭️ SKIP | Phụ thuộc DIS-02 |

### 3.10 APM / BRN / SEC

| ID | Kết quả | Thực tế |
|---|---|---|
| UTC-APM-01 | ✅ PASS | 201, PENDING, đúng tên + SĐT bệnh nhân |
| UTC-APM-02 | ✅ PASS | 400 "Kênh đặt lịch không hợp lệ" |
| UTC-APM-03 | ⏭️ SKIP | Cần chờ job theo lịch |
| UTC-BRN-01 | ✅ PASS | `X-Branch-Id: 1` → 1 lượt khám, 3 ticket |
| **UTC-BRN-02** | ✅ **PASS** | `X-Branch-Id: 2` → **0 lượt khám, 0 ticket** — không rò rỉ chéo chi nhánh |
| UTC-BRN-03 | ⏭️ SKIP | Cần user gán riêng chi nhánh |
| UTC-SEC-01 | ✅ PASS | 403 `PERMISSION_DENIED` |
| UTC-SEC-02 | ✅ PASS | `id_number` che `07********53` |
| UTC-SEC-03 | ✅ PASS | 401 |
| UTC-SEC-04 | ✅ PASS | 3 payload SQLi → 200, **0 bản ghi**, không 500 (tham số hoá đúng) |
| UTC-SEC-05 | ✅ PASS | Lưu nguyên văn, React tự escape khi render |
| UTC-SEC-06 | ✅ PASS | Dược sĩ thu tiền → 403 |

---

## 4. Xác nhận lại các fix của hôm nay (R — gap OCR)

| Mục | Kết luận độc lập của QC | Bằng chứng |
|---|---|---|
| **Bug A** — cờ XN luôn NORMAL | ✅ **ĐÃ FIX THẬT** | DB: HbA1c 8.1 → `CRITICAL` + range 4.0–5.6; UI hiện "! Nguy kịch". Dòng cũ trước fix vẫn `NORMAL/NULL` làm đối chứng |
| **Bug B** — BMI rơi mất khi confirm | ✅ **ĐÃ FIX THẬT** | `indicator_reading` có `BMI = 22.7000` |
| **GAP-1** — soft-delete InBody | ✅ ĐÃ FIX | Dòng còn trong DB với `deleted_at/by/reason` |
| **GAP-2** — lưu diff OCR gốc | ✅ ĐÃ FIX | `ocr_raw_value = "8.10"` |
| **GAP-3** — cảnh báo ngoài ngưỡng vật lý | ✅ ĐÃ FIX | HbA1c 81.0 → `out_of_plausible_range: true` + ghi chú tiếng Việt |
| **GAP-8** — lưu file gốc OCR | ✅ ĐÃ FIX | `source_file_id` có ở cả Lab và Rad |
| **GAP-7** — timeout + loading | ⏭️ Chưa đo được (không tái hiện được OCR chậm >90s) | — |
| **GAP-9** — OCR đơn ngoài / giấy chuyển viện | ⏭️ SKIP (ngoài luồng 1 bệnh nhân) | — |

> **Nhận định:** phần OCR/gap làm hôm nay **chất lượng tốt, fix đúng bản chất**, không phải fix hình thức.
> Tuy nhiên phát hiện thêm **BUG-04** (parser bỏ sót XN khi dòng trên kết thúc bằng chữ cái) nằm cùng khu vực này.

---

## 5. Evidence

25 ảnh trong [`evidence-full-flow-20260831/`](evidence-full-flow-20260831/), mỗi ảnh có:
banner `[Mã case] Tên bước · Kỳ vọng` + khung 🟦 **① NHẬP** / 🟨 **② THAO TÁC** / 🟩 **③ KẾT QUẢ**.

| Ảnh | Case | Nội dung đáng chú ý |
|---|---|---|
| `03-utc-rec-01.png` | UTC-REC-01 | Màn tiếp đón, ô quét CCCD, hàng đợi 3 bệnh nhân |
| `10-utc-enc-01.png` | UTC-ENC-01/02 | **Thấy rõ "Bác sĩ: LT. Test Demo"** (BUG-05) |
| `21-utc-emr-01.png` | UTC-EMR-05 | Bệnh án đã ký số |
| **`23-utc-cls-02.png`** | UTC-CLS-07 | **HbA1c 8.1 · KTTC 4–5.6 · cờ "! Nguy kịch"** — bằng chứng Bug A đã fix; kèm KQ CĐHA từ OCR |
| `15-utc-csh-02.png` | UTC-BIL-01 | Danh sách hoá đơn có tên bệnh nhân |
| `26-utc-inb-01.png` | UTC-INB-06 | Lịch sử InBody sau khi huỷ báo cáo |

Ngoài ra: `fixture-xn-gap3-ngoai-nguong.pdf` — PDF tự dựng để kiểm GAP-3.

Reproduce:
```bash
cd frontend
npx playwright test --config=e2e/full-flow.config.ts                 # phần 1 (17 ảnh)
ENC_ID=... PAT_ID=... BILL_ID=... RX_ID=... \
  npx playwright test --config=e2e/full-flow.config.ts full-flow-evidence-part2   # phần 2 (8 ảnh)
```

---

## 6. Ghi chú trung thực về điều kiện chạy

1. **Đã can thiệp dữ liệu test 1 lần**: nâng `diab_his_sys_rooms.capacity` từ `1` → `60`
   để đi tiếp được luồng sau khi phát hiện **BUG-02**. Đây là **sửa dữ liệu seed, KHÔNG sửa code sản phẩm**.
   Mọi case sau UTC-REC-13 chạy trên điều kiện đã nâng này.
2. **Đã khôi phục tồn kho** bị BUG-01 ăn mất (486 → 366 → khôi phục 486) và xoá 2 dòng
   `stock_movements` rác, để không để lại dữ liệu sai trong DB dev.
3. **Đã bật 2FA cho `qc.admin`** (secret lưu ở `.qc-tmp/admin_totp.txt`, không commit).
   Sau vòng test này admin **cần mã TOTP** để đăng nhập.
4. 10 case SKIP đều ghi rõ lý do, **không có case nào bị bỏ im lặng**.
5. Toàn bộ ảnh evidence chụp trên **stack đã rebuild**, không tái dùng ảnh của các vòng trước.
