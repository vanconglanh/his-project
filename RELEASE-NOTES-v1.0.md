# Release Notes — Pro-Diab HIS v1.0.0

Release date: 2026-05-23

---

## Tổng quan

**Pro-Diab HIS v1.0** là phiên bản ra mắt chính thức của hệ thống quản lý phòng khám đa khoa Cloud SaaS multi-tenant, được phát triển trong 13 sprint liên tục (khoảng 6 tháng).

Hệ thống phục vụ phòng khám quy mô 2-5 bác sĩ với đầy đủ luồng nghiệp vụ từ tiếp đón đến thu ngân, kho dược và BHYT.

---

## Sprint-by-Sprint Changelog

### Sprint 0 — Foundation
- Khởi tạo monorepo: `backend/` (.NET 8), `frontend/` (Next.js 15), `db/` (PostgreSQL 17)
- Docker Compose setup: postgres, redis, minio, backend, frontend, nginx
- CI/CD pipeline cơ bản
- Design system: TailwindCSS v4 + shadcn/ui, light/dark theme
- Cấu trúc database: multi-tenant với Row-Level Security (RLS), schema ban đầu

### Sprint 1 — Auth & Tenant
- JWT + Refresh Token authentication
- RBAC: Admin, BacSi, LeTan, DuocSi, KeToan, DieuDuong, KyThuatVien
- Đăng ký phòng khám (Tenant), subdomain routing
- Mời người dùng qua email (invite flow)
- Xác thực 2 lớp (TOTP/2FA) với mã khôi phục
- Đổi mật khẩu, quên mật khẩu, reset mật khẩu
- Login page, Accept Invite page

### Sprint 2 — Patient Module
- CRUD hồ sơ bệnh nhân (mã tự động, CMND, BHYT)
- Dị ứng (AllergyList): allergen, phản ứng, mức độ
- Liên hệ khẩn cấp (EmergencyContactList)
- Đồng ý điều trị (ConsentList)
- Upload ảnh đại diện bệnh nhân (MinIO)
- Tải lên kết quả CLS từ ngoài (ClsUploadList — PDF/PNG/JPEG)
- PatientForm với Zod validation khớp backend

### Sprint 3 — Reception & Queue
- Tiếp đón bệnh nhân, tạo phiếu khám
- Bảng hàng đợi realtime (poll 5 giây)
- Gọi vào khám, bỏ qua, huỷ lượt
- In phiếu số thứ tự
- Thống kê: đang chờ, đang khám, đã xong
- Ưu tiên: Thường / Ưu tiên / Khẩn cấp
- ReceptionCheckInForm, ReceptionQueueBoard, TicketCard

### Sprint 4 — Encounters & EMR
- CRUD lượt khám (Encounter): tạo, cập nhật, hoàn thành, huỷ
- Trang chi tiết 3 cột (hồ sơ BN / EMR / đơn thuốc + CLS)
- Bệnh án điện tử (EMR) với Tiptap rich-text editor
- Mẫu bệnh án (EmrTemplateSelector)
- Ký số bệnh án (EmrSignDialog)
- Lịch sử bệnh án theo phiên bản
- EncounterTimeline, EncounterStatusBadge

### Sprint 5 — Vital Signs & Diagnosis
- Nhập sinh hiệu: nhiệt độ, mạch, HA, SpO2, nhịp thở, cân nặng, chiều cao, BMI, đường huyết, điểm đau
- Cảnh báo nếu giá trị ngoài khoảng bình thường
- Lịch sử sinh hiệu với biểu đồ xu hướng (VitalSignsHistoryDrawer)
- Tra cứu ICD-10 (tìm theo mã/tên)
- Chẩn đoán: chính/phụ, chẩn đoán phân biệt
- Đánh giá tiểu đường (DiabetesAssessmentForm): HbA1c, biến chứng, mục tiêu điều trị
- DiabetesTrendChart

### Sprint 6 — Lab/Rad (CLS)
- Chỉ định xét nghiệm (LabOrderForm), chỉ định CĐHA (RadOrderForm)
- Nhập kết quả XN (LabResultForm), cờ bất thường, khoảng tham chiếu
- Xác thực kết quả XN (KTV ký xác thực)
- Import kết quả CSV/HL7 ORU
- Kết quả CĐHA (RadResultForm): mô tả, kết luận, upload DICOM
- Tích hợp đối tác lab bên ngoài (LabPartnerForm, LabPartnerConnectionTest)
- Dashboard tích hợp lab (LabIntegrationDashboard)
- LabResultTrendChart

### Sprint 7 — Prescriptions & DTQG
- Kê đơn thuốc (PrescriptionForm, PrescriptionItemForm)
- Tìm thuốc nhanh (DrugAutocomplete)
- Kiểm tra tương tác thuốc DDI (DdiWarningPanel)
- Ký số đơn thuốc (SignPrescriptionWizard)
- Đẩy đơn lên Đơn thuốc Quốc gia (donthuocquocgia.vn)
- QR Code đơn thuốc (QrPrescription)
- Cấu hình ĐTQG (DtqgCredentialsForm)
- Lịch sử gửi ĐTQG (DtqgSubmissionTable)

