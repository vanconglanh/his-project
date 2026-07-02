# ADR-0002: Chiến lược sandbox ĐTQG cho môi trường DEV/STAGING

- **Ngày:** 2026-05-31
- **Trạng thái:** Accepted (formalize implementation đã có)
- **Tác giả:** Lành (Architect)
- **Liên quan:** [docs/prd/crud-gap-triage-2026-05-31.md](../prd/crud-gap-triage-2026-05-31.md) — P0 action `Prescription.Submit`

---

## 1. Context

TT 27/2021/TT-BYT yêu cầu mọi đơn thuốc kê tại CSKCB phải đẩy lên Cổng Đơn thuốc Quốc gia (donthuocquocgia.vn), nhận `ma_don_thuoc` (14 ký tự) để in QR.

Hiện trạng:
- Token production chưa được cấp (đợi hợp đồng + hồ sơ CSKCB).
- BE **đã có** [backend/src/ProDiabHis.Infrastructure/Pharmacy/MockDtqgClient.cs](../../backend/src/ProDiabHis.Infrastructure/Pharmacy/MockDtqgClient.cs) implement `IDtqgClient` — sinh mã giả `VN{yyMMdd}{prescriptionId:D6}` 14 ký tự.
- BE đã có `DtqgController`, `DtqgSubmitRetryJob`, `QrCoderDtqgQrGenerator`.
- **Chưa có endpoint `POST /api/v1/prescriptions/{id}/submit-dtqg`** để FE gọi từ màn kê đơn → đây là gap cần fill.

FE sắp implement action `Prescription.Submit` (P0, effort L). Cần quyết định: BE stub trả mã giả, hay FE tự generate UUID local?

## 2. Decision

**(a) BE stub trả mã giả qua `MockDtqgClient` khi `Integrations:Dtqg:Mode=Mock`.** FE gọi BE endpoint thật, không bao giờ generate code phía client.

Cụ thể:
1. Giữ nguyên `MockDtqgClient` đang chạy. Đăng ký DI theo config:
   ```csharp
   if (config["Integrations:Dtqg:Mode"] == "Mock")
       services.AddScoped<IDtqgClient, MockDtqgClient>();
   else
       services.AddScoped<IDtqgClient, RealDtqgHttpClient>(); // future
   ```
2. **Tạo endpoint mới** `POST /api/v1/prescriptions/{id}/submit-dtqg` (xem contract spec) — gọi `IDtqgClient.SubmitPrescriptionAsync()` → lưu `ma_don_thuoc`, `submitted_at`, `status='submitted'` vào `pres_prescriptions`, ghi audit log.
3. Response trả về thêm `qr_url` = `GET /api/v1/prescriptions/{id}/qr.png` (BE render PNG bằng `QrCoderDtqgQrGenerator` đã có).
4. Header response thêm `X-Dtqg-Mode: Mock|Real` để FE hiển thị badge "Môi trường thử nghiệm" trên UI khi đang Mock.
5. Khi chuyển production: chỉ swap config `Mode=Real` + cấu hình token qua endpoint đã có `PUT /api/v1/dtqg/credentials`. **Không phải sửa FE.**

## 3. Consequences

**Tích cực:**
- FE viết code đúng-1-lần, không refactor khi production token sẵn sàng.
- Test E2E chạy được full luồng kê đơn → submit → in QR ngay từ DEV.
- Mã giả vẫn 14 ký tự đúng format ĐTQG → QR code render được, in thử được.
- Retry job + audit log đã có sẵn → dữ liệu giả test luôn happy path & failure path.

**Tiêu cực:**
- Phải nhớ swap config + cấp token trước khi go-live (mitigate: health check `GET /api/v1/dtqg/credentials/test` đã có, thêm pre-flight check vào deploy script).
- Mã giả có thể leak vào DB production nếu deploy sai config → mitigate: BE startup log WARN `[DTQG] Running in MOCK mode` + dashboard banner đỏ nếu env=Production AND Mode=Mock.

**Effort BE bổ sung:** 0.5 ngày (chỉ thêm 1 endpoint submit + wire vào handler, mock client đã sẵn).

## 4. Alternatives

| Phương án | Lý do reject |
|---|---|
| **(b) FE generate UUID local** | FE phải sửa lại khi có production → đúng 2 lần effort. Không có audit trail BE. Mã không đúng format 14 ký tự ĐTQG → QR sai chuẩn. |
| **Skip submit, để empty `ma_don_thuoc`** | Workflow đơn thuốc bị block, không test được luồng in QR + dispense. Bác sĩ không demo được cho khách hàng. |
| **Dùng portal sandbox của Cục QLKCB** | Chưa được Cục cấp account sandbox công khai. Mất tuần xin. |

## 5. Hành động tiếp theo

- Thảo (BE): tạo endpoint `POST /api/v1/prescriptions/{id}/submit-dtqg` + handler dùng `IDtqgClient`. Cấu hình `Integrations:Dtqg:Mode=Mock` ở `appsettings.Development.json`.
- Nam (FE): gọi endpoint, hiển thị `ma_don_thuoc` + QR. Hiển thị banner vàng "Môi trường thử nghiệm ĐTQG" khi header `X-Dtqg-Mode=Mock`.
- Chương (DevOps): bổ sung pre-flight check deploy `Mode=Real` cho production; tạo runbook swap token.
