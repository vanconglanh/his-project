import type { TourDefinition } from "./types";

/** Tour hướng dẫn trang chi tiết hoá đơn (/billings/[id]). */
export const billingDetailTour: TourDefinition = {
  id: "billing-detail",
  name: "Hướng dẫn Chi tiết hoá đơn",
  steps: [
    {
      title: "Chi tiết hoá đơn",
      description:
        "Trang này hiển thị đầy đủ thông tin hoá đơn: mục dịch vụ, tổng hợp chi phí, BHYT và các thao tác thu tiền.",
    },
    {
      selector: '[data-tour="bill-items"]',
      title: "Mục hoá đơn",
      description: "Danh sách dịch vụ, thuốc kèm số lượng, đơn giá, VAT và thành tiền.",
      side: "right",
    },
    {
      selector: '[data-tour="bill-summary"]',
      title: "Tổng hợp",
      description:
        "Tạm tính, VAT, giảm giá, BHYT thanh toán, số tiền bệnh nhân phải trả và còn lại.",
      side: "left",
    },
    {
      selector: '[data-tour="bill-bhyt"]',
      title: "Áp dụng BHYT",
      description: "Bấm để áp dụng mức hưởng BHYT cho hoá đơn (nếu bệnh nhân có thẻ BHYT).",
      side: "left",
    },
    {
      selector: '[data-tour="bill-confirm"]',
      title: "Xác nhận hoá đơn",
      description: "Xác nhận hoá đơn ở trạng thái nháp để chuyển sang chờ thu tiền.",
      side: "bottom",
    },
    {
      selector: '[data-tour="bill-pay"]',
      title: "Thu tiền",
      description: "Ghi nhận thanh toán tiền mặt / thẻ / chuyển khoản cho hoá đơn.",
      side: "bottom",
    },
    {
      selector: '[data-tour="bill-qr"]',
      title: "Thanh toán QR động",
      description: "Tạo mã QR thanh toán động để bệnh nhân quét bằng app ngân hàng.",
      side: "bottom",
    },
  ],
};
