# Bộ mô phỏng phòng khám (Clinic Simulation Harness)

Mô phỏng 50 bệnh nhân đi qua UI thật của Pro-Diab HIS: **Tiếp đón → Khám → CLS → Kê đơn → Cấp
phát → Thu ngân**, rải theo 5 "ngày" (5 x 10 bệnh nhân), cộng bộ 10 kịch bản ngoại lệ vận hành
(hết thuốc, DDI, FEFO, quá tải, sai luồng trạng thái, lệch ca thu ngân...).

Toàn bộ được viết bằng Playwright + TypeScript, dùng `getByRole`/`getByLabel`/`getByText` +
CSS id thật (không có `data-testid` trong UI hiện tại).

## Yêu cầu môi trường (prerequisites)

1. Backend .NET chạy tại `http://localhost:5000` (mặc định, đổi qua biến môi trường của BE).
2. MySQL đã khởi động (`docker compose up -d mysql` hoặc tương đương) và đã áp dụng migrations.
3. Đã chạy seed **DIAB-TEST** (`tenant_id=2`) — file seed tương ứng, ví dụ `db/seeds/diab_test_tenant.sql`
   (tên file thực tế tuỳ theo trạng thái repo tại thời điểm chạy). Seed này tạo sẵn:
   - 7 tài khoản: `admin.test@diabtest.local`, `letan.test@diabtest.local`,
     `bacsi.test@diabtest.local`, `bacsi2.test@diabtest.local`, `duocsi.test@diabtest.local`,
     `ketoan.test@diabtest.local`, `ktv.test@diabtest.local` — mật khẩu mặc định `admin123`.
   - 4 phòng: "Phòng khám số 1/2/3", "Quầy thu ngân".
   - Danh mục thuốc: Metformin 500mg, Amlodipine 5mg, Atorvastatin 20mg, Paracetamol 500mg,
     Omeprazole 20mg, Gliclazide 30mg (cố tình thiếu tồn), Insulin Glargine (2 lô, 1 lô cận HSD).
   - Cặp DDI chống chỉ định (lý tưởng: Atorvastatin 20mg + Gemfibrozil).
4. Frontend Next.js chạy được ở `http://localhost:3000` (Playwright tự `npm run dev` nếu chưa có
   sẵn server, nhờ `webServer.reuseExistingServer: true` trong `playwright.sim.config.ts`).
5. Cài trình duyệt Playwright (chỉ cần Chromium — cấu hình sim chỉ khai báo project `chromium`):
   ```bash
   npx playwright install chromium
   ```

## Cách chạy

Từ thư mục `frontend/`:

```bash
# Kiểm tra danh sách test (không chạy thật) — an toàn khi BE/DB chưa sẵn sàng
npx playwright test --config=e2e/sim/playwright.sim.config.ts --list

# Chạy nhanh quy mô nhỏ (10 bệnh nhân) để kiểm tra harness còn khớp UI hay không
npm run test:sim:smoke

# Chạy đầy đủ 50 bệnh nhân + 10 kịch bản ngoại lệ
npm run test:sim
```

Xem báo cáo HTML sau khi chạy:

```bash
npx playwright show-report test-results/sim-html-report
```

## Biến môi trường

| Biến             | Mặc định                 | Ý nghĩa                                                                 |
|------------------|---------------------------|--------------------------------------------------------------------------|
| `BASE_URL`       | `http://localhost:3000`   | URL frontend cần test.                                                   |
| `SIM_PATIENTS`   | `50`                      | Số bệnh nhân mô phỏng trong `clinic-simulation.spec.ts` (rút gọn cho smoke test). |
| `SIM_PASSWORD`   | `admin123`                | Mật khẩu dùng chung cho mọi tài khoản seed DIAB-TEST.                    |
| `ADMIN_PASSWORD` | giá trị của `SIM_PASSWORD`| Override riêng mật khẩu admin nếu khác các role còn lại.                 |
| `SIM_USE_ADMIN`  | (tắt)                     | Đặt `=1` để MỌI vai trò đăng nhập bằng tài khoản admin (bypass RBAC) — dùng khi phân quyền theo role chưa đủ để chạy hết luồng (vd role lễ tân/bác sĩ chưa có quyền vào 1 màn hình nào đó). |

Ví dụ chạy full suite với admin bypass toàn bộ:

```bash
# PowerShell
$env:SIM_USE_ADMIN = "1"; npx playwright test --config=e2e/sim/playwright.sim.config.ts

# bash
SIM_USE_ADMIN=1 npx playwright test --config=e2e/sim/playwright.sim.config.ts
```

> Ghi chú: repo hiện KHÔNG cài `cross-env`, nên `test:sim:smoke` dùng script Node thuần
> (`e2e/sim/run-smoke.js`) để set `SIM_PATIENTS=10` theo cách chạy được trên cả PowerShell lẫn
> bash, thay vì cú pháp `VAR=value command` (chỉ chạy được trên POSIX shell).

## Cấu trúc thư mục

