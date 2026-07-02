# Requirement Backlog — Pro-Diab HIS

> Tổng cộng: **110 user story** (88 gốc + 22 bổ sung từ SUNS Clinic Advance), group theo 12 section.
> Format: `As a [role] I want [action] so that [value]`.
> Priority: **MUST** (MVP bắt buộc) / **SHOULD** (MVP+1) / **COULD** (post-MVP).
> Source: HIS tham chiếu hoặc văn bản pháp luật (xem [legal-compliance-checklist.md](legal-compliance-checklist.md) và [his-feature-matrix.md](his-feature-matrix.md)).

---

## Module 1 — Tenant / Clinic (4 stories)

### US-T01 — Đăng ký tenant mới [MUST]
- **As an** Admin hệ thống **I want** đăng ký 1 phòng khám mới với mã CSKCB do Sở Y tế cấp **so that** phòng khám có thể bắt đầu sử dụng hệ thống độc lập với tenant khác.
- **Source:** TT 27/2021 Điều 1 (cần mã CSKCB), VNPT-HIS multi-clinic.
- **AC:** Form nhập tên CSKCB, mã CSKCB (8 chữ số), địa chỉ, số GPHĐ. Tạo `tenant_id UUID` mới. RLS policy auto-apply. Không thể truy cập dữ liệu tenant khác.

### US-T02 — Cấu hình tích hợp ĐTQG per tenant [MUST]
- **As an** Admin tenant **I want** nhập token + endpoint ĐTQG riêng **so that** đơn thuốc của phòng khám đẩy đúng tài khoản trên hệ thống QG.
- **Source:** TT 27/2021 Điều 4, 7.
- **AC:** Token mã hóa AES-256-GCM trong DB. Test connection trước khi lưu. Lưu vào `his_tenant_integration` (kind = `dtqg`).

### US-T03 — Cấu hình tích hợp BHYT per tenant [MUST]
- **As an** Admin tenant **I want** nhập mã CSKCB BHYT + tài khoản Cổng giám định **so that** export XML 4750 gửi đúng cơ quan giám định.
- **Source:** QĐ 4750/QĐ-BYT.
- **AC:** Validate mã CSKCB; lưu mã hóa; có nút "test connection".

### US-T04 — Multi-clinic dưới 1 tenant [SHOULD]
- **As an** Admin chuỗi phòng khám **I want** tạo nhiều chi nhánh dưới 1 tenant **so that** chia sẻ danh mục thuốc/dịch vụ nhưng tách doanh thu.
- **Source:** FPT.eHospital, VNPT-HIS.
- **AC:** Bảng `his_clinic` thuộc `tenant_id`; user gán cho 1 hoặc nhiều clinic.

---

## Module 2 — Users & RBAC (7 stories)

### US-U01 — Đăng nhập JWT + refresh token [MUST]
- **As a** user **I want** đăng nhập 1 lần, refresh ngầm **so that** không bị logout giữa ca làm.
- **Source:** Best practice, mọi HIS đều có.
- **AC:** JWT TTL 15 phút, refresh 7 ngày. Refresh rotate. Logout invalidate refresh trong Redis.

### US-U02 — Tạo user với mã người hành nghề [MUST]
- **As an** Admin **I want** tạo user bác sĩ + nhập mã người hành nghề (BYT cấp) **so that** đơn thuốc gửi ĐTQG có đủ thông tin định danh.
- **Source:** TT 27/2021.
- **AC:** Trường `ma_nguoi_hanh_nghe VARCHAR(20)`; required cho role = `BacSi`.

### US-U03 — Phân quyền theo vai trò [MUST]
- **As an** Admin **I want** gán role (Admin/BacSi/LeTan/DuocSi/KeToan/KyThuatVien) **so that** mỗi user chỉ truy cập đúng tính năng được phép.
- **Source:** Tất cả HIS.
- **AC:** RBAC middleware reject 403 với endpoint sai role.

### US-U04 — Đổi mật khẩu + chính sách độ mạnh [MUST]
- **As a** user **I want** đổi mật khẩu định kỳ **so that** tài khoản an toàn.
- **AC:** Min 10 ký tự, có chữ + số + ký tự đặc biệt. Bcrypt cost 12. Force đổi sau 90 ngày.

### US-U05 — Ký số đơn thuốc bằng USB token [MUST]
- **As a** Bác sĩ **I want** ký số đơn thuốc bằng USB token cá nhân **so that** đơn có giá trị pháp lý theo TT 27.
- **Source:** TT 27/2021 Điều 3, VNPT-CA, FPT.
- **AC:** Tích hợp PKCS#11 hoặc plugin trình duyệt. Lưu signature + cert serial vào đơn.

### US-U06 — 2FA cho Admin [SHOULD]
- **As an** Admin **I want** bật 2FA TOTP **so that** tài khoản quản trị an toàn hơn.
- **AC:** Google Authenticator compatible. Backup codes.

