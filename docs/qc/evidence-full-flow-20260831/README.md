# Evidence — UTE full-flow 2026-08-31

Ảnh chụp trên stack Docker local **đã rebuild** ngày 31/08/2026 (backend + frontend).
Mỗi ảnh có banner `[Mã case] Tên bước · Kỳ vọng` và khoanh 3 vùng:

- 🟦 **① NHẬP** — ô/trường dữ liệu nhập vào
- 🟨 **② THAO TÁC** — nút/thao tác vừa thực hiện
- 🟩 **③ KẾT QUẢ** — vùng hiển thị kết quả

Tài liệu liên quan: [UTC](../utc-full-flow-20260831.md) · [UTE](../ute-full-flow-20260831.md) · [Kết luận nghiệp vụ](../go-live-readiness-nghiepvu-20260831.md)

| # | Ảnh | Mã case | Bước | Kỳ vọng |
|---|---|---|---|---|
| 1 | [`01-utc-auth-01.png`](01-utc-auth-01.png) | UTC-AUTH-01 | Đăng nhập — panel chọn vai trò (dev) | Hiện 6 nút vai trò test |
| 2 | [`02-utc-auth-02.png`](02-utc-auth-02.png) | UTC-AUTH-02 | Vào hệ thống với vai trò Lễ tân | Vào Dashboard, sidebar theo quyền lễ tân |
| 3 | [`03-utc-rec-01.png`](03-utc-rec-01.png) | UTC-REC-01 | Màn Tiếp đón | Có ô quét CCCD, form tiếp đón, bảng hàng đợi |
| 4 | [`04-utc-rec-02.png`](04-utc-rec-02.png) | UTC-REC-02 | Quét QR CCCD (mô phỏng máy quét) | Ô nhận đủ 7 trường phân tách bằng | |
| 5 | [`05-utc-rec-03.png`](05-utc-rec-03.png) | UTC-REC-03 | Sau khi quét — hệ thống kiểm tra trùng CCCD | Điều hướng tạo mới HOẶC hiện dialog trùng |
| 6 | [`06-utc-pat-01.png`](06-utc-pat-01.png) | UTC-PAT-01 | Danh sách bệnh nhân | Hiện danh sách + ô tìm kiếm |
| 7 | [`07-utc-pat-02.png`](07-utc-pat-02.png) | UTC-PAT-02 | Tìm bệnh nhân theo họ tên có dấu | Lọc đúng bệnh nhân tiếng Việt có dấu |
| 8 | [`08-utc-pat-03.png`](08-utc-pat-03.png) | UTC-PAT-03 | Chi tiết hồ sơ bệnh nhân | Có tab Lịch sử InBody + nút tải tài liệu tự nhận diện |
| 9 | [`09-utc-doc-01.png`](09-utc-doc-01.png) | UTC-DOC-01 | Hộp thoại tải tài liệu tự nhận diện | Nhận nhiều tệp PDF/ảnh hoặc 1 tệp ZIP |
| 10 | [`10-utc-enc-01.png`](10-utc-enc-01.png) | UTC-ENC-01 | Danh sách lượt khám (vai trò Bác sĩ) | Hiện lượt khám, có cột Chi nhánh |
| 11 | [`11-utc-lab-01.png`](11-utc-lab-01.png) | UTC-LAB-01 | Màn Cận lâm sàng | 2 tab Kết quả XN / Kết quả CĐHA |
| 12 | [`12-utc-lab-02.png`](12-utc-lab-02.png) | UTC-LAB-02 | Nhập kết quả XN — 2 tab Nhập tay / Đọc từ file | Có tab OCR 'Đọc từ file' |
| 13 | [`13-utc-lab-03.png`](13-utc-lab-03.png) | UTC-LAB-03 | Panel OCR đọc kết quả xét nghiệm | Có ô mã lượt khám + chọn file PDF/ảnh |
| 14 | [`14-utc-csh-01.png`](14-utc-csh-01.png) | UTC-CSH-01 | Màn Thu ngân | Có ca làm việc, hoá đơn chờ thu, công nợ |
| 15 | [`15-utc-csh-02.png`](15-utc-csh-02.png) | UTC-CSH-02 | Danh sách hoá đơn | Cột Bệnh nhân phải có tên (BUG-09 đã fix) |
| 16 | [`16-utc-csh-03.png`](16-utc-csh-03.png) | UTC-CSH-03 | Chi tiết hoá đơn | Có Thu tiền + Thanh toán QR động |
| 17 | [`17-utc-dis-01.png`](17-utc-dis-01.png) | UTC-DIS-01 | Màn Phát thuốc | Hàng chờ phát thuốc theo đơn đã ký |
| 18 | [`21-utc-emr-01.png`](21-utc-emr-01.png) | UTC-EMR-01 | Tab Bệnh án — đã ký số | Bệnh án đã ký: nội dung KHÓA, không sửa được |
| 19 | [`22-utc-cls-01.png`](22-utc-cls-01.png) | UTC-CLS-01 | Tab Cận lâm sàng — đợt chỉ định | Hiện đợt chỉ định + trạng thái thanh toán |
| 20 | [`23-utc-cls-02.png`](23-utc-cls-02.png) | UTC-CLS-02 | Tab Kết quả CLS — cờ cảnh báo (Bug A) | HbA1c 8.1 phải có cờ CRITICAL, KHÔNG phải NORMAL |
| 21 | [`24-utc-rx-01.png`](24-utc-rx-01.png) | UTC-RX-01 | Tab Đơn thuốc | Hiện 2 thuốc đã kê, trạng thái đã ký số |
| 22 | [`25-utc-apm-01.png`](25-utc-apm-01.png) | UTC-APM-01 | Tab Tái khám | Đặt lịch tái khám + danh sách lịch hẹn |
| 23 | [`26-utc-inb-01.png`](26-utc-inb-01.png) | UTC-INB-01 | Tab Lịch sử InBody | Danh sách lần đo; báo cáo đã huỷ (GAP-1) không còn hiển thị |
| 24 | [`27-utc-csh-01.png`](27-utc-csh-01.png) | UTC-CSH-01 | Chi tiết hoá đơn | Có mục hoá đơn, tổng tiền, nút Thu tiền / QR |
| 25 | [`28-utc-csh-04.png`](28-utc-csh-04.png) | UTC-CSH-04 | Màn Công nợ | Danh sách hoá đơn còn nợ, có tên bệnh nhân |

