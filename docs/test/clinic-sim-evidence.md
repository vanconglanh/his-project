# Bằng chứng kiểm thử — Phòng khám mô phỏng (DIAB-TEST)

> Biểu mẫu ghi kết quả cho bộ giả lập 50 bệnh nhân qua UI (Playwright).
> Tester (Phượng) điền các ô `___` sau mỗi lần chạy. Đồng bộ số liệu sang bảng đếm trong
> [docs/status/lo-trinh.html](../status/lo-trinh.html) (mục **C · Tiến độ kiểm thử**).

- **Tenant test:** `DIAB-TEST` (tenant_id = 2) — tách biệt tenant demo `DIAB-HCM` (id = 1).
- **Kịch bản gốc:** mục A/B trong `docs/status/lo-trinh.html`.
- **Harness:** `frontend/e2e/sim/` · **Seed:** `db/seeds/diab_test_tenant.sql`.

---

## 1. Điều kiện tiên quyết (devops dựng)

| Thành phần | Lệnh / ghi chú | Trạng thái |
|---|---|---|
| Docker + MySQL 8 + Redis + MinIO | `docker compose -f ops/docker-compose.yml up -d mysql redis minio` | ___ |
| Áp migration 9xxx | qua service `migrator` (compose) hoặc `ops/scripts/apply-migrations.sh` | ___ |
| Seed tenant test | áp `db/seeds/diab_test_tenant.sql` vào DB `prodiab_his` | ___ |
| Backend .NET `:5000` | `dotnet run` (ASPNETCORE_URLS=http://localhost:5000) | ___ |
| Frontend `:3000` | `npm run dev` (webServer của Playwright tự lo nếu chưa chạy) | ___ |
| Playwright browser | `npx playwright install chromium` | ___ |

**Kiểm chứng seed nhanh** (kỳ vọng users=7, rooms=4, drugs=8, patients=15):
```sql
SELECT COUNT(*) FROM diab_his_sec_users   WHERE tenant_id = 2;
SELECT COUNT(*) FROM diab_his_sys_rooms    WHERE tenant_id = 2;
SELECT COUNT(*) FROM diab_his_pha_drugs    WHERE tenant_id = 2;
SELECT COUNT(*) FROM diab_his_pat_patients WHERE tenant_id = 2;
```

## 2. Lệnh chạy (tester)

```bash
cd frontend
# Smoke 10 bệnh nhân trước
SIM_PATIENTS=10 npm run test:sim            # hoặc: npm run test:sim:smoke
# Nếu PASS → chạy full 50 bệnh nhân + ngoại lệ
npm run test:sim
```
Biến môi trường: `BASE_URL` (mặc định http://localhost:3000), `ADMIN_PASSWORD`,
`SIM_USE_ADMIN=1` (chạy toàn bộ bằng tài khoản admin nếu quyền theo vai trò chưa đủ).
Report JSON: `frontend/test-results/clinic-sim-report.json`; ảnh: `test-results/sim-shots/`.

## 3. Kết quả — Luồng chuẩn (50 bệnh nhân)

| Ngày | Số BN | PASS | FAIL | SKIP | Ghi chú |
|---|---|---|---|---|---|
| Ngày 1 | 10 | ___ | ___ | ___ | ___ |
| Ngày 2 | 10 | ___ | ___ | ___ | ___ |
| Ngày 3 | 10 | ___ | ___ | ___ | ___ |
| Ngày 4 | 10 | ___ | ___ | ___ | ___ |
| Ngày 5 | 10 | ___ | ___ | ___ | ___ |
| **Tổng** | **50** | **___** | **___** | **___** | |

## 4. Kết quả — 10 kịch bản ngoại lệ

| # | Ngoại lệ | Mã lỗi kỳ vọng | Thực tế | Kết quả |
|---|---|---|---|---|
| 1 | Hết thuốc | 422 `PHARMACY_STOCK_INSUFFICIENT` | ___ | ___ |
| 2 | Nhập kho (PO+GRN) | tồn tăng | ___ | ___ |
| 3 | Cận HSD / FEFO | alert near-expiry, FEFO đúng lô | ___ | ___ |
| 4 | Bác sĩ bận / quá tải | alert over-12h, waiting cao | ___ | ___ |
| 5 | Tương tác thuốc (DDI) | 409 `PRESCRIPTION_DDI_BLOCKED` | ___ | ___ |
| 6 | Thẻ BHYT lỗi | 400 `BILLING_INVALID_BHYT` | ___ | ___ |
| 7 | Sai luồng trạng thái | `ENCOUNTER_INVALID_TRANSITION` / `BILLING_ALREADY_FINALIZED` | ___ | ___ |
| 8 | Đóng ca lệch tiền | 422 `CASHIER_CASH_DIFFERENCE` | ___ | ___ |
| 9 | Tài khoản bị khoá | user `LOCKED` | ___ | ___ |
| 10 | Quá tải hệ thống | 429 `RATE_LIMIT_EXCEEDED` | ___ | ___ |

## 5. Kiểm tra cách ly multi-tenant (qc)

- [ ] Đăng nhập tài khoản tenant 1 (`admin@prodiab.local`) → **không** thấy BN/đơn/hóa đơn của tenant 2 (mã `BNT*`, đơn DIAB-TEST).
- [ ] Đăng nhập tenant 2 (`admin.test@diabtest.local`) → chỉ thấy dữ liệu DIAB-TEST.
- [ ] Không có bản ghi cross-tenant trong `diab_his_sec_audit_logs` (cột `cross_tenant_attempt`).

## 6. Lỗi phát hiện (chuyển backend/Thảo)

| ID | Bước | Mô tả | Mức độ | Trạng thái |
|---|---|---|---|---|
| ___ | ___ | ___ | ___ | ___ |

## 7. Kết luận

- Ngày chạy: ___ · Người chạy: ___ · Môi trường: ___
- Kết luận tester: ☐ PASS ☐ FAIL — ghi chú: ___
- QC (Chi): ☐ APPROVE ☐ BLOCK — ghi chú: ___

---

---

## 8. KẾT QUẢ CHẠY THỰC TẾ (02/07/2026)

**Môi trường đã dựng** (máy không có Docker → MySQL 8.0.39 portable): MySQL `:3306` (seed dump+migration+DIAB-TEST), backend `:5000` (roll-forward .NET 10, Redis tắt, CORS +:3100), frontend `:3100`, Chromium. Đăng nhập 6 vai trò OK.

**Luồng lâm sàng (6 BN, admin-bypass): `Total 103 | PASS 87 | FAIL 1 | SKIP 15`.** 6/6 BN đi trọn: tiếp đón → khám → sinh hiệu → chẩn đoán → bệnh án → **ký số → đóng lượt khám → kê đơn → ký & gửi ĐTQG → mở ca thu ngân** đều PASS.

**Cấp phát/thu tiền: SKIP** — module Dispensing xây trên khóa INT lệch GUID (bug #6), đơn không xuất hiện trong hàng chờ phát. Cần rework riêng.

### Bug thật do mô phỏng phát hiện (giá trị cốt lõi của bài test)

| # | Bug | Ảnh hưởng | Trạng thái |
|---|---|---|---|
| 1 | Thiếu view `sec_permissions`/`sec_role_permissions` (9009 bỏ sót) → JwtService nuốt lỗi → token role thật mất claim quyền | Mọi vai trò thật 403 | ✅ Fixed — mig `9022` |
| 2 | Check-in cột `queue_number` không tồn tại; `patient_id` sai kiểu INT→GUID | Tiếp đón 500 | ✅ Fixed — mig `9023` + code |
| 3 | `/drugs/search` trỏ view cũ thiếu `name_vi`; seed `name_vi` rỗng | Không kê được thuốc 500 | ✅ Fixed — code + data |
| 4 | `/reception/queue` cùng lỗi `queue_number` | Hàng đợi 500 | ✅ Fixed — code |
| 5 | `drug_id` lệch kiểu (GUID vs INT) + cột `prescription_items` INT | Thêm dòng thuốc 400/500 | ✅ Fixed — mig `9024` + code |
| 6 | Module **Dispensing** xây trên khóa INT trong khi DB dùng GUID | Cấp phát/thu tiền chưa chạy | ✅ Fixed (03/07) — mig `9025` (6 cột INT→CHAR36/VARCHAR36 trên dispense_records/dispense_items/stock_movements) + code Dispensing/FEFO sang khóa GUID; build BE pass + 56 test pharmacy PASS. Chờ áp 9025 lên DB live + re-run E2E |
| + | Mã permission seed (`reception.create`) lệch mã controller (`reception.checkin`) | RBAC role thật | ⚠️ Cần rà soát seed |

> **Ghi chú:** Chạy admin-bypass (`SIM_USE_ADMIN=1`) để kiểm luồng nghiệp vụ trong khi mã permission seed đang được rà soát. File đổi khi dựng env: `Program.cs` (CORS dev), `appsettings.Development.json` (Redis off + CORS), + các migration `9022/9023/9024` và fix handler Reception/Drugs/Prescription. Full 50 BN + 10 ngoại lệ chạy sau khi fix bug #6.
