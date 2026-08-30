import type { TourDefinition } from "./types";

/** Tour hướng dẫn màn hình Chi tiết lượt khám (bác sĩ). */
export const encounterDetailTour: TourDefinition = {
  id: "encounter-detail",
  name: "Hướng dẫn màn hình Khám bệnh",
  steps: [
    {
      title: "Hướng dẫn màn hình Khám bệnh",
      description:
        "Màn hình chi tiết một lượt khám: bắt đầu khám, ghi bệnh án điện tử (EMR), chỉ định CLS, kê đơn, ký số và kết thúc khám. Đi qua vài bước để nắm quy trình chuẩn.",
    },
    {
      selector: '[data-tour="enc-start"]',
      title: "Bắt đầu khám",
      description:
        "Bấm \"Bắt đầu khám\" khi mời bệnh nhân vào phòng — trạng thái lượt khám chuyển sang Đang khám và tính thời gian mở bệnh án.",
      side: "bottom",
      align: "end",
    },
    {
      selector: '[data-tour="enc-emr-template"]',
      title: "Chọn mẫu bệnh án",
      description:
        "Chọn mẫu bệnh án hệ thống hoặc mẫu tuỳ chỉnh phù hợp chuyên khoa trước khi ghi nội dung khám — mẫu quyết định các trường biểu mẫu có cấu trúc hiển thị bên dưới.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="enc-vital"]',
      title: "Ghi sinh hiệu",
      description:
        "Ghi các chỉ số sinh hiệu (nhiệt độ, mạch, huyết áp, SpO2...) bằng cách nhập tay, hoặc nhập từ file PDF máy đo InBody nếu phòng khám có tích hợp — chỉ số bất thường sẽ được cảnh báo màu đỏ.",
      side: "left",
      align: "start",
    },
    {
      selector: '[data-tour="enc-tabs"]',
      title: "Các tab nghiệp vụ trong ca khám",
      description:
        "Chuyển qua các tab: Tiền sử, Cận lâm sàng (chỉ định XN/CĐHA), Kết quả CLS, Chẩn đoán ICD-10, Đơn thuốc, Tái khám, Tập tin — thao tác đúng thứ tự để bệnh án đầy đủ trước khi kết thúc.",
      side: "bottom",
      align: "start",
    },
    {
      selector: '[data-tour="enc-sign"]',
      title: "Ký số bệnh án",
      description:
        "Sau khi hoàn tất nội dung khám, bấm \"Ký số bệnh án\" để khoá nội dung EMR. Bệnh án đã ký chỉ có thể sửa bằng bản đính chính, không sửa trực tiếp được nữa.",
      side: "bottom",
      align: "end",
    },
    {
      selector: '[data-tour="enc-finish"]',
      title: "Kết thúc khám",
      description:
        "Bấm \"Kết thúc khám\" để đóng lượt khám. Hệ thống sẽ nhắc nếu chưa có chẩn đoán ICD-10 (thiếu sẽ không xuất được XML giám định BHYT) hoặc bệnh án chưa ký số.",
      side: "bottom",
      align: "end",
    },
  ],
};
