# Đánh giá sẵn sàng triển khai — GÓC NHÌN NGHIỆP VỤ
**Pro-Diab HIS** · 2026-08-31 · nhánh `develop` (`6687dbf`) · QC

> Khác với [`go-live-readiness-20260830.md`](go-live-readiness-20260830.md) (tập trung **bảo mật + hạ tầng**),
> báo cáo này chỉ trả lời một câu hỏi: **phòng khám mở cửa ngày mai, dùng phần mềm này chạy được không?**
> Căn cứ: chạy thật 93 case theo đúng hành trình 1 bệnh nhân (xem [UTE](ute-full-flow-20260831.md)).

---

## 0. CẬP NHẬT 2026-08-31 (chiều) — ĐÃ FIX 4 BLOCKER

> **Dev đã sửa xong cả 4 Blocker (BUG-01→04) và verify LIVE trên stack rebuild + DB thật.**
> Commit riêng từng bug trên `develop`. Evidence: [`evidence-blocker-fix-20260831/`](evidence-blocker-fix-20260831/).
> `dotnet build` sạch · `dotnet test` **987 PASS / 0 FAIL** (+9 test mới) · `npx tsc --noEmit` sạch.

| Bug | Trạng thái | Cách verify (đã chạy thật) |
|---|---|---|
| **BUG-01** Phát thuốc lỗi vẫn trừ kho | ✅ **Đã fix** | Phát đơn có Gliclazide hết tồn → **422** `PHARMACY_STOCK_INSUFFICIENT` thông báo rõ ("Không đủ tồn kho để phát Gliclazide 80mg: còn thiếu 30"), tồn Metformin **giữ nguyên** (486/500/800/800), **0** EXPORT/phiếu phát phát sinh. Phát đơn đủ tồn → 201, trừ đúng, commit OK. Bọc `IDbTransaction` + pre-check toàn bộ trước khi ghi. |
| **BUG-02** Phòng chỉ nhận 1 BN/ngày | ✅ **Đã fix** | Với `capacity=1`: tiếp đón BN A → 201, BN B (khác người, cùng phòng, cùng ngày) → **201**. Đếm sức chứa theo BN **đang ở trong phòng** (CALLED+IN_PROGRESS), không luỹ kế cả ngày. |
| **BUG-03** Ô chọn thuốc sai/rỗng tên | ✅ **Đã fix** | `GET /drugs/search?q=Metformin` → **"Metformin 500mg"** (không còn "Paracetamol"); **0** thuốc tên rỗng. Thống nhất đọc/ghi cột `name`; migration `9191` đồng bộ dữ liệu. |
| **BUG-04** Thu tiền âm/0/vượt | ✅ **Đã fix** | `POST /payments` amount 0 & -50000 → **400** VALIDATION_ERROR; 999999999 → **400** EXCEEDS_BALANCE; override giá DV -999999 → **400**. Thêm validator cấp Command cho 5 chỗ + test kiến trúc chặn tái phát (xác nhận 5 chỗ là đầy đủ). |

---

## 1. KẾT LUẬN

# ⛔ CHƯA ĐỦ ĐIỀU KIỆN GO-LIVE

> **Lưu ý:** kết luận dưới đây là bản gốc lúc audit sáng 2026-08-31. Sau khi 4 Blocker đã được fix (mục 0),
> điều kiện chặn go-live về nghiệp vụ đã được gỡ — cần QC chạy lại đủ bộ 93 case + `dotnet test` để chốt PASS chính thức.

**Lý do 1 câu:** Ba khâu **bắt buộc dùng hàng ngày** đang hỏng ở mức chặn vận hành —
**tiếp đón không nhận được bệnh nhân thứ 2 trong ngày**, **cấp phát thuốc làm thất thoát tồn kho thật khi lỗi**,
và **thu ngân nhận được số tiền âm**. Đây không phải lỗi ngoại lệ hiếm gặp, mà nằm ngay trên đường đi chính.

**Điều kiện chuyển sang PASS:** sửa xong **4 Blocker** (BUG-01→04) + kiểm lại bằng đúng bộ case này.
Ước lượng: **1–2 ngày công**, vì cả 4 đều đã xác định chính xác dòng code gây lỗi.

### Đánh giá theo khâu

