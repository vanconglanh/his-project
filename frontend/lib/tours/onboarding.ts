import type { TourDefinition } from "./types";

/**
 * Tour "Làm quen hệ thống" — chạy TỰ ĐỘNG đúng 1 lần khi 1 user cụ thể lần đầu
 * đăng nhập vào hệ thống (không gắn với 1 trang cụ thể).
 * Trạng thái đã xem lưu key riêng `tour-onboarding-seen:{userId}` (xem lib/tours/engine.ts),
 * KHÁC với key `tour-seen:{route}:{userId}` của các tour trang lẻ.
 *
 * Nội dung: chỉ gồm các thành phần dùng chung mọi trang (sidebar, đổi chi nhánh,
 * tìm kiếm toàn cục, thông báo, menu tài khoản) — không lặp lại với tour trang lẻ.
 */
export const onboardingTour: TourDefinition = {
  id: "onboarding",
  name: "Làm quen hệ thống",
  steps: [
    {
      title: "Chào mừng đến với Pro-Diab HIS",
      description:
        "Đây là lần đầu bạn đăng nhập — đi qua vài bước ngắn để làm quen với các thành phần dùng chung ở mọi trang trong hệ thống.",
    },
    {
      selector: '[data-tour="sidebar-nav"]',
      title: "Menu điều hướng",
      description:
        "Danh sách các phân hệ theo vai trò của bạn (Tiếp đón, Khám bệnh, CLS, Kê đơn, Kho dược, Thu ngân, Báo cáo...). Bấm nút ở cuối sidebar để thu gọn/mở rộng.",
      side: "right",
      align: "start",
    },
    {
      selector: '[data-tour="branch-switcher"]',
      title: "Chuyển đổi chi nhánh",
      description:
        "Nếu bạn quản lý nhiều chi nhánh, dùng nút này để chọn chi nhánh đang làm việc — toàn bộ dữ liệu hiển thị sẽ theo chi nhánh đã chọn.",
      side: "bottom",
      align: "center",
    },
    {
      selector: '[data-tour="global-search"]',
      title: "Tìm kiếm toàn cục",
      description:
        "Bấm hoặc dùng phím tắt Ctrl+K để tìm nhanh bệnh nhân, lượt khám, hoá đơn... mà không cần rời trang hiện tại.",
      side: "bottom",
      align: "center",
    },
    {
      selector: '[data-tour="notifications"]',
      title: "Thông báo",
      description:
        "Theo dõi các thông báo hệ thống: nhắc lịch tái khám, cảnh báo tồn kho, kết quả CLS mới...",
      side: "bottom",
      align: "end",
    },
    {
      selector: '[data-tour="user-menu"]',
      title: "Menu tài khoản",
      description:
        "Xem hồ sơ cá nhân, đổi mật khẩu, cài đặt thông báo, mở lại Trung tâm trợ giúp hoặc đăng xuất.",
      side: "bottom",
      align: "end",
    },
  ],
};
