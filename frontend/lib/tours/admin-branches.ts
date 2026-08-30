import type { TourDefinition } from "./types";

/** Tour hướng dẫn trang Quản lý chi nhánh (/admin/branches). */
export const adminBranchesTour: TourDefinition = {
  id: "admin-branches",
  name: "Hướng dẫn Quản lý chi nhánh",
  steps: [
    {
      title: "Quản lý chi nhánh",
      description:
        "Danh sách các chi nhánh của tenant: tạo mới, nhân bản cấu hình, đặt mặc định và bật/tắt hoạt động.",
    },
    {
      selector: '[data-tour="branch-search"]',
      title: "Tìm chi nhánh",
      description: "Gõ tên hoặc mã chi nhánh để lọc nhanh danh sách.",
      side: "bottom",
    },
    {
      selector: '[data-tour="branch-create"]',
      title: "Tạo chi nhánh",
      description: "Bấm để khởi tạo một chi nhánh mới từ đầu.",
      side: "bottom",
    },
    {
      selector: '[data-tour="branch-clone"]',
      title: "Nhân bản chi nhánh",
      description:
        "Sao chép cấu hình từ chi nhánh có sẵn để tạo chi nhánh mới nhanh hơn, đỡ phải cấu hình lại từ đầu.",
      side: "bottom",
    },
    {
      selector: '[data-tour="branch-table"]',
      title: "Bảng chi nhánh & hành động",
      description:
        "Mỗi dòng có các thao tác: Checklist go-live (kiểm tra sẵn sàng), Đặt làm mặc định, Bật/Tắt chi nhánh, Sửa và Xoá.",
      side: "top",
    },
  ],
};