| Khâu trong hành trình | Trạng thái | Ghi chú |
|---|---|---|
| Đăng nhập / phân quyền / 2FA | 🟢 Dùng được | 8/8 case pass, RBAC + che PII + chống SQLi tốt |
| Quét QR CCCD + chống trùng hồ sơ | 🟢 Dùng được | 3 case trùng đúng hoàn toàn — làm rất chắc |
| **Tiếp đón (check-in)** | 🔴 **Chặn** | BUG-02: phòng chỉ nhận **1 BN/ngày** |
| Khám bệnh + bệnh án + ký số | 🟡 Dùng được, có khuyết | Ký số/bất biến sau ký rất chắc; nhưng gán sai bác sĩ + mẫu rỗng |
| Sinh hiệu (tay + InBody) | 🟢 Dùng được | Chặn giá trị vô lý đúng; InBody đọc 9/9 chỉ số |
| CLS — chỉ định + thu tiền + KQ | 🟢 Dùng được | Cổng thanh toán G02 chắc; **cờ cảnh báo XN đã đúng** |
| CLS — OCR đọc phiếu | 🟡 Dùng được một phần | BUG-04: bỏ sót XN tuỳ bố cục phiếu |
| **Kê đơn** | 🔴 **Chặn** | BUG-03: ô chọn thuốc **không có tên thuốc** |
| **Thu ngân** | 🔴 **Chặn** | BUG-01(tiền): nhận số tiền 0 / âm / vượt |
| **Cấp phát thuốc** | 🔴 **Chặn** | BUG-01: thất thoát tồn kho khi lỗi |
| Tái khám | 🟢 Dùng được | — |
| Đa chi nhánh | 🟢 Dùng được | Không rò rỉ dữ liệu chéo chi nhánh |

---

## 2. Bug phát hiện — xếp theo mức độ

### 🔴 BLOCKER (phải sửa trước khi giao cho phòng khám)

---

#### BUG-01 — Phát thuốc thất bại vẫn TRỪ tồn kho thật (mất hàng, không có phiếu)
- **Case:** UTC-DIS-02 / UTC-DIS-03
- **Mức:** 🔴 Blocker — **nghiêm trọng nhất vòng này**
- **Môi trường:** Docker local, `develop 6687dbf`
- **Các bước tái hiện:**
  1. Kê đơn 2 thuốc: Metformin 500mg (SL 60, **còn tồn**) + Gliclazide 80mg (SL 30, **hết tồn**)
  2. Ký số đơn → đơn vào hàng chờ phát
  3. Dược sĩ bấm phát thuốc
- **Kỳ vọng:** Không phát được thì **không được trừ gì cả**; báo rõ "Tồn kho không đủ".
- **Thực tế:**
  - HTTP **500** `INTERNAL_ERROR` — "Lỗi hệ thống, vui lòng thử lại sau"
  - Tồn Metformin lô `LOT-M001`: **486 → 426 → 366** (mỗi lần bấm mất thêm 60)
  - `diab_his_pha_dispenses`: **0 phiếu**
  - `diab_his_pha_stock_movements`: **2 dòng EXPORT 60** — hàng ra kho mà **không có chứng từ**
- **Vì sao đặc biệt nguy hiểm:** thông báo lỗi bảo người dùng *"vui lòng thử lại sau"* → dược sĩ sẽ bấm lại nhiều lần → mỗi lần mất thêm 60 đơn vị. Sai tồn kho kéo theo sai đặt hàng, sai giá vốn, sai báo cáo BHYT.
- **Bằng chứng:** mục 3.9 UTE; dump `diab_his_pha_stock` trước/sau.
- **Giả thuyết nguyên nhân:** `DispenseHandler.Handle` (`DispensingHandlers.cs:131`) trừ kho từng thuốc theo vòng lặp mà **không bọc transaction**; `FefoStrategyImpl.PickAsync` (`FefoStrategyImpl.cs:70`) ném `InvalidOperationException` giữa chừng → các thuốc đã trừ không rollback.
- **Khu vực đề xuất xử lý:** `ProDiabHis.Application/Pharmacy/Dispensing/DispensingHandlers.cs` + `Infrastructure/Pharmacy/FefoStrategyImpl.cs`.
  Cần (a) bọc toàn bộ trong 1 transaction, (b) kiểm đủ tồn cho **tất cả** dòng trước khi trừ dòng đầu tiên.

---

