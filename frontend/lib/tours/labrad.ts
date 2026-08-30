import type { TourDefinition } from "./types";

/** Tour hướng dẫn màn hình Cận lâm sàng (CLS). */
export const labradTour: TourDefinition = {
  id: "labrad",
  name: "Hướng dẫn màn hình Cận lâm sàng",
  steps: [
    {
      title: "Hướng dẫn màn hình Cận lâm sàng",
      description:
        "Quản lý kết quả xét nghiệm, chẩn đoán hình ảnh, đối tác lab liên kết và tích hợp máy/lab ngoài. Đi qua vài bước để nắm nhanh cách tìm và nhập kết quả.",
    },
    {
      selector: '[data-tour="labrad-tabs"]',
      title: "Các tab CLS",
      description:
        "4 tab chính: Kết quả xét nghiệm, Kết quả CĐHA, Đối tác lab (liên kết gửi mẫu ngoài) và Tích hợp lab (kết nối máy/hệ thống LIS).",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="labrad-search"]',
      title: "Tìm kiếm nhanh",
      description:
        "Gõ tên hoặc mã chỉ số xét nghiệm để lọc nhanh trong danh sách kết quả hiện có.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="labrad-filter"]',
      title: "Lọc theo trạng thái / cờ bất thường",
      description:
        "Lọc theo trạng thái (Nháp/Đã xác thực/Đã sửa) hoặc theo cờ cảnh báo (Cao/Thấp/Rất cao/Rất thấp/Nguy kịch) để ưu tiên xử lý các kết quả bất thường trước.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="labrad-table"]',
      title: "Bảng kết quả",
      description:
        "Bấm \"Nhập kết quả\" trên từng dòng để ghi giá trị xét nghiệm; xác thực kết quả trước khi in. Lưu ý: một số chỉ định CLS cần thu tiền theo đợt trước khi trả kết quả cho bệnh nhân.",
      side: "top",
      align: "start",
    },
  ],
};