### Sprint 8 — Pharmacy & Inventory
- Danh mục thuốc (DrugForm, DrugImportDropzone — Excel)
- Kho dược: nhập kho (GrnForm), quản lý lô/HSD
- Tồn kho (StockTable): lọc theo kho, thuốc, lô
- Phát thuốc FEFO (DispenseConfirmDialog)
- Hàng đợi phát thuốc (DispenseQueueCard)
- Điều chỉnh tồn kho / kiểm kê (AdjustmentForm)
- Cảnh báo tồn kho thấp, sắp hết hạn (AlertsTab)
- Nhà cung cấp (SupplierForm)
- Phiếu mua hàng (PurchaseOrderForm)

### Sprint 9 — Cashier & Billing
- Tạo hoá đơn tự động sau khi khám xong
- Thanh toán: tiền mặt, chuyển khoản, VietQR, MoMo, VNPay, thẻ
- QR Payment Modal (QrPaymentModal)
- Mở/đóng ca (CashierShiftOpenDialog, CashierShiftCloseDialog)
- Báo cáo ca: tổng thu, phân tích phương thức
- Công nợ (DebtsTab)
- Hoàn tiền / Void hoá đơn
- Hoá đơn điện tử (EInvoiceIssueDialog)
- Tích hợp nhà cung cấp HĐĐT

### Sprint 10 — BHYT & Notifications
- Export XML BHYT theo QĐ 4750/QĐ-BYT (5 bảng)
- Ký số XML BHYT (BhytSignDialog)
- Validate XSD (BhytExportStepper)
- Đối soát giám định (BhytReconcileUploader, BhytReconcileTable)
- Thống kê BHYT (BhytAmountChart)
- Web Push notification (service worker, VAPID)
- Thông báo realtime trong app (NotificationDropdown)
- Cấu hình thông báo: âm thanh, vị trí

### Sprint 11 — Reports & Dashboard
- Dashboard tổng quan: KPI card, biểu đồ doanh thu 30 ngày, lượt khám 30 ngày
- Top 10 bác sĩ (doanh thu), Top 10 thuốc
- Chuyên mục tiểu đường: phân bố HbA1c, tỉ lệ biến chứng
- Hoạt động gần đây (RecentActivityTimeline)
- Báo cáo tài chính: doanh thu, thanh toán, BHYT vs. tự trả
- Báo cáo lâm sàng: KPI bác sĩ, top ICD-10
- Báo cáo dược: top thuốc, cảnh báo kho
- Xuất Excel/PDF (ExportReportDialog)

### Sprint 12 — Portal & API Partners
- Cổng bệnh nhân (Patient Portal): OTP login, xem hồ sơ, đơn thuốc, kết quả XN
- Đặt lịch hẹn qua portal (PortalAppointmentBookForm)
- API Partners: Public API key management, scope, rate limit, quota
- API request log viewer (ApiPartnerRequestLogsTable)
- Sentry error tracking (frontend + backend)
- Audit log chi tiết mọi thao tác trên dữ liệu BN

### Sprint 13 — Polish & v1.0
- Tablet responsive: sidebar tự collapse < 1024px, DataTable horizontal scroll
- Command Palette (Ctrl+K): tìm BN, điều hướng, thao tác nhanh
- Vim-style navigation: `g+p`, `g+e`, `g+r`, `g+c`, `g+h`
- Shortcuts Modal (`?`): bảng phím tắt đầy đủ
- Empty state có icon + CTA cho mọi list view
- Skeleton loading: table, card grid, chart, form
- Route-level Error Boundary (error.tsx)
- Print stylesheet cho đơn thuốc, biên lai, BHYT XML
- PWA manifest với shortcut icons
- Color theme y tế (teal primary, medical context)
- Medical color palette cho chart
- i18n hoàn chỉnh: vi.json + en.json đủ key mọi page
- Tài liệu người dùng: 8 guide files

---

## Breaking Changes

**Không có breaking change** trong v1.0 (phiên bản ra mắt đầu tiên).

---

## Known Issues

| Issue | Ảnh hưởng | Workaround |
|---|---|---|
| DTQG API trả lỗi 500 khi token hết hạn không rõ thông báo | DuocSi | Vào Quản trị → ĐTQG → Test kết nối → lấy token mới |
| VietQR webhook đôi khi delay 10-30 giây trên mạng chậm | KeToan | Nhấn Làm mới sau 30 giây |
| In phiếu trên Safari iOS 16 bị cắt chữ | Lễ tân | Dùng Chrome hoặc Safari 17+ |
| Biểu đồ recharts không hiển thị đúng trên IE11 | Tất cả | IE11 không được hỗ trợ |

---

## Yêu cầu hệ thống

| Thành phần | Yêu cầu tối thiểu |
|---|---|
| Trình duyệt | Chrome 110+, Firefox 110+, Safari 16+, Edge 110+ |
| Màn hình | 768px chiều rộng trở lên (tablet 10 inch) |
| Internet | 1 Mbps ổn định |
| Server | 4 CPU, 8GB RAM, 100GB SSD (production) |
| OS | Ubuntu 22.04 LTS |
| Docker | 24.x trở lên |

---

## Liên hệ hỗ trợ

- Email: support@prodiab.vn
- Github Issues: internal repo