#### BUG-02 — Phòng khám chỉ tiếp đón được ĐÚNG 1 bệnh nhân mỗi ngày
- **Case:** UTC-REC-13 · **Mức:** 🔴 Blocker
- **Các bước tái hiện:**
  1. Lễ tân tiếp đón bệnh nhân A vào "Phòng khám số 2" → **201 OK**
  2. Tiếp đón bệnh nhân **B (người khác)** vào **cùng phòng, cùng ngày**
- **Kỳ vọng:** 201 — phòng khám phải nhận được nhiều bệnh nhân/ngày.
- **Thực tế:** **409** `RECEPTION_ROOM_FULL` — "Phòng khám đã đạt giới hạn lượt khám tối đa"
- **Giả thuyết nguyên nhân:** `diab_his_sys_rooms.capacity` mặc định `= 1` (ngữ nghĩa: *số bệnh nhân trong phòng cùng lúc*) nhưng `ReceptionHandlers.cs` đặt bí danh `capacity AS max_per_day` rồi so với **tổng số vé trong NGÀY** (loại trừ mỗi `CANCELLED`/`WAITING_CLS`). Cả 8 phòng seed đều `capacity = 1`.
- **Khu vực đề xuất xử lý:** `Application/Reception/ReceptionHandlers.cs` (~dòng 46 và 63-67).
  Chọn 1 trong 2: (a) tách cột `max_per_day` riêng + seed giá trị thực tế; hoặc (b) nếu ý định là giới hạn **đồng thời** thì chỉ đếm vé đang `WAITING/IN_PROGRESS`.
- **Ghi chú:** QC đã tạm nâng `capacity = 60` trên DB dev để test tiếp — **chưa sửa code**.

---

#### BUG-03 — Ô chọn thuốc khi kê đơn không hiển thị tên thuốc (và hiển thị SAI tên)
- **Case:** UTC-RX-01 · **Mức:** 🔴 Blocker (an toàn người bệnh)
- **Các bước tái hiện:** Bác sĩ gõ `Metformin` vào ô tìm thuốc (`GET /api/v1/drugs/search?q=Metformin`)
- **Kỳ vọng:** hiện `Metformin 500mg`
- **Thực tế:**
  - Mục trả về có `generic_name = "Metformin HCl"` nhưng **tên hiển thị = `"Paracetamol 500mg (HIEN moi CN)"`**
  - **28/30 thuốc còn lại** có tên hiển thị **rỗng**
- **Vì sao nguy hiểm:** bác sĩ tìm Metformin lại thấy nhãn "Paracetamol" — nguy cơ chọn nhầm thuốc. Còn lại thì danh sách trắng trơn, không kê được.
- **Giả thuyết nguyên nhân:** bảng `diab_his_pha_drugs` có **2 cột tên song song**: `name` (bộ chuẩn 9005, **đúng đủ 30/30**) và `name_vi` (bộ cũ 9010, **NULL 28/30**, 2 dòng còn lại là dữ liệu test sót từ vòng M-3). Đường **ghi** (`ClosedXmlImporter`, fix L-1) ghi vào `name`; đường **đọc** (`DrugHandlers.cs:55,94` — `d.name_vi as NameVi`) lại đọc `name_vi`.
- **Khu vực đề xuất xử lý:** `Application/Pharmacy/Drugs/DrugHandlers.cs` (thống nhất 1 cột tên) + dọn dữ liệu test sót ở 2 dòng TH001/TH002.
- **Ghi chú:** màn **chi tiết đơn thuốc** hiển thị **đúng** (`drug_name = "Metformin 500mg"`) → lỗi chỉ ở đường danh sách/tìm kiếm, nhưng đó đúng là chỗ bác sĩ chọn thuốc.

---

