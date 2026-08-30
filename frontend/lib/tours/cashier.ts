import type { TourDefinition } from "./types";

/** Tour hướng dẫn trang Thu ngân (/cashier). */
export const cashierTour: TourDefinition = {
  id: "cashier",
  name: "Hướng dẫn Thu ngân",
  steps: [
    {
      title: "Chào mừng đến trang Thu ngân",
      description:
        "Đây là nơi quản lý ca làm việc, thu tiền hoá đơn, xem lịch sử thanh toán và công nợ bệnh nhân.",
    },
    {
      selector: '[data-tour="cashier-shift"]',
      title: "Mở ca / Đóng ca",
      description:
        "Bấm để mở ca trước khi bắt đầu thu tiền, và đóng ca cuối ngày để đối soát tiền mặt.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="cashier-shift-status"]',
      title: "Trạng thái ca",
      description: "Hiển thị ca hiện đang mở hay đã đóng.",
      side: "bottom",
    },
    {
      selector: '[data-tour="cashier-stats"]',
      title: "Thống kê nhanh",
      description:
        "Tổng thu hôm nay, số giao dịch, refund/void và tổng công nợ hiện tại.",
      side: "bottom",
    },
    {
      selector: '[data-tour="cashier-tabs"]',
      title: "Các tab làm việc",
      description:
        "Chuyển giữa Hoá đơn chờ thu, Lịch sử hôm nay, Công nợ và Ca làm việc.",
      side: "bottom",
      align: "start",
    },
  ],
};