### US-U07 — Quản lý ca làm việc / shift [SHOULD]
- **As an** Admin **I want** cấu hình ca làm + lịch trực **so that** phân BN đúng BS đang trực.
- **Source:** Vimes, FPT.

---

## Module 3 — Patient (8 stories)

### US-PT01 — Tạo hồ sơ bệnh nhân mới [MUST]
- **As a** Lễ tân **I want** tạo hồ sơ BN với CCCD + BHYT + thông tin hành chính **so that** mỗi BN có 1 mã duy nhất trong hệ thống.
- **AC:** Unique constraint trên (`tenant_id`, `cccd`). Mã BN format: `BN-{YYMM}-{seq}`. Validate định dạng CCCD 12 số.

### US-PT02 — Tìm kiếm BN nhanh [MUST]
- **As a** Lễ tân **I want** search BN bằng tên/SĐT/CCCD/mã BN **so that** tiếp đón nhanh < 30 giây.
- **AC:** Search ≤ 500ms cho 100k record. Full-text PostgreSQL trên cột tên + SĐT.

### US-PT03 — Lưu thẻ BHYT + tra cứu online [MUST]
- **As a** Lễ tân **I want** nhập số thẻ BHYT và tra cứu hiệu lực trên Cổng BHXH **so that** biết BN có được hưởng BHYT không.
- **Source:** QĐ 4750, KiotViet ⚠️, FPT/VNPT ✅.
- **AC:** API call Cổng BHXH. Lưu kết quả tra cứu (hạn thẻ, nơi đăng ký KCB) cache 24h.

### US-PT04 — Xem lịch sử khám timeline [MUST]
- **As a** Bác sĩ **I want** xem timeline khám/CLS/đơn thuốc của BN **so that** ra quyết định lâm sàng chính xác.
- **AC:** Hiển thị ≤ 50 encounter gần nhất, phân trang. Filter theo loại (khám/CLS/đơn).

### US-PT05 — Mã hóa cột nhạy cảm [MUST]
- **As a** Tenant owner **I want** CCCD/BHYT/ghi chú được mã hóa at-rest **so that** giảm rủi ro nếu DB bị lộ.
- **Source:** Best practice ATTT.
- **AC:** Column encrypted bằng AES-256-GCM, key từ Vault/env. Index trên hash thay vì plaintext.

### US-PT06 — Cập nhật/sửa thông tin BN có audit [MUST]
- **As a** Lễ tân **I want** sửa thông tin BN khi nhập sai **so that** dữ liệu chính xác.
- **AC:** Mỗi sửa đổi ghi `his_audit_log` (who/when/old/new).

### US-PT07 — Map BN sang FHIR Patient resource [SHOULD]
- **As a** integrator **I want** GET `/fhir/Patient/{id}` trả JSON FHIR R4 **so that** kết nối hệ thống ngoài dễ.
- **Source:** Pro-Diab differentiator.

### US-PT08 — Portal BN xem hồ sơ cá nhân [COULD]
- **As a** Bệnh nhân **I want** đăng nhập app xem lịch sử khám + đơn thuốc QR **so that** chủ động chăm sóc sức khỏe.
- **Source:** VNPT, FPT.

---

## Module 4 — Reception (7 stories)

### US-R01 — Tiếp đón + lấy số thứ tự [MUST]
- **As a** Lễ tân **I want** tạo lượt tiếp đón cho BN + cấp số thứ tự phòng khám **so that** phân luồng có trật tự.
- **AC:** Số reset theo ngày + theo phòng. In phiếu (PDF/máy in nhiệt).

### US-R02 — Phân phòng khám / bác sĩ [MUST]
- **As a** Lễ tân **I want** chọn phòng + bác sĩ phụ trách **so that** BN đi đúng phòng.
- **AC:** Hiển thị workload realtime (số BN đang chờ mỗi phòng).

### US-R03 — Đặt lịch khám online [MUST]
- **As a** Bệnh nhân **I want** đặt lịch qua web/app **so that** không phải xếp hàng.
- **Source:** KiotViet, Dr.Cloud, FPT chatbot.
- **AC:** Chọn ngày + khung giờ + bác sĩ. Confirm qua SMS/Zalo. Tự huỷ sau 15 phút nếu BN không đến.

### US-R04 — Nhắc lịch SMS/Zalo [SHOULD]
- **As a** Lễ tân **I want** hệ thống tự nhắc BN trước 1 ngày **so that** giảm no-show.
- **Source:** KiotViet, Dr.Cloud.

### US-R05 — Gửi trạng thái check-in lên BHYT [MUST]
- **As a** hệ thống **I want** đẩy "Bảng check-in" lên Cổng giám định khi BN đến **so that** đúng QĐ 130/4750.
- **Source:** QĐ 130 Bảng check-in.
- **AC:** Background job, retry 3 lần. Log lỗi.