## Tệp phụ

| Tệp | Mục đích |
|---|---|
| `fixture-xn-gap3-ngoai-nguong.pdf` | PDF tự dựng có HbA1c 81.0 / Glucose 72.0 — kiểm GAP-3 (cảnh báo giá trị ngoài ngưỡng vật lý do OCR đọc sai dấu thập phân) |

## Ảnh đáng chú ý

- **`23-utc-cls-02.png`** — HbA1c 8.1, KTTC 4–5.6, cờ **"! Nguy kịch"**: bằng chứng **Bug A đã được sửa thật** (trước đây mọi kết quả xét nghiệm đều ra NORMAL).
- `10-utc-enc-01.png` — thấy rõ **"Bác sĩ: LT. Test Demo"** → bằng chứng BUG-05 (lượt khám gán nhầm lễ tân làm bác sĩ).
- `15-utc-csh-02.png` — danh sách hoá đơn đã có tên bệnh nhân (xác nhận BUG-09 cũ đã fix).

## Cách chạy lại

```bash
cd frontend
npx playwright test --config=e2e/full-flow.config.ts   # phần 1 (17 ảnh)
ENC_ID=<encounter> PAT_ID=<patient> BILL_ID=<billing> RX_ID=<prescription> \
  npx playwright test --config=e2e/full-flow.config.ts full-flow-evidence-part2   # phần 2 (8 ảnh)
```