#### BUG-04 — API thu tiền nhận số tiền 0, ÂM và vượt xa số phải thu
- **Case:** UTC-BIL-06/07/08 · **Mức:** 🔴 Blocker (kiểm soát tài chính)
- **Các bước tái hiện:** `POST /api/v1/payments` với `amount = 0`, `-50000`, `999999999`
- **Kỳ vọng:** 400 VALIDATION_ERROR cho cả 3.
- **Thực tế:** cả 3 đều **201 COMPLETED**. Hoá đơn kết thúc ở trạng thái `paid_amount = 999.949.999`, `balance = −999.459.999`, `status = PAID`.
- **Vì sao nguy hiểm:** thu tiền **âm** = rút tiền khỏi sổ thu mà **không cần quyền `payment.refund`** và không đi qua đường hoàn tiền có kiểm soát → thủng nguyên tắc phân tách nhiệm vụ (SoD), khó phát hiện khi đối soát quỹ.
- **Nguyên nhân (đã xác định chắc chắn):** validator **có tồn tại** — `CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>` với `RuleFor(x => x.Amount).GreaterThan(0)` — **nhưng không bao giờ chạy**. `ValidationBehavior<TRequest>` phân giải `IValidator<TRequest>` với `TRequest = CreatePaymentCommand`, trong khi validator được khai báo cho `CreatePaymentRequest` và **thiếu lớp bọc** `RuleFor(x => x.Request).SetValidator(...)` ở cấp Command (Bệnh nhân và Lịch hẹn đều **có** lớp bọc này nên chạy đúng).
- **Phạm vi lan rộng — 5 validator đang "chết" theo cùng lỗi này:**

  | Validator | Ảnh hưởng |
  |---|---|
  | `CreatePaymentRequest` | 💰 Thu tiền — đã chứng minh |
  | `CreateServicePriceOverrideRequest` | 💰 Giá dịch vụ — **đã chứng minh**: tạo được override giá **−999.999đ** (201) |
  | `UpdateServicePriceOverrideRequest` | 💰 Giá dịch vụ |
  | `CreateDrugPriceOverrideRequest` | 💰 Giá thuốc |
  | `UpdateDrugPriceOverrideRequest` | 💰 Giá thuốc |

- **Khu vực đề xuất xử lý:** `Application/Billing/PaymentHandlers.cs`, `Billing/ServicePriceOverrideHandlers.cs`, `Pharmacy/Drugs/DrugPriceOverrideHandlers.cs`.
  Khuyến nghị thêm: viết 1 **test kiến trúc** bắt buộc mọi `*Command` bọc request phải có validator cấp Command — chặn tái phát lớp lỗi này.

---

### 🟠 HIGH (nên sửa trước go-live, chưa chặn tuyệt đối)

#### BUG-05 — Lượt khám gán bác sĩ là… lễ tân
- **Case:** UTC-ENC-02
- **Thực tế:** lễ tân bấm "Đưa vào khám" → `encounter.doctor_id` = **user lễ tân**; bác sĩ mở khám sau đó **không** ghi đè. Màn khám hiển thị "Bác sĩ: **LT. Test Demo**" (ảnh `10-utc-enc-01.png`, `23-utc-cls-02.png`).
- **Ảnh hưởng:** sai người chịu trách nhiệm chuyên môn trên hồ sơ; **báo cáo KPI bác sĩ sai**; bệnh án in ra ghi sai tên bác sĩ (rủi ro pháp lý).
- **Khu vực:** `Application/Reception/ReceptionHandlers.cs` (admit) + `Application/Encounters/EncounterHandlers.cs` (start nên gán `doctor_id` = người mở khám).

#### BUG-06 — Xem trạng thái liên thông Đơn thuốc Quốc gia luôn lỗi 500
- **Case:** UTC-RX-05
- **Thực tế:** `GET /api/v1/prescriptions/{id}/dtqg/status` → **500** với **mọi đơn đang tồn tại** (đơn không tồn tại thì trả 404 đúng).
- **Nguyên nhân:** `DtqgHandlers.cs:141` dùng `ExecuteScalarAsync<int?>` để đọc cột `ID` của `pha_prescriptions` — cột này là `CHAR(36)` GUID → `System.FormatException: The input string '763cec6a-…' was not in a correct format`. Sót lại từ thời khoá INT.
- **Ảnh hưởng:** khối trạng thái ĐTQG trên màn chi tiết đơn thuốc **hỏng hoàn toàn**; không kiểm tra được đơn đã liên thông chưa — liên quan nghĩa vụ TT 27/2021.

#### BUG-07 — Lỗi nghiệp vụ bình thường hiện thành "Lỗi hệ thống, vui lòng thử lại sau"
- **Case:** UTC-DIS-03
- **Thực tế:** hết tồn kho → 500 + thông báo chung chung, trong khi hệ thống **đã biết** thông điệp đúng: "Tồn kho không đủ (còn thiếu 30)".
- **Nguyên nhân:** `FefoStrategyImpl` ném `InvalidOperationException("PHARMACY_STOCK_INSUFFICIENT:…")`; middleware không nhận dạng nên quy về 500.
- **Ảnh hưởng:** dược sĩ không biết vì sao hỏng → bấm lại → kích hoạt **BUG-01**. Hai lỗi này cộng hưởng nhau.

