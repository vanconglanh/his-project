# Release Checklist — Pro-Diab HIS

> Danh sách việc **thực hiện tại thời điểm triển khai lên server thật** (staging/production) — không phải việc sửa code. Đây là checklist vận hành, tách khỏi task list phát triển tính năng (`docs/TASKLIST-20260829.md`). Cập nhật lại khi có thay đổi hạ tầng.

Nguồn tổng hợp: `docs/qc/go-live-readiness-20260830.md` (audit 30/08/2026) + thảo luận bổ sung cùng ngày.

---

## 🔴 Bắt buộc trước lần deploy đầu tiên lên server thật

| # | Việc | Vì sao bắt buộc | Trạng thái |
|---|---|---|---|
| R-1 | Sinh **mới** toàn bộ secret: `JWT_SECRET`, `Encryption:MasterKey`, `Encryption:BlindIndexKey`, mật khẩu MySQL/MinIO/Redis — **không dùng lại giá trị dev** | Giá trị dev đã lộ trong repo/lịch sử làm việc, không an toàn cho prod | 🔲 |
| R-2 | Backup **2 khoá mã hoá** (`MasterKey`, `BlindIndexKey`) vào nơi lưu trữ an toàn, tách biệt khỏi backup DB thông thường (vd password manager/vault riêng cho hạ tầng) | Mất khoá = mất khả năng đọc CCCD/dữ liệu sức khoẻ + mất khả năng tìm kiếm vĩnh viễn, không cách nào khôi phục | 🔲 |
| R-3 | Set `Minio:PublicEndpoint` đúng domain thật (không để rỗng) | Thiếu → mọi link file (CLS, ảnh, PDF InBody...) không mở được từ trình duyệt người dùng thật | 🔲 |
| R-4 | Cấu hình HTTPS/TLS thật (Let's Encrypt hoặc chứng chỉ đã mua) + xác nhận Nginx enforce HTTPS, có HSTS/CSP header theo đúng CLAUDE.md | Bắt buộc pháp lý cho dữ liệu y tế, chưa xác nhận đã cấu hình đúng | 🔲 |
| R-5 | Chạy thử **backup MySQL** + **restore thử trên môi trường riêng** ít nhất 1 lần, xác nhận restore ra dữ liệu đúng | Backup chưa test = không phải backup thật | 🔲 |
| R-6 | Thêm Docker `healthcheck` cho các service chính (backend/frontend/mysql/redis/minio) | Không có → container die không tự phát hiện được, phải canh log tay | 🔲 |
| R-7 | Migration DATA (không tự chạy lúc khởi động app): `dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx` — chạy 1 lần trên DB đích trước khi mở cho user dùng | Nếu bỏ qua, tìm kiếm bệnh nhân cũ theo CCCD/SĐT sẽ trượt (đã fix code, nhưng data cũ trên server thật vẫn cần backfill riêng) | 🔲 |
| R-8 | Điền `.env` monitoring (`ops/monitoring/.env`): SMTP thật để Alertmanager gửi được cảnh báo email | Cơ chế đã sẵn sàng (envsubst), chỉ cần điền giá trị | 🔲 |
| R-9 | Reverse proxy + xác thực cho Grafana (hiện đang public thẳng port, chưa có auth) | Dashboard có thể lộ thông tin vận hành nhạy cảm nếu không khoá | 🔲 |
| R-10 | **Xác nhận OCR chạy trong image production** (tính năng Nhập hồ sơ cũ / legacy-import): image backend đã cài `libtesseract5` + `libleptonica6` (đã thêm vào `backend/Dockerfile`) và `tessdata` (eng/vie) có trong `/app/tessdata`. Sau deploy, chạy 1 lần upload ZIP ảnh test → xác nhận batch chuyển `done` (không `failed`). | NuGet `Tesseract` chỉ bundle native cho Windows; trên Linux nếu thiếu native lib hoặc symlink `.so` thì OCR crash lúc runtime. Đã fix Dockerfile nhưng **chưa build/verify được image Linux thật** trong phiên (verify chạy trên Windows) → cần smoke test sau deploy | 🔲 |
| R-11 | **Kiểm tra Hangfire worker nghe đủ queue**: `AddHangfireServer` nay cấu hình `Queues = { "default", "bhyt", "ocr" }`. Trước đây server chỉ nghe `default` → job đẩy vào queue `bhyt` (export XML BHYT) có thể chưa từng được xử lý. Sau deploy, kiểm bảng `hangfire_JobQueue` không tồn đọng job ở queue non-default. | Bug tiềm ẩn đã vá; môi trường dev hiện KHÔNG có job BHYT nào treo (đã query: 0 job Enqueued, 1777 job đều Succeeded) nhưng cần theo dõi trên server thật nếu tính năng BHYT đã dùng | 🔲 |

## 🟡 Nên làm sớm sau khi go-live đợt đầu (không chặn lần deploy đầu tiên)

| # | Việc | Ghi chú |
|---|---|---|
| R-19 | CI pipeline chạy `dotnet test`/`tsc` tự động trên mỗi PR trước khi merge vào `develop` | Hiện đang build/test thủ công trước khi push — dễ sót nếu quên chạy |
| R-20 | Xác nhận rate limit (100 req/phút/user, 1000/phút/tenant theo CLAUDE.md) đã enforce thật ở backend, không chỉ là con số trong tài liệu | |
| R-21 | Load test cơ bản — mô phỏng nhiều chi nhánh thao tác đồng thời giờ cao điểm | Chưa có số liệu về giới hạn chịu tải thực tế |
| R-22 | Viết runbook sự cố ngắn (DB đầy disk, lỗi 5xx tăng đột biến, container crash-loop) — ai làm gì khi Alertmanager báo | Có Grafana/Alertmanager rồi, cần người biết phản ứng |
| R-23 | Kênh hỗ trợ khách hàng khi phòng khám gặp lỗi | Chưa định nghĩa |

## 🟢 Vận hành dài hạn, không gấp

| # | Việc | Ghi chú |
|---|---|---|
| R-24 | Chiến lược archive dữ liệu cũ (không xoá, nhưng chuyển sang lưu trữ lạnh để DB không phình vô hạn) — luật yêu cầu giữ 10-20 năm, không bắt buộc giữ trong DB hoạt động | |
| R-25 | Điều khoản dịch vụ + luồng consent dữ liệu cá nhân theo Luật BVDLCN 2025 | Đã có mã hoá kỹ thuật, còn thiếu quy trình pháp lý/UI đồng ý |
| R-26 | Cấu hình email nhắc lịch hẹn thật (eSMS/Zalo ZNS credential) qua `/admin/notification-channels` | Cơ chế đã có sẵn, chỉ cần nhập key thật khi có |
| R-27 | Xác nhận endpoint diaB có sẵn để cắm `IExternalPathwayProvider` thật | Phụ thuộc bên ngoài, cần làm việc với team diaB |

---

## Đã hoàn tất (tham chiếu, không cần làm lại)

- 2 P0 blocker bảo mật/dữ liệu (2FA verify TOTP thật, backfill blind-index CCCD) — `docs/qc/go-live-readiness-20260830.md` mục 3.
- Log tập trung Loki/Grafana + dashboard user/product analytics — `docs/ops/log-monitoring-loki-grafana.md`.
- Alertmanager email alerting (chờ điền SMTP thật — xem R-8).
- Toàn bộ tính năng nghiệp vụ trong `docs/TASKLIST-20260829.md` mục A→N.

---

*Checklist này không thay thế `docs/TASKLIST-20260829.md` (task list phát triển tính năng) — đây là checklist riêng cho hành động triển khai/vận hành thật.*
