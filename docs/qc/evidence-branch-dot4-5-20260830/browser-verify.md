# Evidence verify BROWSER — Đợt 4 + 5 đa chi nhánh (2026-08-30)

Stack local: frontend :3000, backend :5000 (image rebuild sau toàn bộ commit Đợt 4+5 + 3 fix),
MySQL thật. Đăng nhập thật qua form (qc.admin@prodiab.test / Test@123, role admin — S3 cross_view).

## 1. Dashboard chuỗi chi nhánh — `/reports/chain-dashboard` (BR-90/91/92, US-6.1)
- Banner phạm vi dữ liệu: **"Dữ liệu: x/y chi nhánh — <tên các CN>"** hiển thị đúng (BR-92).
- Bảng xếp hạng chi nhánh: cột Hạng, Chi nhánh, Doanh thu, Lượt khám, Doanh thu/lượt, BN mới,
  % thay đổi (mũi tên xanh/đỏ). Dữ liệu thật từ DB (CN1: 983.425đ / 33 lượt / 22 BN mới / +2358.6%).
- Bar chart "Top chi nhánh theo doanh thu".
- **Drill-down (BR-91)**: click 1 dòng chi nhánh → bung bảng bác sĩ của CN đó với cùng bộ chỉ số
  (BS. Nguyễn Văn An 910.000đ/19 lượt/47.895đ, BS. Test Demo, QC Admin Test). OK.

## 2. Tuân thủ BHYT theo chi nhánh — tab trong `/bhyt` (BR-107)
- Tab "Tuân thủ theo chi nhánh": bảng mỗi chi nhánh với cột Mã CSKCB / Khám BHYT / Hợp đồng còn
  hiệu lực / ĐTQG / Token, icon ✓ (xanh) ✗ (đỏ). CN1 có CSKCB (✓), chưa bật BHYT/hợp đồng (✗) —
  khớp dữ liệu DB.

## 3. Quản lý chi nhánh — `/admin/branches` (BR-08/110/111)
- Hiển thị đủ **3 chi nhánh** (sau fix scope entitlement) với **badge trạng thái**:
  CN-CLONE-TEST = "Nháp" (DRAFT), CN02 = "Hoạt động" (ACTIVE), MAIN = "Mặc định" + "Hoạt động".
- Nút "Nhân bản" (clone) + "Tạo chi nhánh" + row actions (clone / checklist go-live / bật-tắt).
- Chi nhánh DRAFT (clone test) hiển thị cho admin (BR-110: chỉ user có branch.create/update thấy DRAFT).

## 4. Chuyển cơ sở nội bộ — `/encounters/referrals` (BR-29)
- Màn "Chuyển cơ sở nội bộ": nút "Giới thiệu sang cơ sở khác" + bảng danh sách referral
  (Bệnh nhân, Chi nhánh nguồn, Lý do, Trạng thái badge "Đã gửi", Ngày tạo, Thao tác "Tiếp nhận"/"Huỷ").
- Dữ liệu thật: referral "Trần Văn Bình" CN DiaBetis HCM → (tạo qua API test), trạng thái SENT.

## Lỗi phát hiện qua verify & đã fix trong đợt này
1. **BR-85 trả nợ chéo** trả `BILLING_NOT_FOUND` do global branch query filter trên Billing →
   fix `IgnoreQueryFilters()` (commit 193936c). Verify lại E2E: sinh đúng bút toán công nợ nội bộ.
2. **BR-111 clone** lỗi 500 do trùng unique (tenant, code) khi copy phòng/kho → fix suffix code
   `-B{newId}` (commit 57d1701). Verify lại: clone OK, DRAFT, không copy nhân sự/tồn kho.
3. **Màn READ cross-branch** (dashboard chuỗi / quản lý CN / tuân thủ BHYT) chỉ thấy 1 CN khi user
   đã chọn 1 chi nhánh (X-Branch-Id) → fix scope theo quyền `branch.cross_view` (commit 4bf31af).
   Verify lại browser: `/admin/branches` hiện đủ 3 CN; ranking meta 3/3.