### US-R06 — Theo dõi trạng thái BN realtime [MUST]
- **As a** Bác sĩ **I want** xem dashboard "BN đang chờ - đang khám - hoàn thành" **so that** điều phối ca khám.
- **Source:** Dr.Cloud, Vimes.
- **AC:** WebSocket update < 2 giây.

### US-R07 — Gọi loa BN vào phòng [SHOULD]
- **As a** Bác sĩ **I want** click gọi BN tiếp theo → loa đọc tên + số **so that** không cần đi gọi tay.
- **Source:** Dr.Cloud.

---

## Module 5 — Encounter (9 stories)

### US-E01 — Bắt đầu encounter từ lượt tiếp đón [MUST]
- **As a** Bác sĩ **I want** mở encounter từ BN đang chờ **so that** bắt đầu khám.
- **AC:** Set `status = in_progress`, `started_at = now()`. Lock cho BS khác.

### US-E02 — Nhập bệnh sử + khám lâm sàng theo template [MUST]
- **As a** Bác sĩ **I want** điền template khám (mạch, HA, nhịp thở, nhiệt độ, BMI, ghi chú) **so that** nhập nhanh.
- **AC:** Template per chuyên khoa. Nội tiết-ĐTĐ có thêm: HbA1c, đường huyết đói, BMI, vòng eo.

### US-E03 — Chẩn đoán ICD-10 multi-select [MUST]
- **As a** Bác sĩ **I want** search ICD-10 + chọn nhiều mã (1 chính + n phụ) **so that** đúng chuẩn báo cáo BYT.
- **Source:** Tất cả HIS.
- **AC:** Auto-complete ≥ 22.000 mã ICD-10 (BYT đã ban hành). Phân biệt chẩn đoán chính/phụ.

### US-E04 — Chỉ định CLS từ trong encounter [MUST]
- **As a** Bác sĩ **I want** chọn từ danh mục XN/CĐHA → tạo phiếu chỉ định **so that** BN xuống phòng CLS làm liền.
- **AC:** Mã dịch vụ chuẩn BYT (theo QĐ 4750 Bảng 3). Sinh phiếu chỉ định in được.

### US-E05 — Chỉ định thủ thuật + giá [MUST]
- **As a** Bác sĩ **I want** chọn thủ thuật từ danh mục có giá **so that** thu ngân tính đúng.

### US-E06 — Đóng encounter + ký số [MUST]
- **As a** Bác sĩ **I want** ký số khi đóng encounter **so that** EMR có giá trị pháp lý theo TT 46.
- **Source:** TT 46/2018 Điều 3.
- **AC:** Ký số bắt buộc trước khi `status = closed`. Lock không sửa được sau khi ký.

### US-E07 — Cấp giấy chứng nhận nghỉ hưởng BHXH [SHOULD]
- **As a** Bác sĩ **I want** tạo giấy nghỉ BHXH gắn vào encounter **so that** BN có chứng từ.
- **Source:** QĐ 130 Bảng 11.

### US-E08 — Cảnh báo encounter quá 12h chưa đóng [MUST]
- **As a** Admin **I want** dashboard cảnh báo encounter `in_progress > 12h` **so that** đúng TT 46 Điều 6.
- **Source:** TT 46/2018 Điều 6.

### US-E09 — Map encounter sang FHIR Encounter [SHOULD]
- **As a** integrator **I want** GET FHIR Encounter resource **so that** kết nối hệ thống ngoài.

---

## Module 6 — LabRad / CLS (7 stories)

### US-L01 — Danh mục dịch vụ CLS theo mã BYT [MUST]
- **As an** Admin **I want** import danh mục dịch vụ kỹ thuật BYT + giá BHYT **so that** dùng cho chỉ định + export 4750.
- **Source:** QĐ 4750 Bảng 3.

### US-L02 — Phòng CLS nhận phiếu chỉ định [MUST]
- **As a** Kỹ thuật viên **I want** xem danh sách phiếu CLS đang chờ **so that** thực hiện theo thứ tự.

### US-L03 — Nhập kết quả XN (giá trị + đơn vị + ngưỡng) [MUST]
- **As a** Kỹ thuật viên **I want** nhập kết quả + đánh dấu bất thường (high/low) **so that** BS đọc nhanh.
- **AC:** Cảnh báo màu đỏ khi vượt ngưỡng tham chiếu.

### US-L04 — Upload file kết quả CĐHA (PDF/DICOM/JPG) [MUST]
- **As a** Kỹ thuật viên CĐHA **I want** upload file kết quả + mô tả **so that** BS xem trong encounter.
- **AC:** Lưu MinIO. File ≤ 100MB. Antivirus scan.

### US-L05 — Trả kết quả realtime cho BS [MUST]
- **As a** Bác sĩ **I want** notification khi có kết quả CLS **so that** kết luận nhanh.