#### BUG-08 — Mẫu bệnh án hệ thống không có nội dung biểu mẫu
- **Case:** UTC-EMR-08
- **Thực tế:** cả 2 mẫu ("Mẫu bệnh án tổng quát", "Mẫu bệnh án đái tháo đường") có `structured_json = null`. Chỉ các mẫu `TEST Snapshot …` (dữ liệu rác của vòng test hôm qua) mới có.
- **Ảnh hưởng:** tính năng EMR template hoá (L-3) **đã làm xong phần kỹ thuật nhưng chưa có dữ liệu**, nên bác sĩ chọn mẫu vẫn phải gõ tay — chưa nhận được giá trị.
- **Đề xuất:** đây là việc **dữ liệu chuẩn (master data)**, không phải lỗi code — cần nội dung chuyên môn từ bác sĩ. Kèm theo: dọn 4 mẫu `TEST Snapshot …` khỏi DB trước khi bàn giao.

---

### 🟡 MEDIUM

| ID | Vấn đề | Ảnh hưởng |
|---|---|---|
| **BUG-09** | Phiếu KQ xét nghiệm thật bị phân loại `Unknown` (0.55) thay vì `LabResult` | Điểm tin cậy cần ≥3 XN khớp mới đạt 0.9; phòng khám nhỏ thường chỉ chỉ định 1–2 XN → tính năng "tự nhận diện" thường xuyên **không tự nhận diện được** đúng loại phổ biến nhất |
| **BUG-10** | `prescription.total_amount = 0` dù đơn có 2 thuốc | Không hiển thị được tiền thuốc trên đơn |
| **BUG-11** | `POST /prescriptions` trả về `items: []` dù DB đã lưu đủ 2 dòng | FE phải gọi lại API mới thấy thuốc vừa kê |
| **BUG-12** | Màn khám hiện "Phòng: Chưa phân phòng" dù vé đã gán "Phòng khám số 1" | Gây nhầm lẫn điều phối |
| **BUG-13** | Quyền `patient.create` tồn tại nhưng **không endpoint nào dùng** (thực tế dùng `patient.write`) | Quyền chết, dễ gây hiểu nhầm khi cấu hình vai trò |

### 🔵 LOW

| ID | Vấn đề |
|---|---|
| BUG-14 | Dữ liệu rác lẫn trong DB dev: 4 mẫu bệnh án `TEST Snapshot …` (1 mẫu tên mojibake `?T?`), 2 thuốc bị đổi tên `(HIEN moi CN)` / `(se AN o CN2)` |
| BUG-15 | `POST /billings/{id}/finalize` không trả `total_amount` (chỉ có `balance`) |
| BUG-16 | Không có kênh đặt lịch `FOLLOW_UP`/`TAI_KHAM` (chỉ WALK_IN/PHONE/WEB/API/APP) dù có màn "Đặt lịch tái khám" |

---

## 3. Điểm LÀM TỐT — cần ghi nhận

Không phải chỗ nào cũng có vấn đề. Những phần sau **chắc chắn, kiểm tra kỹ và đạt**:

1. **Chống trùng hồ sơ theo CCCD** — 3 case (NONE / EXACT_MATCH / FIELD_MISMATCH) đúng tuyệt đối, có chuẩn hoá hoa-thường/khoảng trắng, trả đúng từng trường lệch kèm giá trị cũ–mới. Đây là chỗ dễ sai nhất khi tiếp đón và đã được làm rất chắc.
2. **Bất biến bệnh án sau ký số** — ký lần 2 bị chặn (400), sửa sau ký bị chặn (409). Đúng yêu cầu pháp lý.
3. **Cổng thanh toán CLS (G02)** — không thanh toán thì **không nhập được kết quả**, chặn đúng ở cả đường OCR.
4. **Cách ly dữ liệu đa chi nhánh** — đổi `X-Branch-Id` sang chi nhánh khác trả về **0 bản ghi**, không rò rỉ.
5. **2FA bắt buộc cho admin (fix N-1)** — chạy đúng đủ 5 bước, mã sai bị 401 với thông báo tiếng Việt.
6. **Che PII** — số CCCD **luôn** được che (`07********53`) kể cả với người có quyền đọc.
7. **Chống SQL injection** — 3 payload đều trả 0 bản ghi, không 500 → truy vấn tham số hoá đúng.
8. **Chặn giá trị sinh hiệu vô lý** — 422 kèm thông báo tiếng Việt cụ thể ("Nhiệt độ phải trong khoảng 30-45°C").
9. **Các fix hôm nay (Bug A, Bug B, GAP-1/2/3/8) là fix thật, đúng bản chất** — đã kiểm chứng độc lập ở tầng DB và UI, không phải fix hình thức.