```
e2e/sim/
├── clinic-config.ts       # Hằng số: tài khoản, phòng, thuốc, cờ SIM_USE_ADMIN/SIM_PATIENTS
├── personas.ts             # 50 persona bệnh nhân (5 ngày x 10) + vài persona gắn exceptionTag
├── helpers/
│   ├── ui.ts                # Thao tác Select (base-ui)/dropdown tìm-chọn dùng chung
│   ├── session.ts            # loginAs(), seedAuthToken() (thử nghiệm), attachErrorListeners()
│   └── report.ts             # runStep() không hard-fail + saveReport() ghi JSON + screenshot
├── agents/
│   ├── reception.ts          # Lễ tân: tìm/tạo BN, tiếp đón, gọi vào khám
│   ├── doctor.ts              # Bác sĩ: tạo lượt khám, sinh hiệu, chẩn đoán, EMR, kê đơn, ký số
│   ├── pharmacist.ts          # Dược sĩ: cấp phát, từ chối, (nhập kho: SKIP có ghi chú)
│   ├── cashier.ts              # Thu ngân: mở/đóng ca, thu tiền
│   └── labtech.ts               # KTV xét nghiệm: nhập kết quả CLS (best-effort, SKIP có ghi chú)
├── clinic-simulation.spec.ts # Kịch bản chính: 5 ngày x N bệnh nhân
├── exceptions.spec.ts        # 10 kịch bản ngoại lệ độc lập
├── playwright.sim.config.ts  # Cấu hình Playwright riêng (workers:1, testMatch riêng)
└── run-smoke.js              # Wrapper Node để set SIM_PATIENTS=10 không cần cross-env
```

## Nguyên tắc thiết kế quan trọng

- **Không hard-fail giữa chừng.** Toàn bộ thao tác nghiệp vụ bọc qua `runStep()` — lỗi được ghi
  nhận là `FAIL` hoặc `SKIP` (nếu message bắt đầu `"SKIP:"`) vào báo cáo, KHÔNG throw ra ngoài để
  làm vỡ test/suite. Assert cứng (`expect`) duy nhất nằm ở bước đăng nhập sanity ban đầu của
  `clinic-simulation.spec.ts`.
- **Không dùng `data-testid`.** UI hiện tại chưa gắn testid nên mọi selector dùng
  `getByRole`/`getByLabel`/`getByPlaceholder`/`getByText` (có regex tiếng Việt khi cần) hoặc CSS
  id thật đã xác minh trong source (`#full_name`, `#enc-patient-search`, `#v-hr`,
  `#diag-code-0`, `[data-slot="card"]`, `.ProseMirror`...).
- **Chạy tuần tự.** `playwright.sim.config.ts` đặt `workers: 1`, `fullyParallel: false` vì các vai
  trò dùng chung hàng đợi tiếp đón, tồn kho dược, ca thu ngân — chạy song song sẽ gây xung đột dữ
  liệu giả.
- **Báo cáo JSON.** `saveReport()` ghi ra `test-results/clinic-sim-report.json` (kịch bản chính)
  và `test-results/clinic-sim-exceptions-report.json` (kịch bản ngoại lệ), kèm tổng hợp
  PASS/FAIL/SKIP theo ngày và theo bệnh nhân. Screenshot mỗi bước không-PASS lưu ở
  `test-results/sim-shots/`.

## Các điểm SKIP/giả định đã biết (do giới hạn khảo sát UI hiện tại)

Những điểm này được ghi chú trực tiếp trong code (message bắt đầu `SKIP:`) để không làm vỡ suite:

1. **Nhập kho bổ sung** (`PharmacistAgent.restock`) — chưa xác định được selector ổn định cho
   WarehouseTab/AdjustmentTab tại `/pharmacy` trong phạm vi khảo sát này → luôn SKIP.
2. **Nhập kết quả CLS theo đúng bệnh nhân** (`LabTechAgent.enterResults`) — màn `/labrad` hiện
   quản lý theo danh sách kết quả toàn hệ thống, chưa có bước lọc/khớp rõ ràng theo lượt khám cụ
   thể qua UI → mở được form nhưng SKIP phần nhập chi tiết.
3. **Xác thực FEFO qua UI** — `DispenseConfirmDialog` chỉ hiển thị tên thuốc + số lượng, không lộ
   số lô/HSD cụ thể để kiểm chứng backend đã chọn đúng lô cận hạn → SKIP, cần đối chiếu qua API/DB.
4. **Nhập số thẻ BHYT không hợp lệ khi tạo bệnh nhân** — tab "Bảo hiểm y tế" trong form tạo mới
   (`PatientBhytTab`) hiện là placeholder (chưa có input), BHYT được ghi chú là "thêm sau qua hồ
   sơ bệnh nhân" → SKIP ở luồng tạo mới.
5. **Khoá tài khoản hẳn / rate-limit 429** — cần trigger từ backend/DB hoặc tải trực tiếp ở tầng
   API, không có thao tác UI tương ứng → SKIP, chỉ kiểm tra được phần đăng nhập sai mật khẩu.
6. **Cặp thuốc DDI chống chỉ định** — ưu tiên dùng `Atorvastatin 20mg + Gemfibrozil` theo đề bài;
   nếu `Gemfibrozil` không có trong danh mục đã seed, harness tự động thử `DDI_PAIR_FALLBACK`
   (`Metformin 500mg + Amlodipine 5mg`). Nếu cả 2 cặp đều không được seed ở mức
   `CONTRAINDICATED`, bước xác nhận khoá ký đơn sẽ SKIP kèm ghi chú thay vì fail cứng.
7. **`seedAuthToken()` trong `helpers/session.ts`** là tiện ích THỬ NGHIỆM, KHÔNG dùng mặc định —
   `loginAs()` (điền form UI thật) là đường chạy chính theo đúng yêu cầu đề bài.
