# Key Insights — Pro-Diab HIS Research

> 2 trang. Đọc 5 phút. Cơ sở chốt MVP scope.
> Tham chiếu: [his-feature-matrix.md](his-feature-matrix.md), [requirement-backlog.md](requirement-backlog.md), [legal-compliance-checklist.md](legal-compliance-checklist.md).

---

## Top 10 Insights

### 1. Thị trường VN có 2 phân khúc tách bạch rõ
**HIS bệnh viện** (Vimes, FPT.eHospital, VNPT-HIS, Medisoft, Viettel vHIS) — mạnh nghiệp vụ + tích hợp BHYT/ĐTQG đầy đủ, nhưng UI cũ, triển khai nặng, target BV ≥ 100 giường ([fpt-is.com](https://fpt-is.com/ehospital-2-0/), [vimes.com.vn](https://vimes.com.vn/san-pham/quan-ly-benh-vien/)).
**Phần mềm phòng khám nhỏ** (KiotViet Clinic, Dr.Cloud, một phần eClinic Song Ân) — UI hiện đại, dễ dùng, nhưng **thiếu BHYT, thiếu kê đơn QG, thiếu ký số** ([kiotviet.vn](https://www.kiotviet.vn/quan-ly-phong-kham), [drcloud.vn](https://drcloud.vn/)).
→ **Pro-Diab nằm vào khoảng trống**: UI hiện đại của lớp dưới + compliance đầy đủ của lớp trên.

### 2. Compliance là rào cản số 1 cho phần mềm phòng khám nhỏ
Từ **01/07/2024**, mọi CSKCB muốn được giám định BHYT phải gửi XML theo **QĐ 4750/QĐ-BYT** Bảng 1-5 ([thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/Cong-nghe-thong-tin/Quyet-dinh-4750-QD-BYT-2023-sua-doi-Quyet-dinh-130-QD-BYT-dinh-dang-du-lieu-kham-benh-593340.aspx)). KiotViet, Dr.Cloud, một số eClinic chưa làm được → đây là **wedge** để Pro-Diab chiếm thị phần phòng khám 2-5 BS.

### 3. Kê đơn điện tử (TT 27/2021) đã thành bắt buộc
Mã đơn 14 ký tự `xxxxxyyyyyyy-z` cấp bởi Hệ thống ĐTQG, ký số bằng USB token, đẩy ngay sau khi kết thúc khám ([thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/The-thao-Y-te/Thong-tu-27-2021-TT-BYT-ke-don-thuoc-bang-hinh-thuc-dien-tu-497939.aspx)). HIS lớn đã có; KiotViet/Dr.Cloud chưa rõ ràng. **Module Prescription + tích hợp ĐTQG là điểm sống còn của MVP.**

### 4. Quản lý lô + HSD là yêu cầu cứng (GPP)
Mọi phần mềm nhà thuốc đạt chuẩn GPP đều phải có quản lý lô, HSD, kiểm kê chính xác, liên thông CSDL Dược QG ([misaeshop.vn](https://www.misaeshop.vn/23643/phan-mem-lien-thong-duoc-quoc-gia/), [pharmadi.vn](https://pharmadi.vn/phan-mem-quan-ly-nha-thuoc-cua-so-y-te/)). Pro-Diab cần FEFO mặc định + cảnh báo cận date 30/60/90 ngày + liên thông Cục QLD.

### 5. EMR phải cập nhật ≤ 12h (TT 46/2018)
Điều 6 TT 46 quy định bệnh án điện tử cập nhật ≤ 12h kể từ khi có y lệnh ([drvip.vn](https://drvip.vn/10-diem-mau-chot-trong-thong-tu-46-2018-tt-byt-ve-benh-an-dien-tu/)). Cần background job cảnh báo encounter `in_progress > 12h` cho Admin.

### 6. Audit log bắt buộc trên dữ liệu BN/đơn/EMR
TT 46 Điều 9 + best practice ATTT. Trigger PostgreSQL ghi `his_audit_log` cho mọi INSERT/UPDATE/DELETE trên `his_patient`, `his_encounter`, `his_prescription`. Retention ≥ 10 năm.

### 7. FHIR là cơ hội differentiator không bị cạnh tranh
Không một HIS VN nào public về FHIR R4. Pro-Diab map internal model sang FHIR (Patient, Encounter, MedicationRequest, Observation, Condition) sẽ là điểm bán cho phòng khám có nhu cầu kết nối quốc tế / hợp tác chuỗi.

### 8. UX hiện đại là vũ khí
KiotViet/Dr.Cloud thắng phòng khám nhỏ nhờ UI dễ dùng, mobile-friendly. HIS lớn vẫn dùng layout phiếu in nhỏ. Pro-Diab với Next.js 15 + shadcn/ui + Tremor charts có thể vượt cả 2.

### 9. Focus chuyên khoa = tăng giá trị
Vimes quảng bá "46 loại bệnh chuyên khoa" ([vimes.com.vn](https://vimes.com.vn/san-pham/quan-ly-benh-vien/)) nhưng generic. Pro-Diab focus nội tiết-đái tháo đường (template HbA1c, đường huyết, BMI, eGFR, biến chứng mắt/thận/thần kinh) sẽ thu hút phòng khám tiểu đường — segment đang tăng nhanh.

### 10. Multi-tenant SaaS từ ngày đầu = bài toán kỹ thuật khó nhưng giá trị lớn
HIS lớn vẫn chủ yếu on-premise. KiotViet/Dr.Cloud cloud nhưng yếu nghiệp vụ. Pro-Diab dùng RLS PostgreSQL với `tenant_id` ngay từ migration đầu → đỡ refactor lớn về sau.

### 11. Workflow thực tế VN: Điều dưỡng đo sinh hiệu trước khi vào phòng BS
Tham chiếu SUNS Clinic Advance: phòng khám VN tách rõ vai trò **Điều dưỡng** đo sinh hiệu (mạch/HA/nhịp thở/nhiệt độ/SpO2) **nhiều lần** trong 1 lượt khám (trước - sau truyền dịch, theo dõi sau tiêm). BS chỉ đọc + bổ sung. Backlog gốc của Pro-Diab gộp vào US-E02 (BS nhập 1 record) — đã bổ sung US-N01..N03, E11 để phản ánh đúng thực tế.

### 12. Public API + Tích hợp lab thứ 3 + Web Push = bộ ba “mở rộng hệ sinh thái”
SUNS đã có sẵn: (a) API public cho App/Web/Cam AI đẩy BN vào HIS, (b) gửi/nhận XN với lab partner (Medlatec/Diag), (c) Web Push Notification realtime. Pattern này đang phổ biến → Pro-Diab cần thiết kế API gateway + API key per partner + Service Worker push ngay từ MVP+1 thay vì để post-MVP. Đồng thời tính năng **upload file CLS từ lễ tân** (BN mang kết quả ngoài) là chi tiết nhỏ nhưng dùng cực nhiều — đã thêm US-PT11/PT12/E10.

---

## Gap Analysis

### HIS lớn có — Pro-Diab MVP **không cần**
- PACS/RIS đầy đủ (chỉ cần upload file kết quả)
- HL7/ASTM kết nối máy XN (post-MVP)
- Quản lý nội trú, giường bệnh (Pro-Diab chỉ ngoại trú)
- Quản lý nhân sự, tài sản, văn bản điều hành (tách module riêng nếu cần)
- Chatbot AI tiếp đón, Kiosk thông minh (post-MVP)

### Phòng khám nhỏ thiếu — Pro-Diab MVP **phải có**
- Export XML BHYT chuẩn QĐ 4750 (Bảng 1-5)
- Tích hợp Đơn thuốc QG + mã đơn 14 ký tự
- Ký số đơn thuốc bằng USB token
- Quản lý lô + HSD + FEFO + liên thông Dược QG
- Audit log đầy đủ (TT 46)
- Tra cứu thẻ BHYT online

---

## Recommend MVP Scope (3 tháng)

**MVP = 58 story MUST trong [requirement-backlog.md](requirement-backlog.md).**

### Sprint roadmap đề xuất

| Sprint | Tuần | Module ưu tiên | Mục tiêu |
|---|---|---|---|
| S0 | 1-2 | Hạ tầng | Setup .NET 8 + PostgreSQL + Next.js + Docker. RLS multi-tenant. JWT auth. |
| S1 | 3-4 | Tenant, Users & RBAC, Audit Log | US-T01-03, US-U01-05, US-AL01-03 |
| S2 | 5-6 | Patient, Reception | US-PT01-06, US-R01-03, US-R05-06 |
| S3 | 7-8 | Encounter, LabRad | US-E01-06, US-E08, US-L01-05 |
| S4 | 9-10 | **Prescription + ĐTQG** (critical) | US-P01-05, US-P09 + tích hợp ĐTQG |
| S5 | 11-12 | **Pharmacy** (lô/HSD/FEFO) | US-PH01-08, US-PH10 |
| S6 | 13-14 | **Cashier + BHYT** | US-C01-02, US-C04-05, US-C07, US-B01-05 |
| S7 | 15-16 | Report/BI + Polish | US-RP01-03, US-RP05 + UAT |

### Quyết định cần PM chốt
1. **USB token ký số:** dùng VNPT-CA, Viettel-CA hay FPT-CA làm partner đầu tiên?
2. **Cổng giám định BHYT:** Pro-Diab tự build adapter hay dùng dịch vụ trung gian (TS24/eDoctor)?
3. **MinIO self-host hay AWS S3?** Cân nhắc chi phí + tuân thủ chủ quyền dữ liệu BYT.
4. **Định giá:** SaaS theo phòng khám hay theo BS? Khuyến nghị: theo phòng khám (flat) cho 2-5 BS để cạnh tranh KiotViet.
5. **Chuyên khoa nội tiết-ĐTĐ vào MVP hay sprint 8+?** Khuyến nghị: template ĐTĐ vào ngay S3 để có demo target chuẩn.