### US-L06 — Kết nối máy XN HL7/ASTM [COULD]
- **As a** KTV **I want** máy XN tự đẩy kết quả vào hệ thống **so that** không nhập tay.
- **Source:** FPT, VNPT, Vimes.

### US-L07 — Map sang FHIR Observation [SHOULD]
- **Source:** Pro-Diab differentiator.

---

## Module 7 — Prescription (10 stories)

### US-P01 — Tạo đơn thuốc từ encounter [MUST]
- **As a** Bác sĩ **I want** chọn thuốc từ danh mục + nhập liều/cách dùng/số lượng **so that** tạo đơn cho BN.
- **Source:** TT 27 Điều 5-6.
- **AC:** Required fields: tên thuốc, hàm lượng, dạng bào chế, liều, đường dùng, tần suất, số ngày, số lượng. Tự tính tổng số lượng.

### US-P02 — Đẩy đơn lên ĐTQG + nhận mã 14 ký tự [MUST]
- **As a** hệ thống **I want** call API ĐTQG ngay sau khi BS ký **so that** lấy `ma_don_thuoc` đúng format `xxxxxyyyyyyy-z`.
- **Source:** TT 27 Điều 4, 7.
- **AC:** Background job retry exponential 5 lần. Status tracking (pending/sent/confirmed/failed). Lỗi → cảnh báo Admin.

### US-P03 — Ký số đơn thuốc [MUST]
- **As a** Bác sĩ **I want** ký số đơn bằng USB token **so that** đơn có giá trị pháp lý.
- **Source:** TT 27 Điều 3.

### US-P04 — In đơn thuốc + QR code [MUST]
- **As a** Bác sĩ **I want** in đơn có QR `ma_don_thuoc` **so that** nhà thuốc quét lấy đơn nhanh.
- **Source:** Tất cả HIS.

### US-P05 — Lưu PDF + XML đơn cho lưu trữ [MUST]
- **As an** Admin **I want** mỗi đơn lưu PDF + bản gốc XML trong MinIO **so that** retention 10 năm theo TT 46.

### US-P06 — Cảnh báo tương tác thuốc [SHOULD]
- **As a** Bác sĩ **I want** cảnh báo khi 2 thuốc trong đơn có tương tác **so that** tránh sai sót.
- **Source:** FPT.

### US-P07 — Cảnh báo dị ứng thuốc của BN [SHOULD]
- **As a** Bác sĩ **I want** cảnh báo khi kê thuốc thuộc nhóm BN đã ghi nhận dị ứng **so that** an toàn.

### US-P08 — Đơn mẫu / template bác sĩ [SHOULD]
- **As a** Bác sĩ **I want** lưu đơn mẫu cho bệnh thường gặp **so that** kê nhanh.
- **Source:** Tất cả HIS.

### US-P09 — Sửa/huỷ đơn (có audit) [MUST]
- **As a** Bác sĩ **I want** huỷ đơn khi sai + tạo đơn mới **so that** đúng đơn cuối.
- **AC:** Đẩy thông báo huỷ lên ĐTQG. Audit log đầy đủ.

### US-P10 — Map sang FHIR MedicationRequest [SHOULD]

---

## Module 8 — Pharmacy (12 stories)

### US-PH01 — Danh mục thuốc + mã thuốc BYT [MUST]
- **As an** Admin **I want** import danh mục thuốc có mã BYT + hoạt chất + nhóm **so that** dùng cho kê đơn + export BHYT.
- **Source:** QĐ 4750 Bảng 2.

### US-PH02 — Nhập kho theo lô + HSD [MUST]
- **As a** Dược sĩ **I want** nhập phiếu nhập với số lô + HSD + giá nhập **so that** quản lý đúng GPP.
- **Source:** Chuẩn GPP, Sapo/MISA/Viettel PMS.
- **AC:** 1 thuốc có thể có nhiều record `(lo, hsd)`. Unique `(thuoc_id, lo)`.

### US-PH03 — Xuất kho FEFO mặc định [MUST]
- **As a** Dược sĩ **I want** hệ thống tự gợi ý lô cận date nhất khi xuất **so that** giảm thuốc hết hạn.
- **Source:** Tất cả phần mềm nhà thuốc.
- **AC:** Algorithm: order by HSD ASC, lô nào HSD < hôm nay → skip + cảnh báo.

### US-PH04 — Cảnh báo cận date (30/60/90 ngày) [MUST]
- **As a** Dược sĩ **I want** dashboard danh sách thuốc sắp hết hạn **so that** đẩy bán hoặc huỷ.

### US-PH05 — Cảnh báo tồn dưới ngưỡng [MUST]
- **As a** Dược sĩ **I want** cảnh báo khi tồn ≤ min stock **so that** nhập kịp.

### US-PH06 — Cấp phát thuốc theo đơn [MUST]
- **As a** Dược sĩ **I want** quét QR đơn thuốc → list thuốc cần cấp → xác nhận xuất kho **so that** đúng đơn.
- **AC:** Tự trừ tồn theo FEFO. In nhãn dán + hướng dẫn sử dụng.