---

## 4. Việc cần làm trước khi mở lại vòng kiểm tra

| Thứ tự | Việc | Mức | Ước lượng |
|---|---|---|---|
| 1 | **BUG-01** — bọc transaction cho phát thuốc + kiểm đủ tồn trước khi trừ | 🔴 | 0.5 ngày |
| 2 | **BUG-04** — bọc validator cấp Command cho 5 chỗ (ưu tiên thu tiền) | 🔴 | 0.5 ngày |
| 3 | **BUG-02** — sửa ngữ nghĩa `capacity` của phòng khám | 🔴 | 0.25 ngày |
| 4 | **BUG-03** — thống nhất cột tên thuốc + dọn dữ liệu test sót | 🔴 | 0.25 ngày |
| 5 | BUG-06 — sửa kiểu dữ liệu truy vấn trạng thái ĐTQG | 🟠 | 0.25 ngày |
| 6 | BUG-07 — ánh xạ lỗi nghiệp vụ kho thành 4xx có thông điệp thật | 🟠 | 0.25 ngày |
| 7 | BUG-05 — gán đúng bác sĩ khi mở khám | 🟠 | 0.25 ngày |
| 8 | BUG-08 — nạp nội dung mẫu bệnh án (cần bác sĩ) | 🟠 | phụ thuộc chuyên môn |

**Tổng kỹ thuật: ~2 ngày công.** Sau đó chạy lại đúng bộ 93 case này + `dotnet test`.

---

## 5. Rủi ro còn tồn (nằm ngoài phạm vi vòng này — PO cần biết)

| Rủi ro | Mức | Ghi chú |
|---|---|---|
| **Liên thông ĐTQG thật chưa test được** | Cao | Chưa có credential thật; lại đang vướng BUG-06. Nghĩa vụ theo TT 27/2021 |
| **Xuất XML BHYT 4210 / đối soát giám định chưa test** | Cao | Cần dữ liệu BHYT thật |
| Chưa kiểm hiệu năng / tải đồng thời | Trung bình | Môi trường dev, dữ liệu ít (30 thuốc, 20 XN) |
| Chưa kiểm trên tablet + khả năng truy cập (a11y) | Trung bình | Lễ tân/điều dưỡng hay dùng tablet |
| Sai lệch môi trường | Trung bình | Test trên Docker local, timezone UTC. Cần chạy lại smoke test trên staging đúng cấu hình prod |
| 5 validator "chết" có thể còn chỗ khác tương tự | Trung bình | Đã rà theo tên `*Request`; nên bổ sung test kiến trúc để chặn tái phát |

---

## 6. Kiểm thử tự động bổ sung trong vòng này

Đã viết **12 integration test mới** (chạy MySQL thật qua Testcontainers, theo đúng convention sẵn có của dự án):

| Tệp | Nội dung | Số test |
|---|---|---|
| `backend/tests/ProDiabHis.IntegrationTests/LabResults/LabResultFlagIntegrationTests.cs` | Khoá chặt **Bug A**: tra khoảng tham chiếu từ `diab_his_dict_lab_tests` rồi tính cờ — HbA1c 8.1 → `CRITICAL`, 5.9 → `H`, 5.0 → `NORMAL`, 2.0 → cảnh báo thấp, XN thiếu khoảng / mã lạ → không ném lỗi | 6 |
| `backend/tests/ProDiabHis.IntegrationTests/Patients/CccdDuplicateIntegrationTests.cs` | Chống trùng CCCD 3 case + chuẩn hoá hoa-thường + thiếu CCCD + cách ly tenant | 6 |

Kết quả `dotnet test` toàn bộ giải pháp: **978 PASS / 0 FAIL**
(955 unit · 6 architecture · 17 integration).

> Ghi chú: dự án **không** có sẵn khuôn mẫu test qua HTTP (`WebApplicationFactory` được khai báo nhưng chưa dùng ở đâu) — các test mới bám đúng khuôn hiện có (Testcontainers + `DbContext`/Dapper trực tiếp), không dựng lối đi mới.
