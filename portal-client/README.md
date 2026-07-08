# Portal Client — Cổng bệnh nhân (PWA)

Ứng dụng web mobile-first (PWA) độc lập cho bệnh nhân phòng khám, tách biệt hoàn toàn khỏi `frontend/` (dev tool nội bộ HIS).

## Stack
Next.js 16 App Router, TypeScript, TailwindCSS v4, TanStack Query v5, fetch thuần (không axios), không next-intl (chỉ tiếng Việt hardcode).

## Chạy dev
```bash
npm install
cp .env.local.example .env.local
npm run dev
```
Truy cập `http://phongkham-a.localhost:3000` (thêm entry vào hosts nếu cần), hoặc `http://localhost:3000` — khi đó app tự gắn header `X-Portal-Subdomain` (đọc từ `NEXT_PUBLIC_DEV_SUBDOMAIN`) để backend resolve tenant.

## Multi-tenant
Mỗi phòng khám có 1 subdomain riêng (`phongkham-a.diab.com.vn`). Backend tự resolve tenant theo `Host` header — FE không gửi tenant nào khác. Không có bộ chọn phòng khám trong UI.

## Auth
Token lưu ở cookie `portal-token` (SameSite=Lax). `middleware.ts` chỉ chặn các route cần đăng nhập (mọi route trừ `/login`, `/activate`) khi thiếu cookie.

## Cấu trúc
```
portal-client/
├── app/
│   ├── layout.tsx, globals.css, manifest.ts
│   ├── page.tsx                     — Trang chủ
│   ├── login/page.tsx                — Đăng nhập SĐT+PIN, quên PIN, đặt lại PIN
│   ├── activate/page.tsx             — Kích hoạt tài khoản (3 bước)
│   ├── queue/page.tsx                — Hàng đợi (poll 15s)
│   ├── appointments/page.tsx         — Danh sách + hủy lịch hẹn
│   ├── appointments/new/page.tsx     — Đặt lịch (3 bước: bác sĩ → giờ → xác nhận)
│   ├── encounters/page.tsx           — Lịch sử khám
│   ├── encounters/[id]/page.tsx      — Chi tiết kết quả khám + lời dặn + đơn thuốc
│   ├── prescriptions/page.tsx        — Đơn thuốc + tải PDF
│   ├── lab-results/page.tsx          — Kết quả xét nghiệm/CLS + tải PDF
│   ├── medications/page.tsx          — Lịch uống thuốc hôm nay theo buổi
│   ├── me/page.tsx                   — Hồ sơ cá nhân + đăng xuất
│   └── settings/notifications/page.tsx — Cài đặt thông báo push/email
├── components/                       — BigButton, BigCard, NumPad, BottomNav, AppShell, ConfirmDialog, StateViews, icons
├── lib/                               — api.ts, auth.ts, tenant.ts, utils.ts, push.ts, hooks.ts, types.ts
├── middleware.ts
└── public/sw.js                       — Service worker nhận web push
```

## Web Push
`lib/push.ts` cung cấp `subscribeToPush(vapidPublicKey)`: đăng ký service worker (`public/sw.js`), xin quyền `Notification`, `pushManager.subscribe`, rồi lưu subscription qua `POST /me/push-subscriptions`. Trên iOS chỉ hoạt động khi đã "Thêm vào Màn hình chính" (`navigator.standalone === true`); nếu chưa, màn `/settings/notifications` hiển thị hướng dẫn.

## Design tokens (người lớn tuổi thân thiện)
Font base 18px, heading 24-28px, nút tối thiểu 56px cao, bo góc 12-16px, focus ring 3px, icon luôn kèm label chữ. Xem `app/globals.css` và `components/BigButton.tsx`, `components/NumPad.tsx`.

## Ghi chú / giả định khác spec
- Icon PWA (`public/icons/icon-192.png`, `icon-512.png`) referenced trong manifest nhưng **chưa có file thật** — cần bổ sung asset trước khi build production PWA đầy đủ (không ảnh hưởng build/typecheck).
- Route quên PIN/đặt lại PIN gộp chung trong `/login` (chuyển step nội bộ) thay vì tách route riêng vì spec chỉ liệt kê `/login` trong danh sách màn hình.
- `appointmentId` từ API dùng `id` (string) cho hủy/tạo lịch; `doctorId` khi tạo lịch dùng chung giá trị `doctorRef` trả về từ `/booking/doctors` (spec không có endpoint đổi `doctorRef` → `doctorId` riêng).
- Không dùng `sonner`/toast library để giữ tối giản dependency — dùng banner lỗi inline trong từng trang thay cho toast.
