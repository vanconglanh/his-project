import type { TourDefinition } from "./types";

/** Tour hướng dẫn trang Kho dược (/pharmacy). */
export const pharmacyTour: TourDefinition = {
  id: "pharmacy",
  name: "Hướng dẫn Kho dược",
  steps: [
    {
      title: "Quản lý kho dược",
      description:
        "Theo dõi tồn kho, nhập kho, phát thuốc cho bệnh nhân, điều chỉnh tồn kho và cảnh báo hết hạn/thiếu hàng.",
    },
    {
      selector: '[data-tour="pharmacy-tabs"]',
      title: "Các tab kho dược",
      description:
        "Tồn kho, Nhập kho, Phát thuốc, Điều chỉnh và Cảnh báo — chuyển tab để xem từng nghiệp vụ.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="pharmacy-dispense-search"]',
      title: "Tìm bệnh nhân để phát thuốc",
      description: "Nhập tên hoặc mã bệnh nhân để tìm đơn thuốc cần phát.",
      side: "bottom",
    },
    {
      selector: '[aria-label="Tạo điều chỉnh tồn kho"]',
      title: "Tạo điều chỉnh tồn kho",
      description:
        "Bấm khi cần điều chỉnh số lượng tồn (hao hụt, kiểm kê, hư hỏng) — nút luôn hiển thị ở mọi tab.",
      side: "left",
    },
    {
      title: "Điều chuyển kho",
      description:
        "Cần chuyển thuốc giữa các kho/chi nhánh? Vào trang /pharmacy/stock-transfers để tạo phiếu điều chuyển.",
    },
  ],
};
