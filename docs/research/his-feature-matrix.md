# HIS Feature Matrix — So sánh các HIS đang vận hành tại VN với Pro-Diab HIS (đề xuất)

> Phạm vi: 4 HIS lớn (Vimes, FPT.eHospital, VNPT-HIS, Medisoft) + 3 sản phẩm phòng khám nhỏ (KiotViet Clinic, eClinic của Song Ân, Dr.Cloud).
> Cột cuối cùng "Pro-Diab" là **đề xuất MVP** cho segment phòng khám 2-5 BS.
>
> Ký hiệu: `✅` có / `⚠️` một phần - cần xác minh thêm / `❌` không có / `❓` chưa verify được từ nguồn công khai.
>
> Nguồn tổng quan:
> - FPT.eHospital 2.0+ — [fpt-is.com/ehospital-2-0](https://fpt-is.com/ehospital-2-0/), [brochure FIS](https://www.fis.com.vn/Portals/_default/Brochure%20FPT.eHospital_2.0.pdf?ver=2018-11-13-100629-390&timestamp=1542105096133), [VnExpress: 300+ BV dùng](https://vnexpress.net/hon-300-benh-vien-su-dung-giai-phap-quan-ly-thong-minh-4417823.html)
> - VNPT-HIS — [vnpt.vn/.../vnpt-his](https://vnpt.vn/doanh-nghiep/san-pham-dich-vu/dich-vu-phan-mem-quan-ly-benh-vien-vnpt-his/), [vnpt.vn tin tức](https://vnpt.vn/tin-tuc/vnpt-his-phan-mem-quan-ly-toan-dien-cho-cac-benh-vien.html)
> - Vimes (Cty CP Phần mềm Y tế Việt Nam) — [vimes.com.vn](https://www.vimes.com.vn/), [QL tổng thể bệnh viện](https://vimes.com.vn/san-pham/quan-ly-benh-vien/), [tài liệu HD sử dụng 2024](https://giaothonghospital.vn/tai-lieu-huong-dan-su-dung-phan-mem-vimes-2024)
> - Medisoft (Medigroup) — [toancauits.com/phan-mem-quan-ly-medisoft](https://toancauits.com/phan-mem-quan-ly-medisoft.html)
> - KiotViet Clinic — [kiotviet.vn/quan-ly-phong-kham](https://www.kiotviet.vn/quan-ly-phong-kham), [bài blog QL phòng khám](https://www.kiotviet.vn/phan-mem-quan-ly-phong-kham-nang-cao-hieu-suat-giam-chi-phi-quan-ly/)
> - E-Clinic (Song Ân) — [ehis.vn/phan-mem-quan-ly-phong-kham-e-clinic](https://ehis.vn/phan-mem-quan-ly-phong-kham-e-clinic/), [trang sản phẩm](https://ehis.vn/products/giai-phap-quan-ly-phong-kham-da-khoa)
> - Dr.Cloud — [drcloud.vn](https://drcloud.vn/), [Google Play app](https://play.google.com/store/apps/details?id=com.perfin.drcloud)

---

## 1. Module Tenant / Clinic (đa cơ sở, cấu hình CSKCB)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet Clinic | eClinic (Song Ân) | Dr.Cloud | **Pro-Diab (đề xuất)** |
|---|---|---|---|---|---|---|---|---|
| Multi-tenant SaaS (1 hệ thống nhiều CSKCB) | ⚠️ | ⚠️ | ✅ (CSDL tập trung liên thông) | ❓ | ✅ (chuỗi cửa hàng) | ⚠️ | ✅ (cloud) | ✅ (RLS PostgreSQL theo `tenant_id`) |
| Cấu hình thông tin CSKCB (mã CSKCB BYT, mã BHYT) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| Cấu hình tích hợp ĐTQG / BHYT theo từng tenant | ✅ | ✅ | ✅ | ✅ | ❓ | ✅ | ❓ | ✅ (token mã hóa AES-256) |
| Multi-clinic dưới 1 tenant | ⚠️ | ✅ | ✅ | ❓ | ✅ | ⚠️ | ❓ | ✅ |

## 2. Module Users & RBAC

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Phân quyền theo vai trò (BS/LT/DS/KT) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Mã người hành nghề (TT 27 yêu cầu) | ✅ | ✅ | ✅ | ❓ | ❓ | ⚠️ | ❓ | ✅ (lưu `ma_nguoi_hanh_nghe`) |
| Ký số đơn thuốc (USB token / HSM) | ✅ | ✅ | ✅ (VNPT-CA) | ❓ | ❌ | ⚠️ | ❓ | ✅ (MUST cho ĐTQG) |
| 2FA / OTP login | ❓ | ⚠️ | ⚠️ | ❓ | ⚠️ | ❓ | ❓ | ✅ (SHOULD) |
| Audit log thao tác | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ | ❓ | ✅ (bắt buộc, QĐ 130 + TT 46) |

## 3. Module Patient (Hồ sơ bệnh nhân)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Quản lý hành chính (CCCD, DOB, địa chỉ) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Lưu thẻ BHYT + tra cứu thông tin thẻ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ❓ | ✅ |
| Lịch sử khám đầy đủ (timeline) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| Hồ sơ sức khỏe cá nhân (PHR portal) | ⚠️ | ✅ | ✅ | ❓ | ❌ | ❌ | ⚠️ | 🔵 COULD (post-MVP) |
| Map sang FHIR Patient resource | ❓ | ❓ | ❓ | ❌ | ❌ | ❌ | ❌ | ✅ (differentiator) |
| Mã hóa cột nhạy cảm (CCCD, BHYT) | ❓ | ❓ | ❓ | ❓ | ❌ | ❓ | ❓ | ✅ (AES-256-GCM) |

## 4. Module Reception (Tiếp đón)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Lấy số thứ tự, in phiếu tiếp đón | ✅ | ✅ (Kiosk thông minh) | ✅ | ✅ | ⚠️ | ✅ | ✅ (gọi loa) | ✅ |
| Đặt lịch khám online | ✅ | ✅ (chatbot) | ✅ | ⚠️ | ✅ | ⚠️ | ✅ (app riêng) | ✅ (MUST) |
| SMS/Email/Zalo nhắc lịch | ⚠️ | ✅ | ✅ | ❓ | ✅ | ⚠️ | ✅ | ✅ (SHOULD) |
| Phân phòng / chuyển phòng khám | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| Theo dõi trạng thái: Chờ - Đang khám - Xong | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| Bảng check-in gửi BHYT (QĐ 130 Bảng check-in) | ✅ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | ✅ (MUST nếu có BHYT) |

## 5. Module Encounter (Khám bệnh)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Bệnh sử, lý do khám, khám lâm sàng | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| Chẩn đoán ICD-10 (search + multi) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| Template khám theo chuyên khoa | ✅ (46 chuyên khoa) | ✅ | ✅ | ✅ | ❌ | ✅ | ⚠️ | ✅ (focus: nội tiết - tiểu đường) |
| Chỉ định CLS (XN/CĐHA) trực tiếp trong encounter | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ⚠️ | ✅ |
| Bệnh án điện tử (EMR) chuẩn TT 46 | ✅ | ✅ | ✅ | ⚠️ | ❌ | ⚠️ | ❌ | ✅ (MUST) |
| Gợi ý chẩn đoán AI | ⚠️ | ✅ (AI/Big Data) | ⚠️ | ❌ | ❌ | ❌ | ❌ | 🔵 COULD (GPT-4o, sau MVP) |
| Tái khám / liên kết encounter cũ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## 6. Module LabRad - CLS (XN + CĐHA)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| LIS - quản lý XN | ✅ | ✅ | ✅ (HIS/LIS/PACS) | ✅ | ❌ | ⚠️ | ❌ | ✅ MUST (cơ bản) |
| RIS/PACS - CĐHA | ✅ | ✅ | ✅ | ⚠️ (kết nối thiết bị) | ❌ | ⚠️ | ❌ | 🔵 SHOULD (chỉ kết quả + file PDF/DICOM upload MinIO) |
| Kết nối máy XN qua HL7/ASTM | ✅ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | 🔵 COULD (post-MVP) |
| Trả kết quả qua app/portal BN | ⚠️ | ✅ | ✅ | ❓ | ❌ | ❌ | ⚠️ | 🔵 SHOULD |
| Map sang FHIR Observation | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (differentiator) |

## 7. Module Prescription (Kê đơn)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Kê đơn thuốc + liều dùng / cách dùng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Đẩy đơn lên Hệ thống Đơn thuốc QG (TT 27) | ✅ | ✅ | ✅ | ✅ | ❓ | ⚠️ | ❓ | ✅ MUST |
| Mã đơn 14 ký tự `xxxxxyyyyyyy-z` (TT 27) | ✅ | ✅ | ✅ | ⚠️ | ❌ | ⚠️ | ❌ | ✅ MUST |
| In đơn + QR code mã đơn | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| Ký số đơn thuốc | ✅ | ✅ | ✅ | ⚠️ | ❌ | ⚠️ | ❌ | ✅ MUST |
| Cảnh báo tương tác thuốc / dị ứng | ⚠️ | ✅ | ⚠️ | ⚠️ | ❌ | ⚠️ | ❌ | 🔵 SHOULD |
| Đơn mẫu (template) theo bác sĩ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| Map sang FHIR MedicationRequest | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (differentiator) |

## 8. Module Pharmacy (Kho dược)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Quản lý nhập kho (PO, hóa đơn NCC) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| Quản lý theo **lô + HSD** | ✅ | ✅ | ✅ | ✅ | ✅ ([sapo.vn](https://www.sapo.vn/phan-mem-quan-ly-nha-thuoc.html)) | ✅ | ⚠️ | ✅ MUST |
| Xuất kho FIFO/FEFO theo HSD | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❓ | ✅ MUST (mặc định FEFO) |
| Cảnh báo cận date / hết hàng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ MUST |
| Kiểm kê định kỳ + biên bản chênh lệch | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❓ | ✅ |
| Liên thông Dược Quốc gia (Cục QLD) | ✅ | ✅ | ✅ | ✅ | ✅ (chuẩn GPP, [misaeshop.vn](https://www.misaeshop.vn/23643/phan-mem-lien-thong-duoc-quoc-gia/)) | ✅ | ❓ | ✅ MUST |
| Cấp phát thuốc theo đơn (gắn với Prescription) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| In tem nhãn / hướng dẫn sử dụng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| Quản lý đa kho / chuyển kho nội bộ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ❌ | 🔵 SHOULD |

## 9. Module Cashier (Thu ngân)

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Tính viện phí (khám + CLS + thuốc + thủ thuật) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Áp giá BHYT (% chi trả theo nhóm thẻ) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ❓ | ✅ MUST |
| Thanh toán không tiền mặt (QR/VNPay/Momo) | ✅ ([vimes.com.vn](https://vimes.com.vn/san-pham/quan-ly-benh-vien/)) | ✅ (viện phí thông minh) | ✅ | ⚠️ | ✅ | ⚠️ | ⚠️ | ✅ SHOULD |
| Hóa đơn điện tử (kết nối TCT) | ✅ | ✅ | ✅ (HĐĐT VNPT) | ⚠️ | ✅ | ⚠️ | ❓ | ✅ MUST |
| Công nợ bệnh nhân / trả góp | ⚠️ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ❓ | 🔵 SHOULD |
| Hoàn tiền / huỷ giao dịch | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |

## 10. Module BHYT

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Tra cứu thẻ BHYT online (API BHXH) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ❓ | ✅ MUST |
| Export XML chuẩn QĐ 130 + QĐ 4750 (Bảng 1-5) | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ | ✅ MUST |
| Gửi dữ liệu lên Cổng Giám định BHYT | ✅ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | ✅ MUST |
| Phản hồi giám định + sửa lỗi | ✅ | ✅ | ✅ | ⚠️ | ❌ | ⚠️ | ❌ | ✅ SHOULD |
| Bảng check-in (trạng thái khám realtime) | ✅ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | ✅ MUST |
| Bảng 11 (giấy nghỉ BHXH), Bảng 12 (giám định YK) | ⚠️ | ✅ | ✅ | ⚠️ | ❌ | ❌ | ❌ | 🔵 SHOULD |

## 11. Module Report / BI + Audit Log

| Feature | Vimes | FPT.eHospital | VNPT-HIS | Medisoft | KiotViet | eClinic | Dr.Cloud | **Pro-Diab** |
|---|---|---|---|---|---|---|---|---|
| Dashboard doanh thu, lượt khám | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Top thuốc, top dịch vụ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| KPI bác sĩ (lượt/ngày, doanh thu) | ✅ | ✅ | ✅ | ✅ | ⚠️ | ✅ | ⚠️ | ✅ |
| Báo cáo BYT (thống kê khám/chữa bệnh) | ✅ | ✅ | ✅ | ✅ | ❌ | ⚠️ | ❌ | ✅ SHOULD |
| Export Excel/PDF | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Audit log mọi thao tác trên BN/đơn/EMR (TT 46) | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ⚠️ | ❓ | ✅ MUST |

---

## 12. Bổ sung tham chiếu: SUNS Clinic Service - Advance

> HIS đang vận hành thực tế tại 1 phòng khám VN. Feature list do PM cung cấp. Chi tiết: [gap-suns-clinic.md](gap-suns-clinic.md).

| Module / Feature group | SUNS Clinic Advance | **Pro-Diab (sau cập nhật)** |
|---|---|---|
| Tiếp nhận BN | ✅ | ✅ |
| Thu ngân (gồm Visa/Master + QR động) | ✅ | ✅ (sau US-C09, US-C10) |
| Khám chữa bệnh (EMR) | ✅ | ✅ |
| Cận lâm sàng (CĐHA) | ✅ | ✅ |
| Khoa xét nghiệm (LIS nội bộ) | ✅ | ✅ |
| Kho – Dược | ✅ | ✅ |
| Cấp phát thuốc | ✅ | ✅ |
| Đẩy toa cổng Dược QG | ✅ | ✅ |
| Báo cáo | ✅ | ✅ |
| Web portal tra cứu kết quả khám (EMR) | ✅ | ⚠️ (US-PT08 COULD + US-API05) |
| Quản trị hệ thống | ✅ | ✅ |
| **Form Điều dưỡng riêng** | ✅ | ✅ (US-N01..N03, E11) |
| **Sinh hiệu N record/encounter** | ✅ | ✅ (US-N02) |
| **Upload kết quả CLS từ lễ tân** | ✅ | ✅ (US-PT11, PT12, E10) |
| **Ghi chú tiếp đón cross-module** | ✅ | ✅ (US-PT10) |
| **Ảnh đại diện BN** | ✅ | ✅ (US-PT09) |
| **API public bên thứ 3** | ✅ | ✅ (US-API01..API05) |
| **Tích hợp lab thứ 3 (gửi/nhận XN)** | ✅ | ✅ (US-LIS01..LIS03) |
| **Web Push Notification trình duyệt** | ✅ | ✅ (US-NT01..NT02) |
| **Ẩn/hiện cột tuỳ tenant** | ✅ | ✅ (US-NT03) |
| **Subdomain riêng + SSL + Storage quota** | ✅ | ✅ (US-T05..T08) |
| **Gói khám sức khỏe** | ✅ | ✅ (US-PKG01) |

---

## Tổng kết differentiator dự kiến của Pro-Diab

| # | Differentiator | Lý do |
|---|---|---|
| 1 | **Cloud SaaS multi-tenant** ngay từ đầu, RLS PostgreSQL | KiotViet/Dr.Cloud đã cloud nhưng yếu nghiệp vụ BHYT; FPT/VNPT mạnh nghiệp vụ nhưng triển khai on-prem nặng |
| 2 | **FHIR R4 internal model** | Không HIS VN nào public về FHIR — Pro-Diab dễ xuất khẩu / kết nối quốc tế |
| 3 | **Focus chuyên khoa nội tiết - đái tháo đường** | Template encounter + chỉ số theo dõi (HbA1c, BMI, BP, eGFR) sâu hơn HIS đa khoa generic |
| 4 | **UX hiện đại (Next.js 15 + shadcn/ui)** | HIS VN UI cũ, nhiều phiếu in nhỏ — Pro-Diab dùng layout dashboard ngay |
| 5 | **Pricing nhẹ cho 2-5 BS** | FPT/VNPT/Vimes target BV >100 giường, KiotViet không có nghiệp vụ y tế đầy đủ |
