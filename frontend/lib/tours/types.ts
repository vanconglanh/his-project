/**
 * Kiểu dữ liệu khai báo bước tour cho tính năng "Hướng dẫn" (product tour) chạy runtime trong app.
 * Engine dùng driver.js — xem lib/tours/engine.ts.
 */

/** Một bước trong tour. */
export interface TourStep {
  /**
   * CSS selector trỏ tới element cần khoanh vùng.
   * Khuyến nghị dùng `[data-tour="..."]` cho ổn định thay vì class động.
   * Nếu bỏ trống -> hiển thị tooltip giữa màn hình (bước intro/outro không gắn element).
   */
  selector?: string;
  /** Tiêu đề tooltip (tiếng Việt có dấu). */
  title: string;
  /** Nội dung mô tả bước (tiếng Việt có dấu). Cho phép HTML đơn giản. */
  description: string;
  /**
   * Permission code cần có để hiển thị bước này. Nếu user không có quyền -> ẩn bước.
   * Bỏ trống = ai cũng thấy.
   */
  permission?: string;
  /** Vị trí tooltip so với element. */
  side?: "top" | "right" | "bottom" | "left";
  /** Căn lề tooltip. */
  align?: "start" | "center" | "end";
}

/** Một bộ tour (một kịch bản dẫn dắt). */
export interface TourDefinition {
  /** Mã định danh duy nhất của tour (dùng làm 1 phần key localStorage). */
  id: string;
  /** Tên hiển thị (tiếng Việt). */
  name: string;
  /**
   * Permission code tối thiểu để tour này khả dụng cho user.
   * Nếu user không có -> registry sẽ bỏ qua tour này khi chọn theo role.
   * Bỏ trống = mọi role đều có thể xem.
   */
  requiredPermission?: string;
  /** Danh sách bước. */
  steps: TourStep[];
}
