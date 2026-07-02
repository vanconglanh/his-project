# Legal Compliance Checklist — Pro-Diab HIS

> Trích yếu các văn bản pháp luật BYT liên quan trực tiếp đến data model & feature của Pro-Diab HIS.
> Mỗi điều khoản → mapping module/feature/story.
>
> Nguồn:
> - TT 27/2021/TT-BYT — [thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/The-thao-Y-te/Thong-tu-27-2021-TT-BYT-ke-don-thuoc-bang-hinh-thuc-dien-tu-497939.aspx) | [luatvietnam.vn](https://luatvietnam.vn/y-te/thong-tu-27-2021-tt-byt-bo-y-te-214399-d1.html) | [vanban.chinhphu.vn](https://vanban.chinhphu.vn/default.aspx?pageid=27160&docid=204814)
> - TT 04/2022/TT-BYT (sửa đổi TT 27, đẩy lộ trình) — [trogiupluat.vn](https://trogiupluat.vn/y-te/thong-tu-04-2022-tt-byt-sua-doi-thong-tu-52-2017-tt-byt-thong-tu-18-2018-tt-byt-va-thong-tu-27-2021--26052.html)
> - QĐ 130/QĐ-BYT (18/01/2023) — [thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/The-thao-Y-te/Quyet-dinh-130-QD-BYT-2023-chuan-du-lieu-dau-ra-phuc-vu-quan-ly-chi-phi-kham-chua-benh-551553.aspx) | [tài liệu tập huấn](https://file.medinet.gov.vn//data/soytehcm/trungtamytehocmon/attachments/2023_3/tai_lieu_dao_dao_tap_huan_ban_hanh_kem_quyet_dinh_130-qd-byt_21320239.pdf)
> - QĐ 4750/QĐ-BYT (29/12/2023) — [thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/Cong-nghe-thong-tin/Quyet-dinh-4750-QD-BYT-2023-sua-doi-Quyet-dinh-130-QD-BYT-dinh-dang-du-lieu-kham-benh-593340.aspx) | [vbpl ts24](https://vbpl.ts24.com.vn/support/solutions/articles/16000175924-quyết-định-4750-qđ-byt-ngày-29-12-2023-sửa-đổi-quyết-định-130-qđ-byt-quy-định-về-chuẩn-và-định-dạng-d)
> - QĐ 3176/QĐ-BYT (29/10/2024) - sửa đổi tiếp QĐ 4750 — [luatvietnam.vn](https://luatvietnam.vn/y-te/quyet-dinh-3176-qd-byt-2024-sua-doi-quyet-dinh-4750-qd-byt-sua-doi-quy-dinh-chuan-du-lieu-dau-ra-370146-d1.html)
> - TT 46/2018/TT-BYT — Bệnh án điện tử — [thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/Cong-nghe-thong-tin/Thong-tu-46-2018-TT-BYT-su-dung-va-quan-ly-ho-so-benh-an-dien-tu-391438.aspx) | [DrVIP — 10 điểm mấu chốt](https://drvip.vn/10-diem-mau-chot-trong-thong-tu-46-2018-tt-byt-ve-benh-an-dien-tu/)

---

## 1. TT 27/2021/TT-BYT — Kê đơn thuốc điện tử

| Điều khoản | Nội dung trích yếu | Mapping Pro-Diab |
|---|---|---|
| Điều 1, 2 | Phạm vi: kê đơn điện tử tại CSKCB có giấy phép. Đối tượng: bác sĩ có mã người hành nghề | Module **Users & RBAC**: trường `ma_nguoi_hanh_nghe`; **Tenant**: trường `ma_cskcb` (US-T01, US-U02) |
| Điều 3 | Đơn thuốc điện tử có giá trị pháp lý như đơn giấy nếu được tạo - hiển thị - **ký số** - chia sẻ - lưu trữ điện tử | Module **Prescription**: bắt buộc ký số (US-P03), lưu PDF + bản XML gốc (US-P05) |
| Điều 4 | Mã đơn thuốc: 14 ký tự `xxxxxyyyyyyy-z` do Hệ thống ĐTQG cấp tự động | Module **Prescription**: trường `ma_don_thuoc VARCHAR(14)`, gọi API ĐTQG để cấp (US-P02) |
| Điều 5-6 | Nội dung đơn phải đầy đủ: thông tin BN, chẩn đoán ICD-10, danh mục thuốc (tên, hàm lượng, dạng bào chế, liều dùng, đường dùng, số lượng) | Module **Prescription**: form data model + AC validate `NOT NULL` các field (US-P01) |
| Điều 7 | CSKCB phải gửi đơn lên **Hệ thống Đơn thuốc Quốc gia** ngay khi kết thúc khám (ngoại trú) hoặc trước khi ra viện (nội trú) | Module **Integration ĐTQG**: background job retry (US-P02, US-P04) |
| Điều 8 | Lưu trữ đơn điện tử tối thiểu theo quy định lưu trữ bệnh án | Module **Audit Log / Storage**: soft delete + retention policy ≥ 10 năm |

**Lộ trình (TT 04/2022 sửa đổi):** bệnh viện hạng I, hạng đặc biệt từ 30/06/2022; còn lại từ 30/06/2023.

---

## 2. QĐ 130/QĐ-BYT (2023) + QĐ 4750/QĐ-BYT (2023) — Chuẩn XML BHYT

| Bảng dữ liệu | Nội dung | Mapping Pro-Diab |
|---|---|---|
| Bảng check-in | Trạng thái KCB realtime (BN đến, đang khám, ra viện) gửi lên Cổng giám định | Module **Reception** + **Encounter**: webhook/job đẩy trạng thái (US-R05) |
| Bảng 1 | Chỉ tiêu **tổng hợp KCB** (mã LK, thông tin BN, chẩn đoán, tổng chi phí, BHYT chi trả) | Module **BHYT**: builder XML từ encounter + cashier (US-B01) |
| Bảng 2 | Chi tiết **thuốc** đã sử dụng | Module **Pharmacy** + **Prescription**: lưu `ma_thuoc_BYT`, `don_gia_BHYT` (US-B02) |
| Bảng 3 | Chi tiết **dịch vụ kỹ thuật / CLS** | Module **LabRad** + danh mục dịch vụ BYT (US-B03) |
| Bảng 4 | Chi tiết **CĐHA, thăm dò chức năng** (kết quả) | Module **LabRad** (US-B04) |
| Bảng 5 | Chi tiết **CLS xét nghiệm** (kết quả) | Module **LabRad** (US-B04) |
| Bảng 11 | Giấy chứng nhận nghỉ hưởng BHXH | Module **Encounter**: tạo giấy nghỉ BHXH (US-E07) — SHOULD |
| Bảng 12 | Giám định y khoa | 🔵 Out of MVP |

**Mốc bắt buộc:** từ **01/07/2024**, CSKCB phải gửi/nhận XML theo định dạng tại QĐ 4750 (Bảng 1-5) để được giám định và thanh toán BHYT ([thuvienphapluat.vn](https://thuvienphapluat.vn/van-ban/Cong-nghe-thong-tin/Quyet-dinh-4750-QD-BYT-2023-sua-doi-Quyet-dinh-130-QD-BYT-dinh-dang-du-lieu-kham-benh-593340.aspx)).

**Encoding:** UTF-8, format XML.

---

## 3. TT 46/2018/TT-BYT — Hồ sơ bệnh án điện tử (EMR)

| Điều khoản | Trích yếu | Mapping Pro-Diab |
|---|---|---|
| Điều 1 | Phạm vi: tạo - sử dụng - quản lý EMR tại CSKCB có giấy phép | Module **Encounter** = đơn vị EMR |
| Điều 3 | EMR có giá trị pháp lý như bệnh án giấy nếu được ký số và lưu trữ đúng chuẩn | Ký số bác sĩ trên encounter khi đóng (US-E06) |
| Điều 6 | **EMR phải được cập nhật trong vòng 12 giờ** kể từ khi có y lệnh; tối đa 24 giờ nếu sự cố IT | AC: timestamp `updated_at` + cảnh báo encounter quá hạn (US-E08) |
| Điều 7 | Lưu trữ: phần mềm đạt chuẩn nâng cao, dung lượng đủ, có backup ở DC đạt chuẩn BTTTT | Hạ tầng DevOps: backup MinIO + PostgreSQL daily; checklist DR |
| Điều 8 | Phần mềm EMR phải có đủ chức năng theo Bảng VIII của TT 54/2017 (quản lý dịch vụ y tế, hành chính, hồ sơ bệnh án) | Đối chiếu với feature matrix - đảm bảo coverage |
| Điều 9 | Phải có **audit log** mọi thao tác đọc/ghi/sửa/xoá trên EMR, lưu đủ thông tin user + timestamp | Module **Audit Log**: bảng `his_audit_log`, trigger PostgreSQL (US-AL01) |

---

## 4. (Bonus) Liên thông Dược Quốc gia — yêu cầu Cục QLD

| Yêu cầu | Mapping Pro-Diab |
|---|---|
| Phần mềm nhà thuốc đạt chuẩn GPP, kết nối CSDL Dược Quốc gia ([misaeshop.vn](https://www.misaeshop.vn/23643/phan-mem-lien-thong-duoc-quoc-gia/), [sapo.vn](https://www.sapo.vn/blog/cac-phan-mem-ket-noi-co-so-du-lieu-duoc-quoc-gia)) | Module **Pharmacy**: API liên thông xuất/nhập, quản lý theo Lô + HSD bắt buộc (US-PH02, US-PH04) |
| Báo cáo nhập/xuất/tồn định kỳ về Sở Y tế | Module **Report**: report định kỳ + export XML |

---

## 5. Compliance summary — Bắt buộc cho MVP

| # | Yêu cầu | Module chính | Priority |
|---|---|---|---|
| 1 | Ký số đơn thuốc + gửi ĐTQG (TT 27 Điều 3, 4, 7) | Prescription | **MUST** |
| 2 | Export XML 4750 Bảng 1-5 (QĐ 4750 hiệu lực 01/07/2024) | BHYT | **MUST** |
| 3 | Audit log đầy đủ (TT 46 Điều 9) | Audit Log | **MUST** |
| 4 | Cập nhật EMR ≤ 12h (TT 46 Điều 6) | Encounter | **MUST** |
| 5 | Quản lý lô + HSD + liên thông Dược QG (GPP) | Pharmacy | **MUST** |
| 6 | Mã CSKCB + mã người hành nghề (TT 27) | Tenant, Users | **MUST** |
| 7 | Mã hóa cột nhạy cảm (best practice + chuẩn ATTT BYT) | Patient | **MUST** |
| 8 | Hóa đơn điện tử kết nối TCT | Cashier | **MUST** (sau khi có doanh thu thực tế) |
