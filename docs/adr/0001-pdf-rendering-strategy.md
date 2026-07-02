# ADR-0001: Chiến lược render PDF (hoá đơn, biên lai, phiếu)

- **Ngày:** 2026-05-31
- **Trạng thái:** Accepted
- **Tác giả:** Lành (Architect)
- **Liên quan:** [docs/prd/crud-gap-triage-2026-05-31.md](../prd/crud-gap-triage-2026-05-31.md) — P0 actions `Billing.PrintInvoice`, `Cashier.PrintReceipt`, `Reception.PrintTicket` (P2)

---

## 1. Context (Bối cảnh)

Sprint hiện tại có 3 action P0 cần render PDF:

1. `Billing.PrintInvoice` — hoá đơn A5/A4 giao cho bệnh nhân, lưu trữ pháp lý (TT78/2021/TT-BTC về hoá đơn điện tử).
2. `Cashier.PrintReceipt` — biên lai K80 in nhiệt sau mỗi lần thu tiền.
3. `Reception.PrintTicket` (P2, đã có): phiếu tiếp đón A6 — **đã chạy production** bằng QuestPDF (server-side) tại [backend/src/ProDiabHis.Api/Services/TicketPdfService.cs](../../backend/src/ProDiabHis.Api/Services/TicketPdfService.cs).

Backend hiện đã có sẵn endpoint:
- `GET /api/v1/billings/{id}/pdf` → trả `application/pdf` (đã được wire, dùng QuestPDF)
- `GET /api/v1/cashier/closing/{id}/pdf` → trả PDF báo cáo ca trực

Yêu cầu kiến trúc:
- Hoá đơn cần ký số tương lai (chứng thư số HSM/USB token) → bắt buộc kiểm soát byte-stream phía server.
- Phải archive bản gốc vào MinIO để đối soát thuế + giám định BHYT.
- Pixel-perfect khổ A5/K80, font Unicode tiếng Việt có dấu, không phụ thuộc browser/OS client.
- Tránh thêm 2 stack PDF khác nhau (FE jspdf + BE QuestPDF) → bảo trì 2 codebase template.

## 2. Decision (Quyết định)

**Chọn server-side rendering bằng QuestPDF (Community license), tái sử dụng pattern `TicketPdfService`.**

- Mọi tài liệu in chính thức (hoá đơn, biên lai, phiếu tiếp đón, báo cáo ca, đơn thuốc QR) đều render server-side.
- BE expose endpoint `GET .../pdf` hoặc `POST .../print` trả `application/pdf` + header `Content-Disposition: inline` để FE mở trong `<iframe>` preview rồi gọi `window.print()`.
- FE **không** dùng `jspdf`, `react-to-print`, `html2canvas`. FE chỉ chịu trách nhiệm: gọi API, hiển thị blob trong iframe, trigger print dialog.
- Service mới: `IInvoicePdfService` (A5), `IReceiptPdfService` (K80 — 58mm × dynamic height). Đặt tại `backend/src/ProDiabHis.Api/Services/`.
- Lưu bản PDF cuối cùng (sau finalize) vào MinIO bucket `prodiab-invoices/{tenant_id}/{yyyy}/{mm}/{invoice_no}.pdf`.

## 3. Consequences (Hệ quả)

**Tích cực:**
- 1 nguồn template duy nhất, không drift FE/BE.
- Sẵn sàng cắm chữ ký số HSM (QuestPDF hỗ trợ qua `iText` interop hoặc post-process PdfSharp).
- Pixel-perfect, font tiếng Việt nhúng (Arial/Roboto) — không phụ thuộc client.
- Audit + archive tự nhiên: byte-stream đi qua BE.

**Tiêu cực / chi phí:**
- Mỗi template thay đổi cần redeploy BE (mitigate: tách `PdfTemplates` thành file riêng, hot-reload sau).
- Tốn CPU server khi peak (mitigate: cache 5 phút sau finalize, background job pre-render).
- FE mất khả năng customize layout runtime (chấp nhận được — invoice/receipt là chuẩn cứng).

**Effort BE bổ sung:** 2 ngày (1 ngày `InvoicePdfService` A5, 1 ngày `ReceiptPdfService` K80 + endpoint mới).

## 4. Alternatives (Phương án đã cân nhắc)

| Phương án | Ưu | Nhược | Lý do reject |
|---|---|---|---|
| **Client `react-to-print`** | Không động BE, nhanh 1 ngày | Không ký số, không archive, layout phụ thuộc CSS browser | Không đạt yêu cầu pháp lý TT78 |
| **Client `jspdf` + `html2canvas`** | Full control FE | Font tiếng Việt vỡ, render canvas xấu, file 2-5MB | Chất lượng kém, không archive |
| **BE PuppeteerSharp (HTML → PDF)** | Template HTML/CSS dễ sửa | Chromium ~300MB, RAM peak 500MB/request | Quá nặng cho VM nhỏ phòng khám |
| **BE iText 7** | Chuẩn công nghiệp, ký số tốt | AGPL license phải trả phí cho SaaS | Cost không hợp lý cho MVP |
| **BE QuestPDF** ✅ | Community free, fluent API, code-first, Unicode OK, ~5MB | Template phải code C# | **Đã chạy production cho ticket, có precedent** |

## 5. Hành động tiếp theo

- Thảo (BE): tạo `InvoicePdfService`, `ReceiptPdfService`, wire endpoint mới `POST /api/v1/cashier/receipts/{paymentId}/print` (xem [docs/api/crud-gap-p0-contracts.md](../api/crud-gap-p0-contracts.md)).
- Nam (FE): viết helper `printPdfBlob(url)` mở iframe + `window.print()`. Không thêm dependency PDF nào.
- Chương (DevOps): tạo bucket MinIO `prodiab-invoices` + lifecycle 10 năm theo luật kế toán.
