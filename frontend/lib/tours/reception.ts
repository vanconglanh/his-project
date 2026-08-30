import type { TourDefinition } from "./types";

/** Tour hướng dẫn màn hình Tiếp đón (lễ tân). */
export const receptionTour: TourDefinition = {
  id: "reception",
  name: "Hướng dẫn màn hình Tiếp đón",
  steps: [
    {
      title: "Hướng dẫn màn hình Tiếp đón",
      description:
        "Đây là màn hình lễ tân dùng hằng ngày: tra cứu/tạo bệnh nhân, quét CCCD, check-in và theo dõi hàng đợi khám. Đi qua vài bước để nắm nhanh thao tác chính.",
    },
    {
      selector: '[data-tour="reception-add-patient"]',
      title: "Thêm bệnh nhân mới",
      description:
        "Bấm nút này (hoặc phím tắt F2) khi bệnh nhân chưa có hồ sơ để tạo mới trước khi check-in.",
      side: "bottom",
      align: "end",
    },
    {
      selector: '[data-tour="reception-stats"]',
      title: "Thống kê nhanh trong ngày",
      description:
        "Theo dõi số lượng đang chờ, đang khám, đã khám xong và đã huỷ để nắm tình hình phòng khám theo thời gian thực.",
      side: "bottom",
      align: "center",
    },
    {
      selector: '[data-tour="reception-checkin-form"]',
      title: "Khu vực check-in",
      description:
        "Tìm bệnh nhân theo tên, số điện thoại, CMND hoặc số thẻ BHYT. Nếu không tìm thấy, dùng nút \"Tạo bệnh nhân mới\" ngay trong danh sách kết quả.",
      side: "right",
      align: "start",
    },
    {
      selector: '[data-tour="reception-qr-scan"]',
      title: "Quét mã QR CCCD",
      description:
        "Đặt con trỏ vào ô quét rồi dùng máy quét USB quét mã QR trên thẻ CCCD — hệ thống tự dò trùng và điền thông tin, tránh tạo trùng hồ sơ bệnh nhân.",
      side: "right",
      align: "start",
    },
    {
      selector: '[data-tour="reception-checkin-form"]',
      title: "Chọn phòng khám và mức ưu tiên",
      description:
        "Sau khi chọn bệnh nhân, chọn phòng khám còn trống bác sĩ, mức ưu tiên (Thông thường/Ưu tiên/Khẩn cấp) rồi bấm \"Tiếp đón (F4)\" để hoàn tất check-in.",
      side: "right",
      align: "start",
    },
    {
      selector: '[data-tour="reception-queue"]',
      title: "Bảng hàng đợi",
      description:
        "Danh sách bệnh nhân đang chờ/đang khám theo từng phòng, cập nhật theo thời gian thực — dùng để theo dõi và điều phối luồng khám.",
      side: "left",
      align: "start",
    },
  ],
};