### US-PH07 — Kiểm kê định kỳ [MUST]
- **As a** Dược sĩ **I want** tạo phiên kiểm kê + nhập số thực tế **so that** đối chiếu chênh lệch.
- **AC:** Sinh biên bản chênh lệch. Approve điều chỉnh tồn (audit).

### US-PH08 — Liên thông Dược Quốc gia [MUST]
- **As an** Admin **I want** đẩy dữ liệu nhập/xuất/tồn lên Cục QLD định kỳ **so that** tuân thủ GPP.
- **Source:** GPP, [misaeshop.vn](https://www.misaeshop.vn/23643/phan-mem-lien-thong-duoc-quoc-gia/).

### US-PH09 — Trả hàng nhà cung cấp [SHOULD]
- **As a** Dược sĩ **I want** tạo phiếu trả NCC cho lô lỗi/cận date **so that** thu hồi vốn.

### US-PH10 — Huỷ thuốc hết hạn [MUST]
- **As a** Dược sĩ **I want** tạo phiếu huỷ có biên bản **so that** đúng quy trình.

### US-PH11 — Đa kho / chuyển kho [SHOULD]
- **As an** Admin chuỗi **I want** chuyển thuốc giữa các kho **so that** cân đối tồn.

### US-PH12 — Báo cáo lãi gộp / xoay vòng tồn [SHOULD]
- **As a** Chủ phòng khám **I want** biết thuốc nào tồn lâu / lãi cao **so that** tối ưu nhập.

---

## Module 9 — Cashier (8 stories)

### US-C01 — Tính tổng viện phí từ encounter [MUST]
- **As a** Kế toán **I want** hệ thống auto tính tổng (khám + CLS + thuốc + thủ thuật) **so that** không sai sót.
- **AC:** Realtime cập nhật khi BS chỉ định thêm dịch vụ.

### US-C02 — Áp giá BHYT + % chi trả [MUST]
- **As a** Kế toán **I want** tự động tính phần BHYT chi trả + BN tự trả **so that** thu đúng.
- **Source:** QĐ 4750.
- **AC:** Map nhóm thẻ → % chi trả (80/95/100). Có override thủ công khi sai.

### US-C03 — Thanh toán QR/VNPay/Momo [SHOULD]
- **As a** Bệnh nhân **I want** thanh toán QR **so that** không cần tiền mặt.
- **Source:** Vimes, FPT.

### US-C04 — Xuất hóa đơn điện tử [MUST]
- **As a** Kế toán **I want** hệ thống tự phát hành HĐĐT kết nối TCT **so that** đúng quy định.
- **Source:** Tất cả HIS lớn.

### US-C05 — Huỷ/hoàn tiền giao dịch [MUST]
- **As a** Kế toán **I want** huỷ phiếu thu sai + phát hành lại **so that** sửa lỗi.
- **AC:** Audit log + lý do huỷ bắt buộc.

### US-C06 — Công nợ bệnh nhân [SHOULD]
- **As a** Kế toán **I want** theo dõi BN nợ + nhắc thu **so that** không thất thoát.

### US-C07 — Báo cáo doanh thu cuối ca [MUST]
- **As a** Kế toán **I want** chốt ca + in báo cáo doanh thu **so that** bàn giao ca.

### US-C08 — Phân quyền không cho BS xem giá [SHOULD]
- **As an** Admin **I want** ẩn cột giá với role BacSi **so that** tách bạch chuyên môn / tài chính.

---

## Module 10 — BHYT (7 stories)

### US-B01 — Build XML Bảng 1 (tổng hợp KCB) [MUST]
- **Source:** QĐ 4750 Bảng 1.
- **AC:** Export đúng schema XSD do BHXH cung cấp. Validate UTF-8.

### US-B02 — Build XML Bảng 2 (thuốc) [MUST]
- **Source:** QĐ 4750 Bảng 2.

### US-B03 — Build XML Bảng 3 (DVKT/CLS) [MUST]
- **Source:** QĐ 4750 Bảng 3.

### US-B04 — Build XML Bảng 4 + 5 (CĐHA, XN) [MUST]
- **Source:** QĐ 4750 Bảng 4, 5.

### US-B05 — Gửi XML lên Cổng giám định BHYT [MUST]
- **As a** hệ thống **I want** đẩy XML batch theo ngày **so that** đúng hạn giám định.
- **AC:** Retry 5 lần. Lưu response. Status: pending/accepted/rejected.

### US-B06 — Nhận phản hồi giám định + sửa lỗi [SHOULD]
- **As a** Kế toán **I want** xem lỗi giám định + sửa rồi gửi lại **so that** không bị xuất toán.

### US-B07 — Báo cáo tổng hợp BHYT theo tháng [SHOULD]
- **As an** Admin **I want** dashboard tổng BHYT chi trả/tháng **so that** dự toán dòng tiền.

---

## Module 11 — Report / BI (6 stories)

### US-RP01 — Dashboard tổng quan [MUST]
- **As a** Chủ phòng khám **I want** xem doanh thu, lượt khám, top thuốc, công nợ trên 1 màn hình **so that** ra quyết định.
- **Source:** Mọi HIS + KiotViet.

### US-RP02 — Báo cáo lượt khám theo BS / chuyên khoa [MUST]
- **As a** Quản lý **I want** so sánh KPI BS **so that** đánh giá hiệu suất.

### US-RP03 — Top thuốc bán chạy / dịch vụ [MUST]
- **As a** Dược sĩ **I want** biết top 20 thuốc bán chạy/tháng **so that** nhập đúng nhu cầu.

### US-RP04 — Báo cáo BYT định kỳ [SHOULD]
- **As an** Admin **I want** xuất báo cáo lượt khám/loại bệnh chuẩn BYT **so that** nộp Sở Y tế.

### US-RP05 — Export Excel/PDF mọi báo cáo [MUST]

### US-RP06 — BI tự định nghĩa (pivot) [COULD]

---

## Module 12 — Audit Log (3 stories)

### US-AL01 — Log mọi INSERT/UPDATE/DELETE trên BN/Encounter/Đơn [MUST]
- **Source:** TT 46/2018 Điều 9.
- **AC:** Trigger PostgreSQL ghi `his_audit_log` (user_id, action, table, record_id, old_json, new_json, ip, ua, timestamp).

### US-AL02 — UI tra cứu audit log [MUST]
- **As an** Admin **I want** filter audit log theo user/action/khoảng thời gian **so that** điều tra sự cố.

### US-AL03 — Export audit log + retention 10 năm [MUST]
- **As an** Admin **I want** export audit log Excel **so that** kiểm toán / cung cấp Sở Y tế khi yêu cầu.

---

## 12. Bổ sung từ tham chiếu SUNS Clinic Advance (22 stories)

> Source toàn bộ section: **SUNS Clinic Advance**. Xem thêm [gap-suns-clinic.md](gap-suns-clinic.md).

### US-PT09 — Upload ảnh đại diện bệnh nhân [SHOULD]
- **As a** Lễ tân **I want** upload ảnh chân dung BN khi tạo hồ sơ **so that** nhận diện nhanh khi gọi vào phòng + tránh nhầm BN trùng tên.
- **Source:** SUNS Clinic Advance.
- **AC:** Ảnh JPEG/PNG ≤ 2MB. Lưu MinIO. Hiển thị thumbnail trên màn hình tiếp đón + EMR. Có thể thay ảnh sau (audit).

### US-PT10 — Ghi chú tiếp đón sticky cross-module [SHOULD]
- **As a** Lễ tân **I want** nhập ghi chú trên form tiếp đón và hiển thị xuyên các phòng (BS, dược, thu ngân) **so that** truyền tin nhanh (vd: BN khó nghe, cần phiên dịch, BN VIP).
- **Source:** SUNS Clinic Advance.
- **AC:** Field `reception_note TEXT`. Hiển thị banner vàng ở mọi màn hình liên quan encounter hiện tại. Có timestamp + người ghi.

### US-PT11 — Lễ tân upload file kết quả CLS bên ngoài vào hồ sơ BN [MUST]
- **As a** Lễ tân **I want** chọn BN → popup nhập loại hồ sơ (text tự do) + upload file PNG/JPEG/PDF **so that** BN mang kết quả từ phòng khám khác lưu được vào hồ sơ.
- **Source:** SUNS Clinic Advance.
- **AC:** Multi-file. Lưu MinIO + bảng `his_patient_document` (patient_id, loai_ho_so, file_url, uploaded_by, uploaded_at). Antivirus scan. ≤ 20MB/file.

### US-PT12 — Danh sách & view/download file CLS upload [MUST]
- **As a** Lễ tân/BS **I want** xem danh sách file đã upload (ngày, loại hồ sơ, hình, người upload) + view/download **so that** quản lý chứng từ.
- **Source:** SUNS Clinic Advance.
- **AC:** Pagination, filter theo loại hồ sơ + khoảng ngày. Phân quyền: chỉ user cùng tenant.

### US-E10 — Tab “Kết quả CLS file Upload” trong EMR [MUST]
- **As a** Bác sĩ **I want** trong encounter có tab riêng xem tất cả file CLS upload của BN (cả từ lễ tân và KTV) **so that** không bỏ sót dữ liệu cũ.
- **Source:** SUNS Clinic Advance.
- **AC:** Hiển thị thumbnail + zoom full-screen ảnh. Sort mới nhất trước. Filter theo nguồn (lễ tân/KTV).

### US-N01 — Vai trò Điều dưỡng + form Điều dưỡng riêng [MUST]
- **As an** Admin **I want** role `DieuDuong` với form riêng (tìm kiếm BN, lọc ngày, xem lịch sử khám, nhập sinh hiệu) **so that** tách trách nhiệm với BS.
- **Source:** SUNS Clinic Advance.
- **AC:** Role mới trong RBAC. Menu “Điều dưỡng” chỉ hiển thị các chức năng được phép. Không thấy phần chẩn đoán / kê đơn.

### US-N02 — Nhập sinh hiệu nhiều lần / encounter [MUST]
- **As an** Điều dưỡng **I want** ghi nhiều record sinh hiệu trong 1 lần khám (vd: trước - sau truyền dịch) **so that** theo dõi diễn biến.
- **Source:** SUNS Clinic Advance.
- **AC:** Bảng `his_vital_sign` (encounter_id, measured_at, mach, ha_tt, ha_ttr, nhip_tho, nhiet_do, spo2, bmi, ghi_chu, recorded_by). 1-N với encounter.

### US-N03 — Nhật ký sinh hiệu theo BN [SHOULD]
- **As a** Bác sĩ **I want** xem timeline sinh hiệu xuyên các encounter của BN **so that** theo dõi xu hướng (HA, đường huyết).
- **Source:** SUNS Clinic Advance.
- **AC:** Chart line theo thời gian + bảng chi tiết. Filter theo khoảng ngày + loại chỉ số.

### US-E11 — EMR mặc định sinh hiệu mới nhất từ Điều dưỡng [MUST]
- **As a** Bác sĩ **I want** mở encounter thấy ngay record sinh hiệu mới nhất do điều dưỡng nhập **so that** không phải đo lại / hỏi lại.
- **Source:** SUNS Clinic Advance.
- **AC:** Lấy record `his_vital_sign` mới nhất theo encounter_id. Cho phép BS thêm record mới (không sửa của điều dưỡng).

### US-API01 — API public đăng ký bệnh nhân [SHOULD]
- **As a** Đối tác (App/Web/CamAI) **I want** POST `/api/public/v1/patients` với API key **so that** đẩy BN từ kênh ngoài vào HIS.
- **Source:** SUNS Clinic Advance.
- **AC:** API key per partner, rate-limit 60 req/phút. Validate trùng CCCD. Trả về `patient_id`. Audit nguồn (`source = partner_xxx`).

### US-API02 — API public đặt lịch khám [SHOULD]
- **As a** Đối tác **I want** POST `/api/public/v1/appointments` **so that** website/app đặt lịch cho BN.
- **Source:** SUNS Clinic Advance.
- **AC:** Validate khung giờ khả dụng, BS có làm việc. Trả `appointment_id`. Webhook callback khi BN huỷ.

### US-API03 — API public danh mục gói khám [SHOULD]
- **As a** Đối tác **I want** GET `/api/public/v1/health-packages` **so that** hiển thị giá + nội dung gói trên website.
- **Source:** SUNS Clinic Advance.
- **AC:** Pagination. Cache 5 phút. Bao gồm: tên gói, giá, danh sách dịch vụ con.

### US-API04 — API public danh mục dịch vụ (thuốc, vật tư, CLS) [SHOULD]
- **As a** Đối tác **I want** GET `/api/public/v1/services` filter theo type **so that** đồng bộ danh mục về app riêng.
- **Source:** SUNS Clinic Advance.
- **AC:** Type: `drug | supply | lab | imaging | procedure`. Trả mã BYT + giá.

### US-API05 — API public tra cứu nhật ký khám [SHOULD]
- **As a** Đối tác/BN **I want** GET `/api/public/v1/patients/{id}/encounters` với OTP/token BN **so that** tra cứu lịch sử khám.
- **Source:** SUNS Clinic Advance.
- **AC:** Auth bằng OTP gửi SMS BN. Token TTL 30 phút. Không expose dữ liệu BN khác.

### US-PKG01 — Quản lý gói khám sức khỏe [SHOULD]
- **As an** Admin **I want** tạo gói khám (combo dịch vụ + giá ưu đãi) **so that** bán cho khách hàng / DN.
- **Source:** SUNS Clinic Advance.
- **AC:** Bảng `his_health_package` + `his_health_package_item`. Áp dụng được khi tiếp đón → tự sinh chỉ định CLS theo gói.

### US-LIS01 — Gửi chỉ định XN sang lab thứ 3 [SHOULD]
- **As a** Bác sĩ **I want** chỉ định XN được tự động đẩy sang lab partner (Medlatec/Diag…) **so that** không phải nhập 2 lần.
- **Source:** SUNS Clinic Advance.
- **AC:** Mapping mã XN nội bộ ↔ mã lab partner. Background job push order qua REST/HL7. Trạng thái: sent/acked/result_ready.

### US-LIS02 — Nhận kết quả XN từ lab thứ 3 [SHOULD]
- **As a** hệ thống **I want** webhook nhận kết quả từ lab partner **so that** tự gắn vào encounter đúng.
- **Source:** SUNS Clinic Advance.
- **AC:** Endpoint `/api/integration/lab/{partner}/result`. Validate HMAC signature. Tự match `order_id` ↔ encounter. Notify BS.

### US-LIS03 — Danh mục đơn vị gửi mẫu (lab partner) [SHOULD]
- **As an** Admin **I want** quản lý danh sách lab partner (tên, endpoint, API key, mapping mã XN) **so that** cấu hình tích hợp.
- **Source:** SUNS Clinic Advance.
- **AC:** CRUD bảng `his_lab_partner`. Test connection. API key mã hóa AES-256.

### US-NT01 — Web Push Notification trình duyệt [MUST]
- **As a** Lễ tân/BS **I want** nhận thông báo realtime trên trình duyệt khi có đăng ký mới / kết quả CLS / đơn đẩy ĐTQG lỗi **so that** xử lý kịp thời.
- **Source:** SUNS Clinic Advance.
- **AC:** Web Push API (VAPID). Service Worker đăng ký push. Fallback toast trong app nếu BN từ chối permission. Hoạt động cả khi tab background.

### US-NT02 — Cấu hình thông báo per user (vị trí, âm thanh) [SHOULD]
- **As a** user **I want** chọn vị trí toast (góc), bật/tắt âm thanh, chọn loại sự kiện nhận thông báo **so that** không bị quấy rầy.
- **Source:** SUNS Clinic Advance.
- **AC:** Bảng `his_user_notification_setting`. UI Settings → Notification.

### US-NT03 — Cấu hình ẩn cột theo tenant (BHYT/Sổ khám/VIP/Dân tộc/Nghề nghiệp) [SHOULD]
- **As an** Admin tenant **I want** ẩn các cột không dùng (phòng khám không khám BHYT) trên list BN/tiếp đón **so that** giao diện gọn.
- **Source:** SUNS Clinic Advance.
- **AC:** Cấu hình `his_tenant_ui_config` (visible_columns JSONB). UI tự render theo config.

### US-C09 — Thanh toán thẻ Visa/Master qua gateway quốc tế [SHOULD]
- **As a** Kế toán **I want** quẹt thẻ Visa/Master qua Stripe/Onepay **so that** phục vụ khách nước ngoài / khách không dùng QR nội địa.
- **Source:** SUNS Clinic Advance.
- **AC:** Tích hợp Stripe/Onepay. Lưu transaction_id. Hỗ trợ refund. PCI-DSS: không lưu số thẻ.

### US-C10 — Tạo mã QR thanh toán động (VietQR) per hóa đơn [MUST]
- **As a** Thu ngân **I want** sinh QR VietQR động (đã bind số tiền + nội dung CK = mã hóa đơn) **so that** BN quét chuyển khoản, đối soát auto.
- **Source:** SUNS Clinic Advance.
- **AC:** Generate QR theo chuẩn VietQR. Webhook ngân hàng (Casso/MB/VCB) tự match nội dung CK với mã HĐ → cập nhật `paid`.

### US-T05 — Subdomain riêng per tenant [SHOULD]
- **As a** Tenant **I want** truy cập qua `tenphongkham.prodiab.vn` **so that** branding riêng.
- **Source:** SUNS Clinic Advance.
- **AC:** Wildcard DNS `*.prodiab.vn`. Middleware resolve `tenant_id` từ subdomain. UI hiển thị logo + tên tenant.

### US-T06 — SSL tự động cho subdomain [SHOULD]
- **As a** DevOps **I want** Let’s Encrypt wildcard cert tự renew **so that** mọi tenant subdomain có HTTPS.
- **Source:** SUNS Clinic Advance.
- **AC:** cert-manager + Cloudflare DNS-01 challenge. Auto renew 60 ngày.

### US-T07 — Quota storage per tenant + cảnh báo [SHOULD]
- **As an** Admin hệ thống **I want** giới hạn dung lượng MinIO 20GB/tenant, cảnh báo 80% **so that** kiểm soát chi phí.
- **Source:** SUNS Clinic Advance.
- **AC:** Job đếm tổng size theo `tenant_id`. Email + banner khi đạt 80%. Block upload khi vượt 100%.

### US-T08 — Subscription/billing tự động gia hạn [SHOULD]
- **As a** Tenant owner **I want** thanh toán định kỳ qua thẻ/QR + auto renew **so that** không bị gián đoạn dịch vụ.
- **Source:** SUNS Clinic Advance.
- **AC:** Bảng `his_subscription` (plan, start, end, status). Job 7 ngày trước hết hạn gửi email. Tự khóa tenant sau 7 ngày overdue (read-only).

---

## Tổng kết priority

| Priority | Số story |
|---|---|
| MUST  | 66 |
| SHOULD | 36 |
| COULD | 8 |
| **Tổng** | **110** |
